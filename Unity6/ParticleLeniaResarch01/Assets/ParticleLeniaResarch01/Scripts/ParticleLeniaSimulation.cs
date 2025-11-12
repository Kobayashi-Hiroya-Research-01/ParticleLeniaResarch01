using Assets.MathNet.Numerics.Custom;
using Assets.ParticleLenia.Scripts;
using Assets.ParticleLeniaResarch01.Scripts.Fields;
using MathNet.Numerics;
using MathNet.Numerics.Random;
using NUnit.Framework.Internal;
using System;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Assets.ParticleLeniaResarch01.Scripts.Data
{
    //計算見直さないと…
    public sealed class ParticleLeniaSimulation : IParticleLenia, Grad.IValueAtPoint<IParticleFields>
    {
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

        private CulcSource source;
        private readonly Grad grad = new();

        private readonly Task[] multiThreadTasks = new Task[10];

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

        public void Next(int step)
        {
            if (step <= 0) throw new ArgumentOutOfRangeException(nameof(step));

            if (source == null)
            {
                source = new CulcSource(Initializer);
                CurrentStep = 0;
            }

            for (int i = 0; i < step; i++)
            {
                Step();
            }
        }

        public void Init()
        {
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
            }

            source.lastResult.Step = CurrentStep;
            source.lastResult.Time = CurrentTime;
        }

        private void Step()
        {
            var nextResult = source.tempResult;
            var lastResult = source.lastResult;

            var positions = Positions;
            var distances = Distances;
            distances.Reset();

            for (int i = 0; i < ParticleCount; i++)
            {
                var lastInfo = lastResult[i];
                var lastPosition = lastInfo.position;
                var nextInfo = nextResult[i];
                var nextPosition = nextInfo.position;

                GetUtAndRt(out var ut, out var rt, lastResult.Array, distances, i);
                var gt = lastInfo.fields.G(ut);
                nextInfo.lastGt = gt;
                nextInfo.lastRt = rt;
                nextInfo.lastUt = ut;

                PositionUpdate(lastPosition, nextPosition, lastInfo.fields);
                nextPosition.CopyTo(positions[i]);
            }

            source.lastResult = nextResult;
            source.tempResult = lastResult;

            CurrentStep++;

            source.lastResult.Step = CurrentStep;
            source.lastResult.Time = CurrentTime;
        }

        private void Multi()
        {
            var distances = source.distances;

            var nextResult = source.tempResult;
            var lastResult = source.lastResult;
            int block = ParticleCount / multiThreadTasks.Length;
            int start = 0;
            for (int i = 0; i < multiThreadTasks.Length - 1; i++)
            {
                var _start = start;
                var _end = start + block;
                multiThreadTasks[i] = Task.Run(() =>
                {
                    Excute(_start, _end);
                });

                start = _end;
            }

            {
                multiThreadTasks[^1] = Task.Run(() =>
                {
                    Excute(start, ParticleCount);
                });
            }

            Task.WhenAll(multiThreadTasks).Wait();

            void Excute(int start, int end)
            {
                var positions = new Span2D<double>(source.positions, ParticleCount, Dimensions);
                var vr = new DistanceCache(distances, positions);

                for (int i = start; i < end; i++)
                {
                }
            }
        }

        public void PositionUpdate(ReadOnlySpan<double> oldPosition, Span<double> newPosition, IParticleFields particleFields)
        {
            Span<double> dpPosition = stackalloc double[Dimensions];

            grad.GetGrad(dpPosition, oldPosition, this, particleFields);

            for (int i = 0; i < dpPosition.Length; i++)
            {
                newPosition[i] = oldPosition[i] - DeltaTime * dpPosition[i];
            }
        }

        public void GetUtAndRt(out double ut, out double rt, Span<ParticleInfo> source, ReadOnlySpan<double> point)
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

        public void GetUtAndRt(out double ut, out double rt, Span<ParticleInfo> source, DistanceCache vr, int index)
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

        public double GetEnergyAt(ReadOnlySpan<double> point, IParticleFields particleFields)
        {
            GetUtAndRt(out var ut, out var rt, source.lastResult.Array, point);
            return rt - particleFields.G(ut);
        }

        public double GetValueAt(ReadOnlySpan<double> point, IParticleFields particleFields)
        {
            return GetEnergyAt(point, particleFields);
        }
    }
}