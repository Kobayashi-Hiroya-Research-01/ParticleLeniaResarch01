using System.Collections.Generic;

namespace Assets.ParticleLeniaResarch01.Analyzers
{
    public sealed class ParticleLeniaAnalyzerElement
    {
        public ParticleLeniaAnalyzerElement(ParticleLeniaAnalyzerTiming timing, ParticleLeniaAnalyzerEvent loopStart = null, ParticleLeniaAnalyzerEvent loopEnd = null, ParticleLeniaAnalyzerForeachEvent @foreach = null)
        {
            Timing = timing;
            LoopStart = loopStart;
            LoopEnd = loopEnd;
            Foreach = @foreach;
        }

        public ParticleLeniaAnalyzerTiming Timing { get; }
        public ParticleLeniaAnalyzerEvent LoopStart { get; }
        public ParticleLeniaAnalyzerEvent LoopEnd { get; }
        public ParticleLeniaAnalyzerForeachEvent Foreach { get; }

        public void AppendTo(ParticleLeniaAnalyzerTiming timing, List<ParticleLeniaAnalyzerEvent> loopStartList, List<ParticleLeniaAnalyzerEvent> loopEndList, List<ParticleLeniaAnalyzerForeachEvent> foreachList)
        {
            if (timing == ParticleLeniaAnalyzerTiming.LastAfterStep)
            {
                if (Timing is ParticleLeniaAnalyzerTiming.ForeachAfterStep or ParticleLeniaAnalyzerTiming.OnlyLastAfterStep)
                {
                    if (LoopStart != null) loopStartList.Add(LoopStart);
                    if (LoopEnd != null) loopEndList.Add(LoopEnd);
                    if (Foreach != null) foreachList.Add(Foreach);
                }
            }
            else if (Timing == timing)
            {
                if (LoopStart != null) loopStartList.Add(LoopStart);
                if (LoopEnd != null) loopEndList.Add(LoopEnd);
                if (Foreach != null) foreachList.Add(Foreach);
            }
        }
    }
}