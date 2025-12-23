using Assets.ParticleLeniaResarch01.Analyzers;
using Assets.ParticleLeniaResarch01.Scripts;

namespace Assets.ParticleLeniaResarch01.Analysis
{
    using ClusteringAnalyzer = ClusterTrackingDbscanAnalyzer;

    public class AutopoieticSystemPotentialArg
    {
        public int step;
        public ParticleLeniaSimulation baseSimulation;
        public ClusteringAnalyzer baseComponentAnalyzer;
        public ClusteringAnalyzer baseUnitAnalyzer;
    }
}