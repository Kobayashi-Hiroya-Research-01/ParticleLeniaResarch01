using Assets.Packages.MathNet.Numerics.Custom;
using Assets.ParticleLeniaResarch01.Analyzers;
using Assets.ParticleLeniaResarch01.Scripts.Data;
using Assets.ParticleLeniaResarch01.Scripts.Fields;
using Assets.ParticleLeniaResarch01.Scripts.MathObjects;
using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Assets.ParticleLeniaResarch01.Scripts
{
    public sealed class ParticleLeniaSimulation : ParticleLeniaSimulation.IState, IParticleLenia
    {
        public interface IOption
        {
            string Description { get; set; }
            int ParticleCount { get; set; }
            int Dimensions { get; set; }
            double DeltaTime { get; set; }

            void CopyTo(IOption other)
            {
                other.Description = Description;
                other.ParticleCount = ParticleCount;
                other.Dimensions = Dimensions;
                other.DeltaTime = DeltaTime;
            }
        }

        public interface IState : IOption
        {
            int CurrentStep { get; set; }

            double CurrentTime { get; }

            /// <summary>
            /// 粒子状態のキャッシュ
            /// </summary>
            ParticleInfoArray TempResult { get; }

            /// <summary>
            /// 最新の粒子状態
            /// </summary>
            ParticleInfoArray LastResult { get; }

            void CopyTo(IState other)
            {
                Option.CopyTo(other);
                TempResult.CopyTo(other.TempResult);
                LastResult.CopyTo(other.LastResult);
                other.CurrentStep = CurrentStep;
            }

            IOption Option => this;
        }

        private class ResultsSource
        {
            private DistanceCache distances = new(0);
            private ParticleInfoArray tempResult;
            private ParticleInfoArray lastResult;

            public ResultsSource()
            {
                tempResult = new(0, 0, 0);
                lastResult = new(0, 0, 0);
            }

            /// <summary>
            /// 粒子状態のキャッシュ
            /// </summary>
            public ParticleInfoArray TempResult => tempResult;

            /// <summary>
            /// 最新の粒子状態
            /// </summary>
            public ParticleInfoArray LastResult => lastResult;

            public void Init(ParticleInfoArray particles)
            {
                particles ??= new(0, 0, 0);
                particles.CopyTo(lastResult);
                particles.CopyTo(tempResult);
            }

            public void RemovePoints(ReadOnlySpan<int> particles)
            {
                tempResult.RemoveAll(particles);
                lastResult.RemoveAll(particles);
            }

            public ParticleInfoArray MakeNewLastResult(int currentStep, double currentTime)
            {
                var lastResult = TempResult;
                var tempResult = LastResult;
                this.tempResult = tempResult;
                this.lastResult = lastResult;
                this.lastResult.Step = currentStep;
                this.lastResult.Time = currentTime;

                return lastResult;
            }

            public double GetLastDistance(int a, int b)
            {
                var ap = lastResult[a].position;
                var bp = lastResult[b].position;
                return distances[a, ap, b, bp];
            }

            public void ResetLastDistance(int particleCount)
            {
                distances.Reset(particleCount);
            }

            public ParticleInfoArray Init(ParticleInfoArray particles, ParticleLeniaAnalyzerForeachEvent forEachCreatedParticle)
            {
                particles ??= new(0, 0, 0);
                lastResult = particles;
                var array = lastResult.Array;

                ResetLastDistance(array.Length);

                for (int i = 0; i < array.Length; i++)
                {
                    var lInfo = array[i];
                    UpdateUtRtGt(lInfo, i);
                    forEachCreatedParticle?.Invoke(lInfo, i);
                }

                lastResult.Step = 0;
                lastResult.Time = 0.0;

                lastResult.CopyTo(tempResult);

                return lastResult;
            }

            public ParticleInfo StepForParticle(int index, Grad grad, EnergyField energyField, double deltaTime)
            {
                var lastInfo = lastResult[index];
                var nextInfo = tempResult[index];

                PositionUpdate(lastInfo, nextInfo, grad, energyField, deltaTime);
                UpdateUtRtGt(nextInfo, index);

                return nextInfo;
            }

            private void UpdateUtRtGt(ParticleInfo result, int index)
            {
                var particles = lastResult.Array;
                var particleCount = particles.Length;
                var ut = 0.0;
                var rt = 0.0;

                for (int i = 0; i < particleCount; i++)
                {
                    if (i == index) continue;

                    var fields = particles[i].fields;
                    var distance = GetLastDistance(i, index);
                    ut += fields.K(distance);
                    rt += fields.R(distance);
                }

                result.lastUt = ut;
                result.lastRt = rt;
                result.lastGt = result.fields.G(ut);
            }

            private void PositionUpdate(ParticleInfo oldInfo, ParticleInfo newInfo, Grad grad, EnergyField energyField, double deltaTime)
            {
                var oldPosition = oldInfo.position;
                var newPosition = newInfo.position;

                var gradValue = grad.GetGrad(oldPosition, energyField, newInfo.fields);

                for (int i = 0; i < gradValue.Length; i++)
                {
                    newPosition[i] = oldPosition[i] - deltaTime * gradValue[i];
                }
            }
        }

        private class ThreadingSource
        {
            public const int BLOCK_SIZE_MIN = 8;

            public const int DEFAULT_Thread_Count = 32;

            public delegate void ParallelStepEvent(int index, Grad grad);

            private class ThreadFunc
            {
                private Task task;
                private Grad grad;
                private int start;
                private int end;
                private ParallelStepEvent stepForParticle;

                public void RunTask(int start, int end, ParallelStepEvent stepForParticle)
                {
                    this.start = start;
                    this.end = end;
                    this.stepForParticle = stepForParticle;

                    task = Task.Run(Run);
                }

                private void Run()
                {
                    for (int i = start; i < end; i++)
                    {
                        stepForParticle.Invoke(i, grad);
                    }
                }

                public void Reset()
                {
                    task = null;
                    grad = new();
                    start = 0;
                    end = 0;
                    stepForParticle = null;
                }

                public void Wait()
                {
                    task.Wait();
                    task = null;
                }
            }

            private int threadCount;
            private ThreadFunc[] threadFuncs;

            public ThreadingSource()
            {
                threadCount = DEFAULT_Thread_Count;
                threadFuncs = new ThreadFunc[DEFAULT_Thread_Count];
                ResetThreads();
            }

            public int ThreadCount
            {
                get => threadCount;
                set
                {
                    value = Math.Max(1, value);
                    threadCount = value;

                    if (threadFuncs.Length < threadCount)
                    {
                        threadFuncs = new ThreadFunc[threadFuncs.Length * 2];
                        ResetThreads();
                    }
                }
            }

            public void Init()
            {
                ResetThreads();
            }

            public void Next(int particleCount, ParallelStepEvent stepForParticle)
            {
                if (ThreadCount == 1 || particleCount <= BLOCK_SIZE_MIN)
                {
                    var grad = new Grad();
                    for (int i = 0; i < particleCount; i++)
                    {
                        stepForParticle(i, grad);
                    }
                }
                else
                {
                    ParallelStep(particleCount, stepForParticle);
                }
            }

            private void ParallelStep(int particleCount, ParallelStepEvent stepForParticle)
            {
                int block = Math.Max(BLOCK_SIZE_MIN, 1 + particleCount / threadCount);

                var maxThreadCount = threadCount;
                var runThreadCount = 0;
                int start = 0;

                for (int i = 0; i < maxThreadCount; i++)
                {
                    var end = start + block;
                    if (end < particleCount)
                    {
                        var func = threadFuncs[i];
                        func.RunTask(start, end, stepForParticle);
                        start = end;
                    }
                    else
                    {
                        var func = threadFuncs[i];
                        func.RunTask(start, particleCount, stepForParticle);
                        runThreadCount = i + 1;
                        break;
                    }
                }

                for (int i = 0; i < runThreadCount; i++)
                {
                    threadFuncs[i].Wait();
                }
            }

            private void ResetThreads()
            {
                for (int i = 0; i < threadFuncs.Length; i++)
                {
                    threadFuncs[i] ??= new();
                    threadFuncs[i].Reset();
                }
            }
        }

        private class EnergyField : Grad.IValueAtPoint<IParticleFields>
        {
            private readonly ResultsSource results;

            public EnergyField(ResultsSource results)
            {
                this.results = results;
            }

            public double GetValueAt(ReadOnlySpan<double> point, IParticleFields particleFields)
            {
                return GetEnergyAt(point, particleFields);
            }

            public double GetEnergyAt(ReadOnlySpan<double> point, IParticleFields particleFields)
            {
                GetUtAndRt(point, out var ut, out var rt);
                return rt - particleFields.G(ut);
            }

            public void GetUtAndRt(ReadOnlySpan<double> point, out double ut, out double rt)
            {
                var array = results.LastResult.Array;
                ut = 0.0;
                rt = 0.0;

                for (int i = 0; i < array.Length; i++)
                {
                    var distance = DistanceCache.Distance(array[i].position, point);
                    var fields = array[i].fields;
                    ut += fields.K(distance);
                    rt += fields.R(distance);
                }
            }
        }

        private class OptionData : IOption
        {
            public string Description { get; set; } = "Particle Lenia original simulation";

            public int ParticleCount { get; set; }

            public int Dimensions { get; set; }

            public double DeltaTime { get; set; }

            public void Init(IParticleLeniaInitializer initializer)
            {
                ParticleCount = initializer.ParticleCount;
                Dimensions = initializer.Dimensions;
                DeltaTime = initializer.DeltaTime;
            }

            public IOption AsOption => this;
        }

        private class StateData : IState
        {
            private readonly OptionData option;
            private readonly ResultsSource results;

            public StateData(OptionData option, ResultsSource results)
            {
                this.option = option;
                this.results = results;
                CurrentStep = 0;
            }

            public int CurrentStep { get; set; }

            public ParticleInfoArray TempResult => results.TempResult;
            public ParticleInfoArray LastResult => results.LastResult;
            public string Description { get => option.Description; set => option.Description = value; }
            public int ParticleCount { get => option.ParticleCount; set => option.ParticleCount = value; }
            public int Dimensions { get => option.Dimensions; set => option.Dimensions = value; }
            public double DeltaTime { get => option.DeltaTime; set => option.DeltaTime = value; }

            public double CurrentTime => CurrentStep * DeltaTime;

            public IOption Option => this;
            public IState AsState => this;

            public void Init()
            {
                CurrentStep = 0;
            }

            public void Next()
            {
                CurrentStep++;
            }
        }

        private readonly OptionData option;
        private readonly StateData state;
        private readonly ResultsSource results;
        private readonly ThreadingSource threading;
        private readonly EnergyField energyField;

        public ParticleLeniaSimulation()
        {
            results = new();
            threading = new();
            energyField = new(results);
            option = new();
            state = new(option, results);
        }

        public IOption Option => this;

        public IState State => this;

        public void CopyTo(ParticleLeniaSimulation other)
        {
            State.CopyTo(other);
        }

        public IParticleLeniaInitializer Initializer { get; set; }

        int IState.CurrentStep { get => state.CurrentStep; set => state.CurrentStep = value; }
        double IState.CurrentTime => state.CurrentTime;
        ParticleInfoArray IState.TempResult => state.TempResult;
        ParticleInfoArray IState.LastResult => state.LastResult;

        string IOption.Description { get => state.Description; set => state.Description = value; }
        int IOption.ParticleCount { get => state.ParticleCount; set => state.ParticleCount = value; }
        int IOption.Dimensions { get => state.Dimensions; set => state.Dimensions = value; }
        double IOption.DeltaTime { get => state.DeltaTime; set => state.DeltaTime = value; }

        string IParticleLenia.Description => state.Description;

        double IParticleLenia.CurrentTime => state.CurrentTime;

        int IParticleLenia.CurrentStep => state.CurrentStep;

        public void RemovePoints(ReadOnlySpan<int> particles)
        {
            Option.ParticleCount -= particles.Length;
            results.RemovePoints(particles);
        }

        public ParticleInfoArray GetLastResult()
        {
            return results.LastResult;
        }

        public ParticleInfoArray Init(ParticleLeniaAnalyzerForeachEvent forEachCreatedParticle)
        {
            Control.UseBestProviders();

            option.Init(Initializer);
            state.Init();
            return results.Init(Initializer.CreateParticles(), forEachCreatedParticle);
        }

        private ParticleLeniaAnalyzerForeachEvent nextForEachUpdatedParticle;

        public ParticleInfoArray Next(ParticleLeniaAnalyzerForeachEvent forEachUpdatedParticle)
        {
            nextForEachUpdatedParticle = forEachUpdatedParticle;

            results.ResetLastDistance(option.ParticleCount);
            threading.Next(option.ParticleCount, StepForParticle);
            state.Next();

            var result = results.MakeNewLastResult(state.CurrentStep, state.CurrentTime);

            return result;
        }

        private void StepForParticle(int index, Grad grad)
        {
            var nextInfo = results.StepForParticle(index, grad, energyField, option.DeltaTime);
            nextForEachUpdatedParticle?.Invoke(nextInfo, index);
        }
    }
}