using Assets.ParticleLenia.Scripts;
using Assets.ParticleLeniaResarch01.Scripts.Fields;
using System;
using System.Threading;
using System.Threading.Tasks;
using Assets.ParticleLeniaResarch01.Scripts.Data;
using UnityEngine.UIElements;
using System.Globalization;
using UnityEngine;
using MathNet.Numerics;
using Assets.Packages.MathNet.Numerics.Custom;
using System.Linq;

namespace Assets.ParticleLeniaResarch01.Scripts.Data
{
    public sealed class ParticleLeniaSimulation : IParticleLenia, Grad.IValueAtPoint<IParticleFields>
    {
        private const int DEFAULT_Thread_Count = 10;

        /// <summary>
        /// 計算量とメモリアロケーション削減のための資源
        /// </summary>
        private class CulcSource
        {
            /// <summary>
            /// 粒子状態のキャッシュ
            /// </summary>
            public ParticleInfoArray tempResult;

            /// <summary>
            /// 最新の粒子状態
            /// </summary>
            public ParticleInfoArray lastResult;

            /// <summary>
            /// 粒子位置のキャッシュ
            /// </summary>
            public double[] positions;

            /// <summary>
            /// 粒子距離のキャッシュ
            /// </summary>
            public double[] distances;

            public int ParticleCount { get; private set; }
            public int Dimensions { get; private set; }
            public double DeltaTime { get; private set; }
            public Span2D<double> Positions => new(positions, ParticleCount, Dimensions);
            public DistanceCache Distances => new(distances, Positions);

            public Task[] threads = new Task[DEFAULT_Thread_Count];

            public CulcSource(IParticleLeniaInitializer initializer)
            {
                var particleCount = initializer.ParticleCount;
                var dimensions = initializer.Dimensions;
                ParticleCount = particleCount;
                Dimensions = dimensions;
                DeltaTime = initializer.DeltaTime;

                lastResult = initializer.CreateParticles();
                tempResult = NewTempResult(lastResult, particleCount, dimensions);
                positions = NewPositions(lastResult, particleCount, dimensions);
                distances = NewDistances(particleCount);
                new DistanceCache(distances, Positions).Reset();
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

            private static double[] NewPositions(ParticleInfoArray lastResult, int particleCount, int dimensions)
            {
                int size = Span2D<double>.GetRawSize(particleCount, dimensions);
                var positions = new double[size];
                var positions2d = new Span2D<double>(positions, particleCount, dimensions);
                var lastResultArray = lastResult.Array;
                for (int i = 0; i < lastResultArray.Length; i++)
                {
                    var position_i = lastResultArray[i].position;
                    position_i.CopyTo(positions2d[i]);
                }

                return new double[size];
            }

            private static double[] NewDistances(int particleCount)
            {
                return new double[DistanceCache.GetCombinationLength(particleCount)];
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

        public IParticleLeniaInitializer Initializer { get; set; }

        public string Description => "Particle Lenia original simulation";

        public int CurrentStep { get; private set; }
        public int ParticleCount => source.ParticleCount;
        public int Dimensions => source.Dimensions;
        public double DeltaTime => source.DeltaTime;
        public double CurrentTime => CurrentStep * DeltaTime;

        private Span2D<double> Positions => source.Positions;
        private DistanceCache Distances => source.Distances;

        public ParticleInfoArray GetResult()
        {
            return source.lastResult;
        }

        public DistanceCache LastDistances => source.Distances;

        public void Next(int step, IParticleLenia.PostParticleAction intermediatePostAction, IParticleLenia.PostParticleAction lastPostAction, IParticleLenia.PostStepAction postStepAction)
        {
            if (step <= 0) throw new ArgumentOutOfRangeException(nameof(step));

            if (source == null)
            {
                source = new CulcSource(Initializer);
                CurrentStep = 0;
            }

            for (int i = 0; i < step - 1; i++)
            {
                Step(intermediatePostAction);
                postStepAction?.Invoke();
            }

            Step(lastPostAction);
            postStepAction?.Invoke();
        }

        public void Init(IParticleLenia.PostParticleAction postAction)
        {
            Control.UseBestProviders();

            source = new CulcSource(Initializer);
            CurrentStep = 0;

            var distances = Distances;
            var lastResult = source.lastResult.Array;
            for (int i = 0; i < lastResult.Length; i++)
            {
                var pInfo = lastResult[i];
                GetUtAndRt(out var ut, out var rt, lastResult, distances, i);
                var gt = pInfo.fields.G(ut);
                pInfo.lastGt = gt;
                pInfo.lastRt = rt;
                pInfo.lastUt = ut;
                postAction?.Invoke(pInfo, i);
            }

            source.lastResult.Step = CurrentStep;
            source.lastResult.Time = CurrentTime;
        }

        private void Step(IParticleLenia.PostParticleAction postAction)
        {
            const int BLOCK_SIZE_MIN = 10;
            var nextResult = source.tempResult;
            var lastResult = source.lastResult;

            var positions = Positions;
            var distances = Distances;
            distances.Reset();

            if (postAction == null)
            {
                if (ParticleCount < ThreadCount * BLOCK_SIZE_MIN)
                {
                    for (int i = 0; i < ParticleCount; i++)
                    {
                        StepForParticle(i, nextResult, lastResult, positions, distances);
                    }
                }
                else
                {
                    ParallelStep(StepForParticle);
                }
            }
            else
            {
                if (ParticleCount < ThreadCount * BLOCK_SIZE_MIN)
                {
                    for (int i = 0; i < ParticleCount; i++)
                    {
                        var nextInfo = StepForParticle(i, nextResult, lastResult, positions, distances);
                        postAction.Invoke(nextInfo, i);
                    }
                }
                else
                {
                    ParallelStep((start, end) => StepForParticle(start, end, postAction));
                }
            }

            source.lastResult = nextResult;
            source.tempResult = lastResult;

            CurrentStep++;

            source.lastResult.Step = CurrentStep;
            source.lastResult.Time = CurrentTime;
        }

        private void ParallelStep(Action<int, int> stepForParticle)
        {
            int block = ParticleCount / ThreadCount;
            int next = 0;
            for (int i = 0; i < ThreadCount - 1; i++)
            {
                var end = next + block;
                var _next = next;
                source.threads[i] = Task.Run(() =>
                {
                    try
                    {
                        stepForParticle(_next, end);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                });

                next = end;
            }

            if (next < ParticleCount)
            {
                source.threads[^1] = Task.Run(() =>
                {
                    try
                    {
                        stepForParticle(next, ParticleCount);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                });
            }

            Task.WhenAll(source.threads).Wait();
        }

        private void StepForParticle(int start, int end)
        {
            var nextResult = source.tempResult;
            var lastResult = source.lastResult;
            var positions = Positions;
            var distances = Distances;

            for (int i = start; i < end; i++)
            {
                StepForParticle(i, nextResult, lastResult, positions, distances);
            }
        }

        private void StepForParticle(int start, int end, IParticleLenia.PostParticleAction postAction)
        {
            var nextResult = source.tempResult;
            var lastResult = source.lastResult;
            var positions = Positions;
            var distances = Distances;

            for (int i = start; i < end; i++)
            {
                var nextInfo = StepForParticle(i, nextResult, lastResult, positions, distances);
                postAction.Invoke(nextInfo, i);
            }
        }

        private ParticleInfo StepForParticle(int index, ParticleInfoArray nextResult, ParticleInfoArray lastResult, Span2D<double> positions, DistanceCache distances)
        {
            var lastInfo = lastResult[index];
            var lastPosition = lastInfo.position;
            var nextInfo = nextResult[index];
            var nextPosition = nextInfo.position;

            GetUtAndRt(out var ut, out var rt, lastResult.Array, distances, index);
            var gt = lastInfo.fields.G(ut);
            nextInfo.lastGt = gt;
            nextInfo.lastRt = rt;
            nextInfo.lastUt = ut;

            PositionUpdate(lastPosition, nextPosition, lastInfo.fields);
            nextPosition.CopyTo(positions[index]);

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

        private void GetUtAndRt(out double ut, out double rt, Span<ParticleInfo> source, ReadOnlySpan<double> point)
        {
            ut = 0.0;
            rt = 0.0;

            for (int i = 0; i < source.Length; i++)
            {
                var fields = source[i].fields;
                var position = source[i].position;
                var distance = DistanceCache.Distance(position, point);
                ut += fields.K(distance);
                rt += fields.R(distance);
            }
        }

        private void GetUtAndRt(out double ut, out double rt, Span<ParticleInfo> source, DistanceCache vr, int index)
        {
            ut = 0.0;
            rt = 0.0;

            int i;
            for (i = 0; i < index; i++)
            {
                var fields = source[i].fields;
                var distance = vr.Distance(i, index);
                ut += fields.K(distance);
                rt += fields.R(distance);
            }

            for (i = index + 1; i < source.Length; i++)
            {
                var fields = source[i].fields;
                var distance = vr.Distance(i, index);
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