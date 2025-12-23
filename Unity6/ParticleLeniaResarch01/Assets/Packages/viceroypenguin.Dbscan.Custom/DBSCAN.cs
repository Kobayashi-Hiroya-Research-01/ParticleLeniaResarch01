// This code was copied from https://github.com/viceroypenguin/DBSCAN
// and edited by this project.

using Assets.ParticleLeniaResarch01.Analysis;
using System;
using System.Collections.Generic;
using XCharts.Runtime;

namespace Assets.Packages.viceroypenguin.Dbscan.Custom
{
    public abstract class Dbscan : Dbscan.IOption
    {
        public double Epsilon { get; set; }
        public int MinimumPointsPerCluster { get; set; }

        public interface IOption
        {
            double Epsilon { get; set; }
            int MinimumPointsPerCluster { get; set; }

            void CopyTo(IOption other)
            {
                other.Epsilon = Epsilon;
                other.MinimumPointsPerCluster = MinimumPointsPerCluster;
            }
        }

        public IOption AsOption => this;
    }

    /// <summary>
    /// Contains static methods to run the DBSCAN algorithm.
    /// </summary>
    public class Dbscan<TPoint, TCluster> : Dbscan, Dbscan<TPoint, TCluster>.IState, IDbscan<TPoint, TCluster>
        where TPoint : IPointData
        where TCluster : Cluster<TPoint>, new()
    {
        public interface IState : IOption
        {
            ListSpatialIndex<TPoint> PointInfos { get; }

            void Reset(List<TPoint> points)
            {
                PointInfos.Clear();
                PointInfos.AddRange(points);
            }

            IOption AsOption => this;
        }

        private readonly ListSpatialIndex<TPoint> pointInfos;
        private readonly Queue<int> queue;

        public Dbscan()
        {
            pointInfos = new();
            queue = new(1000);
        }

        public IState AsState => this;

        public ListSpatialIndex<TPoint> PointInfos => pointInfos;

        public void AddPoint(TPoint data)
        {
            pointInfos.Add(data);
        }

        public void Reset()
        {
            pointInfos.Clear();
            queue.Clear();
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

        public ClusterSet<TPoint, TCluster> CalculateClusters()
        {
            pointInfos.Reset(Epsilon);
            queue.Clear();

            var points = pointInfos.Items;
            var clusters = new Dictionary<int, TCluster>(points.Length);
            var unclusteredObjects = new List<TPoint>(points.Length);
            var unclusteredObjectsCluster = new TCluster();
            unclusteredObjectsCluster.Init(ClusterID.UnclusteredID, unclusteredObjects);

            int clusterIndex = 0;
            foreach (var pointInfo in points)
            {
                if (pointInfo.Visited) continue;

                pointInfo.Visited = true;
                queue.Enqueue(pointInfo.Item.Index);
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

            foreach (var point in points)
            {
                if (!point.Clustered)
                {
                    unclusteredObjects.Add(point.Item);
                }
                point.Clustered = false;
                point.Visited = false;
            }

            return new ClusterSet<TPoint, TCluster>(clusters, unclusteredObjectsCluster);
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
                var newPointIndex = queue.Dequeue();
                var newPoint = pointInfos.Items[newPointIndex];
                if (!newPoint.Visited)
                {
                    newPoint.Visited = true;
                    SearchAndEnqueue(newPoint.Item);
                }

                if (!newPoint.Clustered)
                {
                    objects.Add(newPoint.Item);
                    newPoint.Clustered = true;
                }
            }

            objects.TrimExcess();
            return cluster;
        }

        private void SearchAndEnqueue(TPoint point)
        {
            foreach (var pointInfo in pointInfos.Search(point))
            {
                if (!pointInfo.Visited)
                {
                    queue.Enqueue(pointInfo.Item.Index);
                }
            }
        }
    }
}