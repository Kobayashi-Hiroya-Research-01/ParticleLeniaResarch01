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
    public class Dbscan<T> where T : IPointData
    {
        private int lastIDValue = 0;
        private readonly ListSpatialIndex<T> pointInfos;
        private readonly Queue<PointInfo<T>> queue;

        public double Epsilon { get; set; } = 0.1;
        public int MinimumPointsPerCluster { get; set; } = 2;

        public Dbscan()
        {
            pointInfos = new ListSpatialIndex<T>();
            queue = new Queue<PointInfo<T>>();
        }

        public void AddPoint(T data)
        {
            pointInfos.List.Add(new PointInfo<T>(data));
        }

        public ClusterSet<T> CalculateClusters(Action<T, ClusterID> onClustered)
        {
            var clusters = new List<Cluster<T>>();
            var unclusterdObjects = new List<T>();

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

            foreach (var pointInfo in pointInfos.List)
            {
                if (!pointInfo.Clustered)
                {
                    onClustered(pointInfo.Item, ClusterID.UnclusteredID);
                    unclusterdObjects.Add(pointInfo.Item);
                }
                else
                {
                    pointInfo.Clustered = false;
                }

                pointInfo.Visited = false;
            }

            return new ClusterSet<T>(clusters, unclusterdObjects);
        }

        private Cluster<T> BuildCluster(PointInfo<T> corePointInfo, Action<T, ClusterID> onClustered)
        {
            int clusterIDValue = (lastIDValue == int.MaxValue) ? 0 : (lastIDValue + 1);
            lastIDValue = clusterIDValue;
            var clusterID = new ClusterID(clusterIDValue);
            var objects = new List<T>();

            corePointInfo.Clustered = true;
            onClustered(corePointInfo.Item, clusterID);
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
                    onClustered?.Invoke(newPoint.Item, clusterID);
                    objects.Add(newPoint.Item);
                }
            }

            return new Cluster<T>(clusterID, objects);
        }

        private void OnFind(PointInfo<T> pInfo)
        {
            if (!pInfo.Visited)
            {
                queue.Enqueue(pInfo);
            }
        }
    }
}