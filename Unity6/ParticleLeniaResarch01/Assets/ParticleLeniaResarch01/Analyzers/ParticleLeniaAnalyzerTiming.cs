namespace Assets.ParticleLeniaResarch01.Analyzers
{
    public enum ParticleLeniaAnalyzerTiming : byte
    {
        None = 0,
        ForeachCreated = 1,
        ForeachUpdated = 2,
        ForeachAfterStep = 4,
        OnlyLastAfterStep = 8,
        LastAfterStep = ForeachAfterStep | OnlyLastAfterStep,
    }
}