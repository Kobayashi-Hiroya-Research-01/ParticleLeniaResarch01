using Assets.Packages.viceroypenguin.Dbscan.Custom;

namespace Assets.ParticleLeniaResarch01.Analysis
{
    public interface IClusterTrackingPointData : IPointData
    {
        ClusterID ClusterID { get; set; }
    }
}