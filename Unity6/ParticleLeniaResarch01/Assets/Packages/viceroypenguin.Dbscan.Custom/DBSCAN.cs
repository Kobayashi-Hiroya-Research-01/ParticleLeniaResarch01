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
    public class Dbscan<TPoint, TCluster> : Dbscan.IOption, IDbscan<TPoint, TCluster>
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
            pointInfos.Add(new PointInfo<TPoint>(data));
        }

        public ClusterSet<TPoint, TCluster> CalculateClusters()
        {
            queue.Clear();
            pointInfos.Reset();

            var points = pointInfos.Items;
            var clusters = new Dictionary<int, TCluster>(points.Length);

            int clusterIndex = 0;

            foreach (var pointInfo in points)
            {
                if (pointInfo.Visited) continue;

                pointInfo.Visited = true;
                queue.Enqueue(pointInfo);
                SearchAndEnqueue(pointInfo.Item);

                if (queue.Count >= MinimumPointsPerCluster)
                {
                    var cluster = BuildCluster(clusterIndex++, pointInfo, points.Length);
                    clusters.Add(cluster.ClusterID.Value, cluster);
                }
                else
                {
                    queue.Clear();
                }
            }

            var unclusteredObjects = new List<TPoint>(points.Length - clusters.Count);
            var unclusteredObjectsCluster = new TCluster();
            unclusteredObjectsCluster.Init(ClusterID.UnclusteredID, unclusteredObjects);

            foreach (var pointInfo in points)
            {
                if (!pointInfo.Clustered)
                {
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

        public ClusterSet<TPoint, TCluster> CalculateClusters(Action<TPoint, TCluster> onClustered)
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

        public void Reset()
        {
            pointInfos.Clear();
            queue.Clear();
        }

        private TCluster BuildCluster(int index, PointInfo<TPoint> corePointInfo, int capacity)
        {
            int clusterIDValue = index;
            var clusterID = new ClusterID(clusterIDValue);
            var objects = new List<TPoint>(capacity: capacity);
            var cluster = new TCluster();
            cluster.Init(clusterID, objects);

            corePointInfo.Clustered = true;
            objects.Add(corePointInfo.Item);

            while (queue.Count > 0)
            {
                var newPoint = queue.Dequeue();

                if (!newPoint.Visited)
                {
                    newPoint.Visited = true;
                    SearchAndEnqueue(newPoint.Item);
                }

                if (!newPoint.Clustered)
                {
                    newPoint.Clustered = true;
                    objects.Add(newPoint.Item);
                }
            }
            objects.TrimExcess();
            return cluster;
        }

        private void SearchAndEnqueue(TPoint point)
        {
            pointInfos.Search(point, Epsilon, Enqueue);
        }

        private void Enqueue(PointInfo<TPoint> pInfo)
        {
            if (!pInfo.Visited)
            {
                queue.Enqueue(pInfo);
            }
        }
    }
}