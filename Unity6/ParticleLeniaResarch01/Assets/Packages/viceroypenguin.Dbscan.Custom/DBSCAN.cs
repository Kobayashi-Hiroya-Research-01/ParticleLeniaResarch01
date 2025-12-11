// This code was copied from https://github.com/viceroypenguin/DBSCAN
// and edited by this project.

using System;
using System.Collections.Generic;
using System.Linq;
using XCharts.Runtime;

namespace Assets.Packages.viceroypenguin.Dbscan.Custom
{
    public static class Dbscan
    {
        public interface IOption
        {
            public double Epsilon { get; set; }
            public int MinimumPointsPerCluster { get; set; }

            public void CopyTo(IOption other)
            {
                other.Epsilon = Epsilon;
                other.MinimumPointsPerCluster = MinimumPointsPerCluster;
            }
        }
    }

    /// <summary>
    /// Contains static methods to run the DBSCAN algorithm.
    /// </summary>
    public class Dbscan<TPoint, TCluster> : Dbscan.IOption
        where TPoint : IPointData
        where TCluster : Cluster<TPoint>, new()
    {
        private readonly ListSpatialIndex<TPoint> pointInfos;
        private readonly Queue<PointInfo<TPoint>> queue;

        public Dbscan()
        {
            pointInfos = new ListSpatialIndex<TPoint>();
            queue = new Queue<PointInfo<TPoint>>();
        }

        public Dbscan(Dbscan.IOption option) : this()
        {
            option.CopyTo(this);
        }

        public double Epsilon { get; set; }
        public int MinimumPointsPerCluster { get; set; }

        public void AddPoint(TPoint data)
        {
            pointInfos.List.Add(new PointInfo<TPoint>(data));
        }

        public ClusterSet<TPoint, TCluster> CalculateClusters(Action<TPoint, TCluster> onClustered)
        {
            queue.Clear();
            pointInfos.Reset();

            var points = pointInfos.List;
            var clusters = new Dictionary<int, TCluster>(points.Count);

            int clusterIndex = 0;

            foreach (var pointInfo in points)
            {
                if (pointInfo.Visited) continue;

                pointInfo.Visited = true;
                queue.Enqueue(pointInfo);
                pointInfos.Search(pointInfo.Item, Epsilon, OnFind);

                if (queue.Count >= MinimumPointsPerCluster)
                {
                    var cluster = BuildCluster(clusterIndex++, pointInfo, onClustered);
                    clusters.Add(cluster.ClusterID.Value, cluster);
                }
                else
                {
                    queue.Clear();
                }
            }

            var unclusteredObjects = new List<TPoint>(points.Count - clusters.Count);
            var unclusteredObjectsCluster = new TCluster();
            unclusteredObjectsCluster.Init(ClusterID.UnclusteredID, unclusteredObjects);

            foreach (var pointInfo in points)
            {
                if (!pointInfo.Clustered)
                {
                    onClustered?.Invoke(pointInfo.Item, unclusteredObjectsCluster);
                    unclusteredObjects.Add(pointInfo.Item);
                }
                else
                {
                    pointInfo.Clustered = false;
                }

                pointInfo.Visited = false;
            }

            return new ClusterSet<TPoint, TCluster>(clusters, unclusteredObjectsCluster);
        }

        public void Reset()
        {
            pointInfos.Clear();
            queue.Clear();
        }

        private TCluster BuildCluster(int index, PointInfo<TPoint> corePointInfo, Action<TPoint, TCluster> onClustered)
        {
            int clusterIDValue = index;
            var clusterID = new ClusterID(clusterIDValue);
            var objects = new List<TPoint>();
            var cluster = new TCluster();
            cluster.Init(clusterID, objects);

            corePointInfo.Clustered = true;
            onClustered?.Invoke(corePointInfo.Item, cluster);
            objects.Add(corePointInfo.Item);

            while (queue.Count > 0)
            {
                var newPoint = queue.Dequeue();

                if (!newPoint.Visited)
                {
                    newPoint.Visited = true;

                    pointInfos.Search(newPoint.Item, Epsilon, OnFind);
                }

                if (!newPoint.Clustered)
                {
                    newPoint.Clustered = true;
                    onClustered?.Invoke(newPoint.Item, cluster);
                    objects.Add(newPoint.Item);
                }
            }

            return cluster;
        }

        private void OnFind(PointInfo<TPoint> pInfo)
        {
            if (!pInfo.Visited)
            {
                queue.Enqueue(pInfo);
            }
        }
    }
}