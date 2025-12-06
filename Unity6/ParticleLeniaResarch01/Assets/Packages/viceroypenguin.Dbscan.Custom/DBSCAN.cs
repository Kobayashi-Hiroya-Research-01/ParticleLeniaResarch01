// This code was copied from https://github.com/viceroypenguin/DBSCAN
// and edited by this project.

using Assets.Packages.viceroypenguin.Dbscan.Custom;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;

namespace Assets.Packages.viceroypenguin.Dbscan.Custom
{
    /// <summary>
    /// Contains static methods to run the DBSCAN algorithm.
    /// </summary>
    public class Dbscan<TPoint, TCluster> : IDbscan<TPoint, TCluster> where TPoint : IPointData
        where TCluster : Cluster<TPoint>, new()
    {
        private int lastIDValue = 0;
        private readonly ListSpatialIndex<TPoint> pointInfos;
        private readonly Queue<PointInfo<TPoint>> queue;

        public double Epsilon { get; set; } = 0.1;
        public int MinimumPointsPerCluster { get; set; } = 2;

        public Dbscan()
        {
            pointInfos = new ListSpatialIndex<TPoint>();
            queue = new Queue<PointInfo<TPoint>>();
        }

        public void AddPoint(TPoint data)
        {
            pointInfos.List.Add(new PointInfo<TPoint>(data));
        }

        public ClusterSet<TPoint, TCluster> CalculateClusters(Action<TPoint, TCluster> onClustered)
        {
            var clusters = new List<TCluster>();

            foreach (var pointInfo in pointInfos.List)
            {
                if (pointInfo.Visited) continue;

                pointInfo.Visited = true;
                queue.Enqueue(pointInfo);
                pointInfos.Search(pointInfo.Item, Epsilon, OnFind);

                if (queue.Count >= MinimumPointsPerCluster)
                {
                    var cluster = BuildCluster(pointInfo, onClustered);
                    clusters.Add(cluster);
                }
                else
                {
                    queue.Clear();
                }
            }

            var unclusteredObjectsCluster = new TCluster();
            var unclusteredObjects = new List<TPoint>();
            unclusteredObjectsCluster.Init(ClusterID.UnclusteredID, unclusteredObjects);

            foreach (var pointInfo in pointInfos.List)
            {
                if (!pointInfo.Clustered)
                {
                    onClustered(pointInfo.Item, unclusteredObjectsCluster);
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

        private TCluster BuildCluster(PointInfo<TPoint> corePointInfo, Action<TPoint, TCluster> onClustered)
        {
            int clusterIDValue = (lastIDValue == int.MaxValue) ? 0 : (lastIDValue + 1);
            lastIDValue = clusterIDValue;
            var clusterID = new ClusterID(clusterIDValue);
            var objects = new List<TPoint>();
            var cluster = new TCluster();
            cluster.Init(clusterID, objects);

            corePointInfo.Clustered = true;
            onClustered(corePointInfo.Item, cluster);
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