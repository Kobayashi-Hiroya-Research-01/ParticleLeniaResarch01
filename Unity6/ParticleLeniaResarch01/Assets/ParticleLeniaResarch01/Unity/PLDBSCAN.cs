using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Assets.ParticleLeniaResarch01.Analysis;
using System;
using System.Collections.ObjectModel;
using Assets.Packages.viceroypenguin.Dbscan.Custom;

public class PLDBSCAN : MonoBehaviour
{
    [Serializable]
    public class DebugData
    {
        public int clusterCount;
    }

    public int clusterInfoIndex;
    public PLMinMax mimmax;

    [Range(1, 10)]
    public int minPtr;

    [Min(0)]
    public double eps;

    [Min(0)]
    public double utWeight;

    [Min(0)]
    public double rtWeight;

    [Range(0, 1)]
    public double continuousRatio;

    public bool trackingMode;

    public DebugData debugData;

    private Dbscan<Point> dbscan;
    private ClusterSet<Point> oldSet;
    private int nextID = 0;

    private class Point : IPointData
    {
        public PLParticleController particle;
        private readonly PLDBSCAN pLDBSCAN;
        private readonly double[] point;

        public Point(PLParticleController particle, PLDBSCAN pLDBSCAN)
        {
            this.particle = particle;
            this.pLDBSCAN = pLDBSCAN;
            point = new double[4];
        }

        Span<double> IPointData.Point
        {
            get
            {
                var particleInfo = particle.ParticleInfo;
                var minmax = pLDBSCAN.mimmax;
                point[0] = particleInfo.position[0];
                point[1] = particleInfo.position[1];
                point[2] = minmax.utMinMax.GetRate(pLDBSCAN.utWeight * particleInfo.lastUt);
                point[3] = minmax.rtMinMax.GetRate(pLDBSCAN.rtWeight * particleInfo.lastRt);
                return point;
            }
        }

        public ClusterID OldClusterID { get; private set; } = ClusterID.UnclusteredID;

        public ClusterID ClusterID
        {
            get
            {
                var list = particle.ClusterInfo.clusterInfoList;

                while (pLDBSCAN.clusterInfoIndex >= list.Count)
                {
                    list.Add(new ClusterInfo());
                }

                return list[pLDBSCAN.clusterInfoIndex].clusterID;
            }
        }

        public int Index => particle.particleIndex;

        public void SetClusterID(ClusterID value)
        {
            var list = particle.ClusterInfo.clusterInfoList;

            while (pLDBSCAN.clusterInfoIndex >= list.Count)
            {
                list.Add(new ClusterInfo());
            }

            OldClusterID = list[pLDBSCAN.clusterInfoIndex].clusterID;

            list[pLDBSCAN.clusterInfoIndex].clusterID = value;
        }
    }

    private void OnEnable()
    {
        debugData = new();
    }

    private void OnValidate()
    {
        dbscan ??= new Dbscan<Point>();
        dbscan.Epsilon = eps;
        dbscan.MinimumPointsPerCluster = minPtr;
    }

    public void SimulationInit()
    {
        dbscan = new Dbscan<Point>() { Epsilon = eps, MinimumPointsPerCluster = minPtr };
        nextID = clusterInfoIndex * 1000;
    }

    public void InitPostAction(PLParticleController particle)
    {
        var point = new Point(particle, this);
        dbscan.AddPoint(point);
    }

    public void UpdateSimulationAfter()
    {
        var set = dbscan.CalculateClusters(OnClustered);

        if (trackingMode)
        {
            UpdateWhthTracking(set.OrderByDescendingCountClusters);
        }
        else
        {
            UpdateOnlyFrame(set.OrderByDescendingCountClusters);
        }

        debugData.clusterCount = set.Count;

        oldSet = set;
        oldSet.UpdateDict();
    }

    private void OnClustered(Point point, ClusterID clusterID)
    {
        point.SetClusterID(clusterID);
    }

    private int GetNextID(ReadOnlyCollection<Cluster<Point>> clusters)
    {
        if (nextID > int.MaxValue / 2)
        {
            nextID = 0;
        }

        for (int i = 0; i < clusters.Count; i++)
        {
            var e = clusters[i];
            if (e.ClusterID.Value == nextID)
            {
                ++nextID;
                i = 0;
                continue;
            }
        }

        return nextID++;
    }

    private void UpdateWhthTracking(ReadOnlyCollection<Cluster<Point>> clusters)
    {
        if (oldSet == null)
        {
            UpdateOnlyFrame(clusters);
            return;
        }

        Dictionary<int, Cluster<Point>> trackedClusters = new();

        foreach (var cluster in clusters)
        {
            // このクラスタの各粒子の旧クラスタにおける分布を計算
            var countOlds = new Dictionary<ClusterID, int>();
            foreach (var point in cluster)
            {
                var oldId = point.OldClusterID;

                if (oldId == ClusterID.UnclusteredID) continue;
                if (!oldSet.ContainsKey(oldId)) continue;

                countOlds[oldId] = countOlds.GetValueOrDefault(oldId) + 1;
            }

            // 条件を満たすもっとも量が多い旧クラスタをトラッキング
            var trackedID = countOlds
                .OrderByDescending(countOld => countOld.Value)
                .FirstOrDefault(countOld =>
                {
                    //旧クラスタ由来量
                    var sameCount = countOld.Value;
                    //旧クラスタ総量
                    var oldCount = oldSet[countOld.Key].Count;

                    return sameCount >= oldCount * continuousRatio &&    // 旧クラスタ総量の continuousRatio 倍以上が新クラスタに残っている
                           oldCount >= sameCount * continuousRatio;      // 旧クラスタ由来量の continuousRatio 倍以上が旧クラスタ由来である
                });

            if (trackedID.Value != default)
            {
                var trackedIDValue = trackedID.Key.Value;
                if (trackedClusters.TryGetValue(trackedIDValue, out var sisterCluster))
                {
                    sisterCluster.ClusterID.UpdateValue(GetNextID(clusters));
                    cluster.ClusterID.UpdateValue(GetNextID(clusters));
                    Debug.Log($"Split {oldSet[trackedID.Key]} to {sisterCluster} and {cluster}");
                }
                else
                {
                    cluster.ClusterID.UpdateValue(trackedIDValue);
                    trackedClusters.Add(trackedIDValue, cluster);
                }
            }
            else
            {
                cluster.ClusterID.UpdateValue(GetNextID(clusters));
                Debug.Log($"New {cluster}");
            }
        }
    }

    private void UpdateOnlyFrame(ReadOnlyCollection<Cluster<Point>> clusters)
    {
        int i = clusterInfoIndex * 1000;
        foreach (var cluster in clusters)
        {
            cluster.ClusterID.UpdateValue(i++);
        }
        nextID = i;
    }
}