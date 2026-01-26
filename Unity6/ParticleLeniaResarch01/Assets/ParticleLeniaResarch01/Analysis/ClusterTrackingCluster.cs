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

        public int LifeTime { get; private set; } = 0;

        public Dictionary<int, int> OldIDCount { get; private set; } = new Dictionary<int, int>();

        public void UpdateClusterInfo(int newIDValue, int lifeTime)
        {
            LifeTime = lifeTime;
            ClusterID.UpdateValue(newIDValue);
        }
    }
}