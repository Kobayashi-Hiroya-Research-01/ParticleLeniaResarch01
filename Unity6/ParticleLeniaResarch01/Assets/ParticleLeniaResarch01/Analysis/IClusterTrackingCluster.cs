using Assets.Packages.viceroypenguin.Dbscan.Custom;

namespace Assets.ParticleLeniaResarch01.Analysis
{
    public interface IClusterTrackingCluster : ICluster
    {
        public int LifeTime { get; }
    }
}