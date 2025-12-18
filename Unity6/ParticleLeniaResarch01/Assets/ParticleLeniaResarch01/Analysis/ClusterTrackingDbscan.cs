using Assets.Packages.viceroypenguin.Dbscan.Custom;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets.ParticleLeniaResarch01.Analysis
{
    using Cluster = ClusterTrackingCluster<ClusterTrackingPoint>;
    using Point = ClusterTrackingPoint;

    public class ClusterTrackingDbscan : ClusterTrackingDbscan.IState, IDbscan<Point, Cluster>
    {
        public interface IOption
        {
            Dbscan.IOption DbscanOption { get; }
            ClusterTrackingMode TrackingMode { get; set; }
            int StartIndex { get; set; }
            double ContinuousRatio { get; set; }

            void CopyTo(IOption other)
            {
                DbscanOption.CopyTo(other.DbscanOption);
                other.TrackingMode = TrackingMode;
                other.StartIndex = StartIndex;
                other.ContinuousRatio = ContinuousRatio;
            }
        }

        public interface IState : IOption
        {
            int LastID { get; set; }

            Dbscan<Point, Cluster>.IState DbscanState { get; }
            ClusterSet<Point, Cluster> LastResult { get; }

            void CopyTo(IState other)
            {
                AsOption.CopyTo(other);
                //DbscanState.CopyTo(other.DbscanState);
                //LastResult.CopyTo(other.LastResult);
                other.LastID = LastID;
            }

            IOption AsOption => this;
        }

        private readonly Dbscan<Point, Cluster> dbscan;
        private ClusterSet<Point, Cluster> lastResult;

        public ClusterTrackingDbscan()
        {
            dbscan = new Dbscan<Point, Cluster>();
            var clusters = new Dictionary<int, Cluster>();
            var unclusteredObjectsCluster = new Cluster(ClusterID.UnclusteredID);
            lastResult = new ClusterSet<Point, Cluster>(clusters, unclusteredObjectsCluster);
            LastID = -1;
        }

        public ClusterTrackingMode TrackingMode { get; set; }
        public int StartIndex { get; set; }
        public double ContinuousRatio { get; set; }
        public int LastID { get; set; }

        public void Reset()
        {
            dbscan.Reset();
            var clusters = new Dictionary<int, Cluster>();
            var unclusteredObjectsCluster = new Cluster(ClusterID.UnclusteredID);
            lastResult = new ClusterSet<Point, Cluster>(clusters, unclusteredObjectsCluster);
            LastID = StartIndex;
        }

        public IOption AsOption => this;

        public IState AsState => this;

        public Dbscan.IOption DbscanOption => dbscan;

        public Dbscan<Point, Cluster> Dbscan => dbscan;

        public ClusterSet<Point, Cluster> LastResult => lastResult;

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

        public Dbscan<Point, Cluster>.IState DbscanState => dbscan;

        public void AddPoint(Point data)
        {
            Dbscan.AddPoint(data);
        }

        public ClusterSet<Point, Cluster> CalculateClusters(Action<Point, Cluster> onClustered)
        {
            var set = CalculateClusters();

            if (onClustered != null)
            {
                foreach (var cluster in set.Values)
                {
                    foreach (var point in cluster)
                    {
                        onClustered(point, cluster);
                    }
                }

                foreach (var point in set.UnclusteredObjects)
                {
                    onClustered(point, set.UnclusteredObjects);
                }
            }

            return set;
        }

        public ClusterSet<Point, Cluster> CalculateClusters()
        {
            var set = Dbscan.CalculateClusters(OnClustered);

            SetIdByTrackingMode(set, TrackingMode);
            lastResult = set;

            return set;
        }

        private void SetIdByTrackingMode(ClusterSet<Point, Cluster> set, ClusterTrackingMode trackingMode)
        {
            switch (trackingMode)
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

        private void OnClustered(Point point, Cluster cluster)
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

        private readonly Dictionary<int, Cluster> trackedClusters = new();

        private void SetIDByContinuousTracking(ClusterSet<Point, Cluster> clusters, Func<double, double, bool> condition)
        {
            trackedClusters.Clear();

            foreach (var cluster in clusters.Values)
            {
                // 条件を満たすもっとも量が多い旧クラスタをトラッキング
                var trackedID = cluster.OldIDCount
                    .Select(oldIdCount =>
                    {
                        int oldId = oldIdCount.Key;

                        //新旧クラスタ一致量
                        int sameCount = oldIdCount.Value;

                        double p, r;

                        if (lastResult.TryGetValue(oldId, out var oldCluster))
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
                    id = GetNewID();
                    lifeTime = 1;
                }
                else if (trackedClusters.TryGetValue(trackedIDValue, out var sisterCluster))
                {
                    if (sisterCluster.ClusterID.Value == trackedIDValue)
                    {
                        UpdateClusterInfo(sisterCluster, GetNewID(), 1);
                    }

                    id = GetNewID();
                    lifeTime = 1;
                }
                else
                {
                    trackedClusters.Add(trackedIDValue, cluster);

                    id = trackedIDValue;
                    lifeTime = lastResult[trackedIDValue].LifeTime + 1;
                }

                UpdateClusterInfo(cluster, id, lifeTime);
                cluster.OldIDCount.Clear();
            }

            clusters.UpdateIDs();
        }

        private void SetIDByOrderByCount(ClusterSet<Point, Cluster> clusters)
        {
            int i = StartIndex;

            foreach (var cluster in clusters.Values.ToArray().OrderByDescending(e => e.Count))
            {
                var id = i++;
                UpdateClusterInfo(cluster, id, 1);
            }

            clusters.UpdateIDs();
        }

        private void UpdateClusterInfo(Cluster cluster, int newIDValue, int lifeTime)
        {
            cluster.LifeTime = lifeTime;
            cluster.ClusterID.UpdateValue(newIDValue);
        }

        private int GetNewID()
        {
            if (LastID < StartIndex)
            {
                LastID = StartIndex;
            }
            else if (LastID < int.MaxValue)
            {
                LastID++;
            }
            else
            {
                throw new NotSupportedException();
            }

            return LastID;
        }
    }
}