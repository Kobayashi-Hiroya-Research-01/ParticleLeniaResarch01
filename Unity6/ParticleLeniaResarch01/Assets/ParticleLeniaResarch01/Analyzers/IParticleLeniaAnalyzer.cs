using Assets.ParticleLeniaResarch01.Scripts;
using System;
using System.Collections.Generic;

namespace Assets.ParticleLeniaResarch01.Analyzers
{
    public interface IParticleLeniaAnalyzer
    {
        public bool Enabled { get; set; }

        public void Reset();

        public void SetupDependencies(ParticleLeniaAnalyzer dependencies, IParticleLenia particleLenia);

        public ReadOnlySpan<ParticleLeniaAnalyzerElement> GetAnalyzerElements();

        public void AppendTo(ParticleLeniaAnalyzerTiming timing, List<ParticleLeniaAnalyzerEvent> loopStartList, List<ParticleLeniaAnalyzerEvent> loopEndList, List<ParticleLeniaAnalyzerForeachEvent> foreachList)
        {
            var elements = GetAnalyzerElements();
            foreach (var element in elements)
            {
                element.AppendTo(timing, loopStartList, loopEndList, foreachList);
            }
        }
    }
}