using Assets.Packages.MathNet.Numerics.Custom;
using Assets.ParticleLeniaResarch01.Analyzers;
using Assets.ParticleLeniaResarch01.Scripts.Data;
using Assets.ParticleLeniaResarch01.Scripts.Fields;
using Assets.ParticleLeniaResarch01.Scripts.MathObjects;
using MathNet.Numerics;
using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.ParticleLeniaResarch01.Scripts
{
    public sealed class ParticleLeniaSimulation : IParticleLenia, Grad.IValueAtPoint<IParticleFields>
    {
        private const int BLOCK_SIZE_MIN = 40;
        private const int DEFAULT_Thread_Count = 15;

        /// <summary>
        /// 計算量とメモリアロケーション削減のための資源
        /// </summary>
        private class CulcSource
        {
            public CulcSource Clone()
            {
                return new CulcSource()
                {
                    tempResult = tempResult.Clone(),
                    lastResult = lastResult.Clone(),
                    distances = new(distances.Distances.Length),
                    ParticleCount = ParticleCount,
                    Dimensions = Dimensions,
                    DeltaTime = DeltaTime,
                    threads = new Task[DEFAULT_Thread_Count],
                };
            }

            /// <summary>
            /// 粒子状態のキャッシュ
            /// </summary>
            public ParticleInfoArray tempResult;

            /// <summary>
            /// 最新の粒子状態
            /// </summary>
            public ParticleInfoArray lastResult;

            public DistanceCache distances;

            public Task[] threads = new Task[DEFAULT_Thread_Count];

            public int ParticleCount { get; private set; }
            public int Dimensions { get; private set; }
            public double DeltaTime { get; private set; }

            private CulcSource()
            {
            }

            public CulcSource(IParticleLeniaInitializer initializer)
            {
                var particleCount = initializer.ParticleCount;
                var dimensions = initializer.Dimensions;
                ParticleCount = particleCount;
                Dimensions = dimensions;
                DeltaTime = initializer.DeltaTime;

                lastResult = initializer.CreateParticles();
                tempResult = NewTempResult(lastResult, particleCount, dimensions);
            }

            private static ParticleInfoArray NewTempResult(ParticleInfoArray lastResult, int particleCount, int dimensions)
            {
                var tempResult = new ParticleInfoArray(particleCount, 0, 0);

                for (var i = 0; i < particleCount; i++)
                {
                    var last = lastResult[i];
                    var tempPos = new double[dimensions];
                    tempResult[i] = new ParticleInfo(last.fields, tempPos, last.lastGt, last.lastRt, last.lastUt);
                }

                return tempResult;
            }
        }

        public int ThreadCount
        {
            get => source.threads.Length;
            set
            {
                if (value != source.threads.Length)
                {
                    source.threads = new Task[value];
                }
            }
        }

        private CulcSource source;
        private readonly Grad grad = new();

        public ParticleLeniaSimulation Clone()
        {
            return new ParticleLeniaSimulation()
            {
                source = source.Clone(),
                Initializer = Initializer,
                CurrentStep = CurrentStep,
            };
        }

        public IParticleLeniaInitializer Initializer { get; set; }

        public string Description => "Particle Lenia original simulation";

        public int CurrentStep { get; private set; }
        public int ParticleCount => source.ParticleCount;
        public int Dimensions => source.Dimensions;
        public double DeltaTime => source.DeltaTime;
        public double CurrentTime => CurrentStep * DeltaTime;

        public ParticleInfoArray GetLastResult()
        {
            return source.lastResult;
        }

        public ParticleInfoArray Next(ParticleLeniaAnalyzerForeachEvent forEachUpdatedParticle)
        {
            if (source == null)
            {
                throw new NotSupportedException();
            }

            DistanceReset();

            if (forEachUpdatedParticle == null)
            {
                if (ParticleCount <= BLOCK_SIZE_MIN)
                {
                    for (int i = 0; i < ParticleCount; i++)
                    {
                        StepForParticle(i);
                    }
                }
                else
                {
                    ParallelStep(StepForParticle);
                }
            }
            else
            {
                if (ParticleCount <= BLOCK_SIZE_MIN)
                {
                    for (int i = 0; i < ParticleCount; i++)
                    {
                        var nextInfo = StepForParticle(i);
                        forEachUpdatedParticle.Invoke(nextInfo, i);
                    }
                }
                else
                {
                    ParallelStep((start, end) => StepForParticle(start, end, forEachUpdatedParticle));
                }
            }

            var nextResult = source.tempResult;
            var lastResult = source.lastResult;
            source.lastResult = nextResult;
            source.tempResult = lastResult;

            CurrentStep++;

            source.lastResult.Step = CurrentStep;
            source.lastResult.Time = CurrentTime;

            return source.lastResult;
        }

        public ParticleInfoArray Init(ParticleLeniaAnalyzerForeachEvent forEachCreatedParticle)
        {
            Control.UseBestProviders();

            source = new CulcSource(Initializer);
            CurrentStep = 0;

            var lastResult = source.lastResult.Array;
            DistanceReset();

            for (int i = 0; i < lastResult.Length; i++)
            {
                var pInfo = lastResult[i];
                GetUtAndRt(out var ut, out var rt, lastResult, i);
                var gt = pInfo.fields.G(ut);
                pInfo.lastGt = gt;
                pInfo.lastRt = rt;
                pInfo.lastUt = ut;
                forEachCreatedParticle?.Invoke(pInfo, i);
            }

            source.lastResult.Step = CurrentStep;
            source.lastResult.Time = CurrentTime;

            return source.lastResult;
        }

        private void ParallelStep(Action<int, int> stepForParticle)
        {
            int block = Math.Max(BLOCK_SIZE_MIN, 1 + ParticleCount / ThreadCount);
            int next = 0;
            int threadIndex = 0;
            while (next < ParticleCount)
            {
                var _end = Math.Min(ParticleCount, next + block);
                var _next = next;
                source.threads[threadIndex] = Task.Run(() =>
                {
                    stepForParticle(_next, _end);
                });

                next = _end;
                threadIndex++;
            }

            Task.WhenAll(source.threads[..threadIndex]).Wait();
            source.threads.AsSpan().Fill(null);
        }

        private void StepForParticle(int start, int end)
        {
            for (int i = start; i < end; i++)
            {
                StepForParticle(i);
            }
        }

        private void StepForParticle(int start, int end, ParticleLeniaAnalyzerForeachEvent forEachUpdatedParticle)
        {
            for (int i = start; i < end; i++)
            {
                var nextInfo = StepForParticle(i);
                forEachUpdatedParticle.Invoke(nextInfo, i);
            }
        }

        private void DistanceReset()
        {
            source.distances.Reset(ParticleCount);
        }

        private double GetDistance(int a, int b)
        {
            var array = source.lastResult;
            return source.distances[a, array[a].position, b, array[b].position];
        }

        private ParticleInfo StepForParticle(int index)
        {
            var lastResult = source.lastResult;
            var nextResult = source.tempResult;

            var lastInfo = lastResult[index];
            var lastPosition = lastInfo.position;
            var nextInfo = nextResult[index];
            var nextPosition = nextInfo.position;

            GetUtAndRt(out var ut, out var rt, lastResult.Array, index);
            var gt = lastInfo.fields.G(ut);
            nextInfo.lastGt = gt;
            nextInfo.lastRt = rt;
            nextInfo.lastUt = ut;

            PositionUpdate(lastPosition, nextPosition, lastInfo.fields);
            //nextPosition.CopyTo(distances.positions[index]);

            return nextInfo;
        }

        private void PositionUpdate(ReadOnlySpan<double> oldPosition, Span<double> newPosition, IParticleFields particleFields)
        {
            Span<double> dpPosition = stackalloc double[Dimensions];

            grad.GetGrad(dpPosition, oldPosition, this, particleFields);

            for (int i = 0; i < dpPosition.Length; i++)
            {
                newPosition[i] = oldPosition[i] - DeltaTime * dpPosition[i];
            }
        }

        private void GetUtAndRt(out double ut, out double rt, ReadOnlySpan<ParticleInfo> source, ReadOnlySpan<double> point)
        {
            ut = 0.0;
            rt = 0.0;

            for (int i = 0; i < source.Length; i++)
            {
                var distance = DistanceCache.Distance(source[i].position, point);
                var fields = source[i].fields;
                ut += fields.K(distance);
                rt += fields.R(distance);
            }
        }

        private void GetUtAndRt(out double ut, out double rt, ReadOnlySpan<ParticleInfo> source, int index)
        {
            ut = 0.0;
            rt = 0.0;

            int i;
            for (i = 0; i < index; i++)
            {
                var fields = source[i].fields;
                var distance = GetDistance(i, index);
                ut += fields.K(distance);
                rt += fields.R(distance);
            }

            for (i = index + 1; i < source.Length; i++)
            {
                var fields = source[i].fields;
                var distance = GetDistance(i, index);
                ut += fields.K(distance);
                rt += fields.R(distance);
            }
        }

        private double GetEnergyAt(ReadOnlySpan<double> point, IParticleFields particleFields)
        {
            GetUtAndRt(out var ut, out var rt, source.lastResult.Array, point);
            return rt - particleFields.G(ut);
        }

        double Grad.IValueAtPoint<IParticleFields>.GetValueAt(ReadOnlySpan<double> point, IParticleFields particleFields)
        {
            return GetEnergyAt(point, particleFields);
        }
    }
}