using Assets.Packages.viceroypenguin.Dbscan;
using Assets.Packages.viceroypenguin.Dbscan.Custom;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine.Rendering.Universal;

namespace Assets.ParticleLeniaResarch01.Analysis
{
    public class ClusterTrackingDbscan<TPoint> : IDbscan<TPoint, ClusterTrackingCluster<TPoint>>
        where TPoint : IClusterTrackingPointData
    {
        private ClusterSet<TPoint, ClusterTrackingCluster<TPoint>> oldSet;

        public ClusterTrackingDbscan()
        {
            var clusters = new List<ClusterTrackingCluster<TPoint>>();
            var unclusteredObjectsCluster = new ClusterTrackingCluster<TPoint>();
            unclusteredObjectsCluster.Init(ClusterID.UnclusteredID, new());
            oldSet = new(clusters, unclusteredObjectsCluster);
        }

        public ClusterTrackingMode TrackingMode { get; set; }

        public int NextID { get; set; }

        public int StartIndex { get; set; }

        public double ContinuousRatio { get; set; }

        public double Epsilon
        {
            get => Dbscan.Epsilon;
            set => Dbscan.Epsilon = value;
        }

        public int MinimumPointsPerCluster
        {
            get => Dbscan.MinimumPointsPerCluster;
            set => Dbscan.MinimumPointsPerCluster = value;
        }

        public Dbscan<TPoint, ClusterTrackingCluster<TPoint>> Dbscan { get; } = new Dbscan<TPoint, ClusterTrackingCluster<TPoint>>();

        public void AddPoint(TPoint data)
        {
            Dbscan.AddPoint(data);
        }

        public ClusterSet<TPoint, ClusterTrackingCluster<TPoint>> CalculateClusters(Action<TPoint, ClusterTrackingCluster<TPoint>> onClustered)
        {
            var set = Dbscan.CalculateClusters(OnClustered + onClustered);

            switch (TrackingMode)
            {
                case ClusterTrackingMode.Continuous_FowlkesMallowsLike:
                    SetIDByContinuousTracking(set.Clusters, FowlkesMallowsLikeCondition);
                    break;

                case ClusterTrackingMode.Continuous_FowlkesMallowsIndex:
                    SetIDByContinuousTracking(set.Clusters, FowlkesMallowsIndexCondition);
                    break;

                case ClusterTrackingMode.OrderByCount:
                    SetIDByOrderByCount(set.Clusters);
                    break;

                case ClusterTrackingMode.OrderByLifeTime:
                    SetIDByOrderByLifeTime(set.Clusters);
                    break;

                case ClusterTrackingMode.None:
                    break;

                default:
                    throw new NotImplementedException();
            }

            oldSet = set;
            oldSet.UpdateDict();

            return set;
        }

        private bool FowlkesMallowsLikeCondition(double p, double r)
        {
            return p >= ContinuousRatio     // 旧クラスタ総量の continuousRatio 倍以上が新クラスタに残っている
                && r >= ContinuousRatio;    // 旧クラスタ由来量の continuousRatio 倍以上が旧クラスタ由来である
        }

        private bool FowlkesMallowsIndexCondition(double p, double r)
        {
            return p * r >= ContinuousRatio * ContinuousRatio;
        }

        private void OnClustered(TPoint point, ClusterTrackingCluster<TPoint> cluster)
        {
            var oldID = point.ClusterID.Value;

            if (cluster.OldIDCount.TryGetValue(oldID, out int count))
            {
                cluster.OldIDCount[oldID] = count + 1;
            }
            else
            {
                cluster.OldIDCount[oldID] = 1;
            }

            point.ClusterID = cluster.ClusterID;
        }

        private void SetIDByContinuousTracking(ReadOnlyCollection<ClusterTrackingCluster<TPoint>> clusters, Func<double, double, bool> condition)
        {
            Dictionary<int, ClusterTrackingCluster<TPoint>> trackedClusters = new();

            foreach (var cluster in clusters)
            {
                // 条件を満たすもっとも量が多い旧クラスタをトラッキング
                var trackedID = cluster.OldIDCount
                    .Select(countOld =>
                    {
                        int oldId = countOld.Key;

                        //新旧クラスタ一致量
                        int sameCount = countOld.Value;

                        double p, r;

                        if (oldSet.TryGetValue(oldId, out var oldCluster))
                        {
                            //旧クラスタ割合
                            p = (double)sameCount / oldCluster.Count;
                            //新クラスタ割合
                            r = (double)sameCount / cluster.Count;
                        }
                        else
                        {
                            //旧クラスタ割合
                            p = 0;
                            //新クラスタ割合
                            r = sameCount / cluster.Count;
                        }

                        return (oldId, sameCount, p, r);
                    })
                    .Where(values => condition(values.p, values.r))
                    .OrderByDescending(values => values.sameCount)
                    .FirstOrDefault();

                int trackedIDValue = trackedID.oldId;
                int id, lifeTime;

                if (trackedID.sameCount == 0)
                {
                    id = GetNextID(clusters);
                    lifeTime = 1;
                }
                else if (trackedClusters.TryGetValue(trackedIDValue, out var sisterCluster))
                {
                    if (sisterCluster.ClusterID.Value == trackedIDValue)
                    {
                        UpdateClusterInfo(sisterCluster, GetNextID(clusters), 1);
                    }

                    id = GetNextID(clusters);
                    lifeTime = 1;
                }
                else
                {
                    trackedClusters.Add(trackedIDValue, cluster);

                    id = trackedIDValue;
                    lifeTime = oldSet[trackedIDValue].LifeTime + 1;
                }

                UpdateClusterInfo(cluster, id, lifeTime);
            }
        }

        private void SetIDByOrderByCount(ReadOnlyCollection<ClusterTrackingCluster<TPoint>> clusters)
        {
            int i = StartIndex;

            foreach (var cluster in clusters.OrderByDescending(e => e.Count))
            {
                var id = i++;
                UpdateClusterInfo(cluster, id, cluster.OldIDCount[id] > 0 ? cluster.LifeTime + 1 : 1);
            }

            NextID = i;
        }

        private void SetIDByOrderByLifeTime(ReadOnlyCollection<ClusterTrackingCluster<TPoint>> clusters)
        {
            int i = StartIndex;

            foreach (var cluster in clusters.OrderByDescending(e => e.LifeTime))
            {
                var id = i++;
                UpdateClusterInfo(cluster, id, cluster.OldIDCount[id] > 0 ? cluster.LifeTime + 1 : 1);
            }

            NextID = i;
        }

        private void UpdateClusterInfo(ClusterTrackingCluster<TPoint> cluster, int newIDValue, int lifeTime)
        {
            cluster.LifeTime = lifeTime;
            cluster.ClusterID.UpdateValue(newIDValue);
        }

        private int GetNextID(ReadOnlyCollection<ClusterTrackingCluster<TPoint>> clusters)
        {
            if (NextID > int.MaxValue / 2)
            {
                NextID = 0;
            }

            for (int i = 0; i < clusters.Count; i++)
            {
                var e = clusters[i];
                if (e.ClusterID.Value == NextID)
                {
                    ++NextID;
                    i = 0;
                    continue;
                }
            }

            return NextID++;
        }
    }
}