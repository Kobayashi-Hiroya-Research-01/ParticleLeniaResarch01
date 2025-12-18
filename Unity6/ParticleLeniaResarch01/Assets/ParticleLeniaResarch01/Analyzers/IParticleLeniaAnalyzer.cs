using Assets.ParticleLeniaResarch01.Scripts;
using System;
using System.Collections.Generic;

namespace Assets.ParticleLeniaResarch01.Analyzers
{
    public interface IParticleLeniaAnalyzer
    {
        bool Enabled { get; set; }

        void Reset();

        void SetupDependencies(ParticleLeniaAnalyzer dependencies, IParticleLenia particleLenia);

        ReadOnlySpan<ParticleLeniaAnalyzerElement> GetAnalyzerElements();

        void AppendTo(ParticleLeniaAnalyzerTiming timing, List<ParticleLeniaAnalyzerEvent> loopStartList, List<ParticleLeniaAnalyzerEvent> loopEndList, List<ParticleLeniaAnalyzerForeachEvent> foreachList)
        {
            var elements = GetAnalyzerElements();
            foreach (var element in elements)
            {
                element.AppendTo(timing, loopStartList, loopEndList, foreachList);
            }
        }
    }
}