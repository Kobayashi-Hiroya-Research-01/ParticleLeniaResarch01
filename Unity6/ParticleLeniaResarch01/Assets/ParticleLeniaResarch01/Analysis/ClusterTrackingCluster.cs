using Assets.Packages.viceroypenguin.Dbscan.Custom;
using System.Collections.Generic;

namespace Assets.ParticleLeniaResarch01.Analysis
{
    public class ClusterTrackingCluster<TPoint> : Cluster<TPoint>, IClusterTrackingCluster
        where TPoint : IClusterTrackingPointData
    {
        public ClusterTrackingCluster()
        {
        }

        public ClusterTrackingCluster(ClusterID clusterID)
        {
            Init(clusterID, new());
        }

        public int LifeTime { get; set; } = 0;
        public Dictionary<int, int> OldIDCount { get; } = new Dictionary<int, int>();
    }
}