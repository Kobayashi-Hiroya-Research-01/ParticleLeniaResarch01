using Assets.ParticleLeniaResarch01.Scripts;
using Assets.ParticleLeniaResarch01.Scripts.Data;
using Assets.ParticleLeniaResarch01.Scripts.MathObjects;
using System;

namespace Assets.ParticleLeniaResarch01.Analyzers
{
    public class MinMaxAnalyzer : IParticleLeniaAnalyzer
    {
        private readonly MinMax.Writerble eMinMax = new();
        private readonly MinMax.Writerble rtMinMax = new();
        private readonly MinMax.Writerble gtMinMax = new();
        private readonly MinMax.Writerble utMinMax = new();
        private readonly MinMax.Writerble xMinMax = new();
        private readonly MinMax.Writerble yMinMax = new();
        private readonly ParticleLeniaAnalyzerElement[] elements;

        public MinMaxAnalyzer()
        {
            elements = new ParticleLeniaAnalyzerElement[]
            {
                new(timing: ParticleLeniaAnalyzerTiming.ForeachUpdated, loopStart: LoopStart, @foreach: Foreach),
            };
        }

        public MinMax EMinMax => eMinMax;
        public MinMax RtMinMax => rtMinMax;
        public MinMax GtMinMax => gtMinMax;
        public MinMax UtMinMax => utMinMax;
        public MinMax XMinMax => xMinMax;
        public MinMax YMinMax => yMinMax;

        public bool Enabled { get; set; } = true;

        public void Reset()
        {
            eMinMax.Reset();
            rtMinMax.Reset();
            gtMinMax.Reset();
            utMinMax.Reset();
            xMinMax.Reset();
            yMinMax.Reset();
        }

        public void SetupDependencies(ParticleLeniaAnalyzer dependencies, IParticleLenia particleLenia)
        {
        }

        public ReadOnlySpan<ParticleLeniaAnalyzerElement> GetAnalyzerElements()
        {
            return elements;
        }

        public void LoopStart(ParticleInfoArray particles)
        {
            eMinMax.Reset();
            rtMinMax.Reset();
            gtMinMax.Reset();
            utMinMax.Reset();
            xMinMax.Reset();
            yMinMax.Reset();
        }

        public void Foreach(ParticleInfo info, int index)
        {
            eMinMax.Update(info.LastE);
            rtMinMax.Update(info.lastRt);
            gtMinMax.Update(info.lastGt);
            utMinMax.Update(info.lastUt);
            xMinMax.Update(info.position[0]);
            yMinMax.Update(info.position[1]);
        }

        public void RemovePoints(IParticleLenia particleLenia, ReadOnlySpan<int> pointIndexs)
        {
        }
    }
}