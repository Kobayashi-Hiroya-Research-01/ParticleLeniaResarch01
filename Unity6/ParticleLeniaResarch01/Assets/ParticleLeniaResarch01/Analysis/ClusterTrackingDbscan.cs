using Assets.Packages.viceroypenguin.Dbscan.Custom;
using Assets.ParticleLeniaResarch01.Scripts.Data;
using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Unity.VisualScripting;
using UnityEngine;

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

        public void Reset()
        {
            dbscan.Reset();
            var clusters = new Dictionary<int, Cluster>();
            var unclusteredObjectsCluster = new Cluster(ClusterID.UnclusteredID);
            lastResult = new ClusterSet<Point, Cluster>(clusters, unclusteredObjectsCluster);
            LastID = StartIndex;
        }

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
                case ClusterTrackingMode.None:
                    break;

                case ClusterTrackingMode.OrderByCount:
                    SetIDByOrderByCount(set);
                    break;

                case ClusterTrackingMode.FowlkesMallowsIndex:
                    SetIDByContinuousTracking(set, FowlkesMallowsIndexCondition);
                    break;

                case ClusterTrackingMode.ElementWiseFowlkesMallowsIndex:
                    SetIDByContinuousTracking(set, ElementWiseFowlkesMallowsIndexCondition);
                    break;

                case ClusterTrackingMode.F1:
                    SetIDByContinuousTracking(set, F1Condition);
                    break;

                case ClusterTrackingMode.F0_5:
                    SetIDByContinuousTracking(set, F0_5Condition);
                    break;

                case ClusterTrackingMode.F2:
                    SetIDByContinuousTracking(set, F2Condition);
                    break;

                case ClusterTrackingMode.PairwiseF1:
                    SetIDByContinuousTracking(set, PairwiseF1Condition);
                    break;

                case ClusterTrackingMode.PairwiseF0_5:
                    SetIDByContinuousTracking(set, PairwiseF0_5Condition);
                    break;

                case ClusterTrackingMode.PairwiseF2:
                    SetIDByContinuousTracking(set, PairwiseF2Condition);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private bool FowlkesMallowsIndexCondition(int sameCount, int oldCount, int newCount, double continuousRatio)
        {
            if (oldCount < 2 || newCount < 2) return false;

            var tp = (double)PairwiseCount(sameCount);
            var recall = tp / PairwiseCount(oldCount);
            var precision = tp / PairwiseCount(newCount);

            return FowlkesMallowsIndexCondition(recall, precision, continuousRatio * continuousRatio);
        }

        private bool ElementWiseFowlkesMallowsIndexCondition(int sameCount, int oldCount, int newCount, double continuousRatio)
        {
            if (oldCount <= 0 || newCount <= 0) return false;

            var tp = (double)sameCount;
            var recall = tp / oldCount;
            var precision = tp / newCount;

            return FowlkesMallowsIndexCondition(recall, precision, continuousRatio);
        }

        private bool F1Condition(int sameCount, int oldCount, int newCount, double continuousRatio)
        {
            if (oldCount <= 0 || newCount <= 0) return false;

            var tp = (double)sameCount;
            var recall = tp / oldCount;
            var precision = tp / newCount;

            return F1Condition(recall, precision, continuousRatio);
        }

        private bool PairwiseF1Condition(int sameCount, int oldCount, int newCount, double continuousRatio)
        {
            if (oldCount < 2 || newCount < 2) return false;

            var tp = (double)PairwiseCount(sameCount);
            var recall = tp / PairwiseCount(oldCount);
            var precision = tp / PairwiseCount(newCount);

            return F1Condition(recall, precision, continuousRatio * continuousRatio);
        }

        private bool F2Condition(int sameCount, int oldCount, int newCount, double continuousRatio)
        {
            if (oldCount <= 0 || newCount <= 0) return false;

            var tp = (double)sameCount;
            var recall = tp / oldCount;
            var precision = tp / newCount;

            return F2Condition(recall, precision, continuousRatio);
        }

        private bool PairwiseF2Condition(int sameCount, int oldCount, int newCount, double continuousRatio)
        {
            if (oldCount < 2 || newCount < 2) return false;

            var tp = (double)PairwiseCount(sameCount);
            var recall = tp / PairwiseCount(oldCount);
            var precision = tp / PairwiseCount(newCount);

            return F2Condition(recall, precision, continuousRatio * continuousRatio);
        }

        private bool F0_5Condition(int sameCount, int oldCount, int newCount, double continuousRatio)
        {
            if (oldCount <= 0 || newCount <= 0) return false;

            var tp = (double)sameCount;
            var recall = tp / oldCount;
            var precision = tp / newCount;

            return F0_5Condition(recall, precision, continuousRatio);
        }

        private bool PairwiseF0_5Condition(int sameCount, int oldCount, int newCount, double continuousRatio)
        {
            if (oldCount < 2 || newCount < 2) return false;

            var tp = (double)PairwiseCount(sameCount);
            var recall = tp / PairwiseCount(oldCount);
            var precision = tp / PairwiseCount(newCount);

            return F0_5Condition(recall, precision, continuousRatio * continuousRatio);
        }

        private static bool F2Condition(double recall, double precision, double continuousRatio)
        {
            return FBataCondition(recall, precision, 2.0, continuousRatio);
        }

        private static bool F0_5Condition(double recall, double precision, double continuousRatio)
        {
            return FBataCondition(recall, precision, 0.5, continuousRatio);
        }

        private static bool FowlkesMallowsIndexCondition(double recall, double precision, double continuousRatio)
        {
            return recall * precision >= continuousRatio * continuousRatio;
        }

        private static bool F1Condition(double recall, double precision, double continuousRatio)
        {
            return 2.0 * recall * precision >= continuousRatio * (precision + recall);
        }

        private static bool FBataCondition(double recall, double precision, double bata, double continuousRatio)
        {
            var bata2 = bata * bata;
            return (bata2 + 1.0) * recall * precision >= continuousRatio * (bata2 * precision + recall);
        }

        private static int PairwiseCount(int elementCount)
        {
            var k = elementCount;

            return k * (k - 1) >> 1;
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

        private delegate bool TrackingCondition(int sameCount, int oldCount, int newCount, double continuousRatio);

        private void SetIDByContinuousTracking(ClusterSet<Point, Cluster> clusters, TrackingCondition condition)
        {
            trackedClusters.Clear();

            foreach (var cluster in clusters.Values)
            {
                // 条件を満たすもっとも量が多い旧クラスタをトラッキング
                bool CheckCondition(KeyValuePair<int, int> oldIdCount)
                {
                    int oldId, sameCount, oldCount, newCount;
                    oldId = oldIdCount.Key;
                    //新旧クラスタ一致量
                    sameCount = oldIdCount.Value;
                    //旧クラスタ割合
                    oldCount = lastResult.TryGetValue(oldId, out var oldCluster) ? oldCluster.Count : 0;
                    //新クラスタ割合
                    newCount = cluster.Count;

                    return condition(sameCount, oldCount, newCount, ContinuousRatio);
                }

                var trackedIDCount = cluster.OldIDCount
                    .Where(CheckCondition)
                    .OrderByDescending(OrderByValue)
                    .FirstOrDefault();

                cluster.OldIDCount.Clear();

                int trackedID = trackedIDCount.Key;
                int id, lifeTime;

                if (trackedIDCount.Value == 0 || trackedIDCount.Key == ClusterID.UnclusteredID.Value)
                {
                    id = GetNewID();
                    lifeTime = 1;
                }
                else if (trackedClusters.TryGetValue(trackedID, out var sisterCluster))
                {
                    if (sisterCluster.ClusterID.Value == trackedID)
                    {
                        sisterCluster.UpdateClusterInfo(GetNewID(), 1);
                    }

                    id = GetNewID();
                    lifeTime = 1;
                }
                else
                {
                    trackedClusters.Add(trackedID, cluster);

                    id = trackedID;
                    lifeTime = lastResult[trackedID].LifeTime + 1;
                }

                cluster.UpdateClusterInfo(id, lifeTime);
            }

            clusters.UpdateIDs();
        }

        private int OrderByValue(KeyValuePair<int, int> oldIdCount)
        {
            return oldIdCount.Value;
        }

        private void SetIDByOrderByCount(ClusterSet<Point, Cluster> clusters)
        {
            int i = StartIndex;

            foreach (var cluster in clusters.Values.ToArray().OrderByDescending(e => e.Count))
            {
                var id = i++;
                cluster.UpdateClusterInfo(id, 1);
            }

            clusters.UpdateIDs();
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

        internal void CopyIDTo(ClusterTrackingDbscan other)
        {
            other.LastID = LastID;

            var array = lastResult.Values;
            var other_array = other.lastResult.Values;
            for (int i = 0; i < array.Length; i++)
            {
                var item = array[i];
                var otherItem = other_array[i];
                otherItem.UpdateClusterInfo(item.ClusterID.Value, item.LifeTime);
                otherItem.OldIDCount.Clear();

                foreach (var (key, value) in item.OldIDCount)
                {
                    otherItem.OldIDCount[key] = value;
                }
            }

            other.lastResult.UpdateIDs();
        }

        internal void RemovePoints(ReadOnlySpan<int> pointIndexs)
        {
            Dbscan.RemovePoints(pointIndexs);
            lastResult.RemovePoints(pointIndexs);
        }
    }
}