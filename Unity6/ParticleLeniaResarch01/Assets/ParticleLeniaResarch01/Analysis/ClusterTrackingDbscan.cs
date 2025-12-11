using Assets.Packages.viceroypenguin.Dbscan.Custom;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace Assets.ParticleLeniaResarch01.Analysis
{
    public static class ClusterTrackingDbscan
    {
        public interface IOption
        {
            public Dbscan.IOption DbscanOption { get; set; }

            public ClusterTrackingMode TrackingMode { get; set; }
            public int StartIndex { get; set; }
            public double ContinuousRatio { get; set; }

            public void CopyTo(IOption other)
            {
                DbscanOption.CopyTo(other.DbscanOption);
                other.TrackingMode = TrackingMode;
                other.StartIndex = StartIndex;
                other.ContinuousRatio = ContinuousRatio;
            }
        }
    }

    public class ClusterTrackingDbscan<TPoint> : ClusterTrackingDbscan.IOption, IDbscan<TPoint, ClusterTrackingCluster<TPoint>>
        where TPoint : IClusterTrackingPointData
    {
        private readonly Dbscan<TPoint, ClusterTrackingCluster<TPoint>> dbscan;
        private ClusterSet<TPoint, ClusterTrackingCluster<TPoint>> oldSet;

        public ClusterTrackingDbscan()
        {
            dbscan = new Dbscan<TPoint, ClusterTrackingCluster<TPoint>>();
            var clusters = new Dictionary<int, ClusterTrackingCluster<TPoint>>();
            var unclusteredObjectsCluster = new ClusterTrackingCluster<TPoint>(ClusterID.UnclusteredID);
            oldSet = new ClusterSet<TPoint, ClusterTrackingCluster<TPoint>>(clusters, unclusteredObjectsCluster);
        }

        public ClusterTrackingDbscan(ClusterTrackingDbscan.IOption option) : this()
        {
            option.CopyTo(this);
        }

        public void Reset()
        {
            dbscan.Reset();
            var clusters = new Dictionary<int, ClusterTrackingCluster<TPoint>>();
            var unclusteredObjectsCluster = new ClusterTrackingCluster<TPoint>(ClusterID.UnclusteredID);
            oldSet = new ClusterSet<TPoint, ClusterTrackingCluster<TPoint>>(clusters, unclusteredObjectsCluster);
        }

        public Dbscan.IOption DbscanOption
        {
            get => dbscan;
            set => value.CopyTo(dbscan);
        }

        public ClusterTrackingMode TrackingMode { get; set; }
        public int StartIndex { get; set; }
        public double ContinuousRatio { get; set; }

        public Dbscan<TPoint, ClusterTrackingCluster<TPoint>> Dbscan => dbscan;

        public ClusterSet<TPoint, ClusterTrackingCluster<TPoint>> LastResult => oldSet;

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

        public void AddPoint(TPoint data)
        {
            Dbscan.AddPoint(data);
        }

        public ClusterSet<TPoint, ClusterTrackingCluster<TPoint>> CalculateClusters(Action<TPoint, ClusterTrackingCluster<TPoint>> onClustered)
        {
            var set = Dbscan.CalculateClusters((p, c) => { OnClustered(p, c); onClustered(p, c); });

            switch (TrackingMode)
            {
                case ClusterTrackingMode.Continuous_FowlkesMallowsLike:
                    SetIDByContinuousTracking(set, FowlkesMallowsLikeCondition);
                    break;

                case ClusterTrackingMode.Continuous_FowlkesMallowsIndex:
                    SetIDByContinuousTracking(set, FowlkesMallowsIndexCondition);
                    break;

                case ClusterTrackingMode.OrderByCount:
                    SetIDByOrderByCount(set);
                    break;

                case ClusterTrackingMode.None:
                    break;

                default:
                    throw new NotImplementedException();
            }

            oldSet = set;

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

        private void SetIDByContinuousTracking(ClusterSet<TPoint, ClusterTrackingCluster<TPoint>> clusters, Func<double, double, bool> condition)
        {
            var trackedClusters = new Dictionary<int, ClusterTrackingCluster<TPoint>>();
            var usedIDList = new HashSet<int>(oldSet.Keys);

            foreach (var cluster in clusters)
            {
                // 条件を満たすもっとも量が多い旧クラスタをトラッキング
                var trackedID = cluster.Value.OldIDCount
                    .Select(oldIdCount =>
                    {
                        int oldId = oldIdCount.Key;

                        //新旧クラスタ一致量
                        int sameCount = oldIdCount.Value;

                        double p, r;

                        if (oldSet.TryGetValue(oldId, out var oldCluster))
                        {
                            //旧クラスタ割合
                            p = (double)sameCount / oldCluster.Count;
                            //新クラスタ割合
                            r = (double)sameCount / cluster.Value.Count;
                        }
                        else
                        {
                            //旧クラスタ割合
                            p = 0;
                            //新クラスタ割合
                            r = sameCount / cluster.Value.Count;
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
                    id = GetNewID(usedIDList);
                    lifeTime = 1;
                }
                else if (trackedClusters.TryGetValue(trackedIDValue, out var sisterCluster))
                {
                    if (sisterCluster.ClusterID.Value == trackedIDValue)
                    {
                        UpdateClusterInfo(sisterCluster, GetNewID(usedIDList), 1);
                    }

                    id = GetNewID(usedIDList);
                    lifeTime = 1;
                }
                else
                {
                    trackedClusters.Add(trackedIDValue, cluster.Value);

                    id = trackedIDValue;
                    lifeTime = oldSet[trackedIDValue].LifeTime + 1;
                }

                UpdateClusterInfo(cluster.Value, id, lifeTime);
            }

            clusters.UpdateDict();
        }

        private void SetIDByOrderByCount(ClusterSet<TPoint, ClusterTrackingCluster<TPoint>> clusters)
        {
            int i = StartIndex;

            foreach (var cluster in clusters.OrderByDescending(e => e.Value.Count))
            {
                var id = i++;
                UpdateClusterInfo(cluster.Value, id, 1);
            }

            clusters.UpdateDict();
        }

        private void UpdateClusterInfo(ClusterTrackingCluster<TPoint> cluster, int newIDValue, int lifeTime)
        {
            cluster.LifeTime = lifeTime;
            cluster.ClusterID.UpdateValue(newIDValue);
        }

        private int GetNewID(HashSet<int> usedIDList)
        {
            for (int i = StartIndex + 1; i < int.MaxValue; i++)
            {
                if (usedIDList.Add(i))
                {
                    return i;
                }
            }

            throw new NotSupportedException();
        }
    }
}