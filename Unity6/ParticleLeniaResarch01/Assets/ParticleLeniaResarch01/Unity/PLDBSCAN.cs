using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Assets.ParticleLeniaResarch01.Analysis;
using System;
using System.Collections.ObjectModel;
using Assets.Packages.viceroypenguin.Dbscan.Custom;
using static UnityEngine.Rendering.DebugUI;
using System.Security.Cryptography;
using UnityEngine.Rendering.Universal;
using static PLMinMax;

public class PLDBSCAN : MonoBehaviour
{
    [Serializable]
    public class LifeTime
    {
        public int id;
        public int lifeTime;
    }

    [Serializable]
    public class DebugData
    {
        public int clusterCount;
        public bool calcLifeTime;
        public LifeTime[] lifeTime;
        public double silhouetteCoefficient;
        public bool calcSilhouetteCoefficient;

        public void Update(ClusterSet<Point> oldSet)
        {
            clusterCount = oldSet.Count;
            if (calcLifeTime)
            {
                lifeTime = oldSet.OrderByDescendingCountClusters
                    .Select(e => new LifeTime() { id = e.ClusterID.Value, lifeTime = e.ClusterID.LifeTime })
                    .OrderByDescending(e => e.lifeTime)
                    .ToArray();
            }

            if (calcSilhouetteCoefficient)
            {
                silhouetteCoefficient = oldSet.SilhouetteCoefficient();
            }
        }
    }

    public enum PointMode
    {
        UtRt,
        Ut,
        E,
        NoPointUtRt,
        NoPointUt,
        NoPointE,
    }

    public PointMode pointMode;
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

    public bool utRtWeightNormalizeWithPosRange;

    [Range(0, 1)]
    public double continuousRatio;

    public bool trackingMode;

    public DebugData debugData;

    private Dbscan<Point> dbscan;
    private ClusterSet<Point> oldSet;
    private int nextID = 0;
    private Dictionary<int, Dictionary<int, int>> oldIDCountByNewID;

    public class Point : IPointData
    {
        public PLParticleController particle;
        private readonly PLDBSCAN pLDBSCAN;
        private readonly double[] point;
        private readonly Action updatePoint;

        public Point(PLParticleController particle, PLDBSCAN pLDBSCAN)
        {
            this.particle = particle;
            this.pLDBSCAN = pLDBSCAN;
            point = new double[4];

            var particleInfo = particle.ParticleInfo;

            const double DIV_SQRT2 = 0.70710678118654752440084436210485;

            updatePoint = pLDBSCAN.pointMode switch
            {
                PointMode.UtRt => () =>
                {
                    GetWeights(pLDBSCAN, out var uw, out var rw, out var ew);
                    point[0] = particleInfo.position[0];
                    point[1] = particleInfo.position[1];
                    point[2] = uw * particleInfo.lastUt * DIV_SQRT2;
                    point[3] = rw * particleInfo.lastRt * DIV_SQRT2;
                }
                ,
                PointMode.Ut => () =>
                {
                    GetWeights(pLDBSCAN, out var uw, out var rw, out var ew);
                    point[0] = particleInfo.position[0];
                    point[1] = particleInfo.position[1];
                    point[2] = uw * particleInfo.lastUt;
                    point[3] = 0;
                }
                ,
                PointMode.E => () =>
                {
                    GetWeights(pLDBSCAN, out var uw, out var rw, out var ew);
                    point[0] = particleInfo.position[0];
                    point[1] = particleInfo.position[1];
                    point[2] = ew * particleInfo.LastE;
                    point[3] = 0;
                }
                ,
                PointMode.NoPointUtRt => () =>
                {
                    GetWeights(pLDBSCAN, out var uw, out var rw, out var ew);
                    point[0] = 0;
                    point[1] = 0;
                    point[2] = uw * particleInfo.lastUt * DIV_SQRT2;
                    point[3] = rw * particleInfo.lastRt * DIV_SQRT2;
                }
                ,
                PointMode.NoPointUt => () =>
                {
                    GetWeights(pLDBSCAN, out var uw, out var rw, out var ew);
                    point[0] = 0;
                    point[1] = 0;
                    point[2] = uw * particleInfo.lastUt;
                    point[3] = 0;
                }
                ,
                PointMode.NoPointE => () =>
                {
                    GetWeights(pLDBSCAN, out var uw, out var rw, out var ew);
                    point[0] = 0;
                    point[1] = 0;
                    point[2] = ew * particleInfo.LastE;
                    point[3] = 0;
                }
                ,
                _ => throw new NotImplementedException(),
            };
        }

        private static void GetWeights(PLDBSCAN pLDBSCAN, out double uw, out double rw, out double ew)
        {
            var minmax = pLDBSCAN.mimmax;
            if (pLDBSCAN.utRtWeightNormalizeWithPosRange)
            {
                var xyRange = Math.Max(minmax.xMinMax.Range, minmax.yMinMax.Range);
                uw = xyRange * pLDBSCAN.utWeight / minmax.utMinMax.Range;
                rw = xyRange * pLDBSCAN.rtWeight / minmax.rtMinMax.Range;
                ew = xyRange * pLDBSCAN.utWeight / minmax.eMinMax.Range;
            }
            else
            {
                uw = pLDBSCAN.utWeight / minmax.utMinMax.Range;
                rw = pLDBSCAN.rtWeight / minmax.rtMinMax.Range;
                ew = pLDBSCAN.utWeight / minmax.eMinMax.Range;
            }
        }

        Span<double> IPointData.Point
        {
            get
            {
                updatePoint();
                return point;
            }
        }

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
            var index = pLDBSCAN.clusterInfoIndex;

            while (index >= list.Count)
            {
                list.Add(new ClusterInfo());
            }

            list[index].clusterID = value;
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

        UpdateSimulationAfter();
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
        oldSet ??= new ClusterSet<Point>(new List<Cluster<Point>>(), new List<Point>());
        oldIDCountByNewID ??= new();
        oldIDCountByNewID.Clear();

        var set = dbscan.CalculateClusters(OnClustered);

        if (trackingMode)
        {
            UpdateWhthTracking(set.OrderByDescendingCountClusters);
        }
        else
        {
            UpdateOnlyFrame(set.OrderByDescendingCountClusters);
        }

        oldSet = set;
        oldSet.UpdateDict();

        debugData.Update(set);
    }

    private void OnClustered(Point point, ClusterID clusterID)
    {
        var oldId = point.ClusterID;
        point.SetClusterID(clusterID);

        if (clusterID == ClusterID.UnclusteredID) return;

        if (!oldIDCountByNewID.TryGetValue(clusterID.Value, out var oldIDCount))
        {
            oldIDCountByNewID[clusterID.Value] = oldIDCount = new Dictionary<int, int>();
        }

        oldIDCount[oldId.Value] = oldIDCount.GetValueOrDefault(oldId.Value) + 1;
    }

    private void UpdateWhthTracking(ReadOnlyCollection<Cluster<Point>> clusters)
    {
        Dictionary<int, Cluster<Point>> trackedClusters = new();

        foreach (var cluster in clusters)
        {
            var clusterID = cluster.ClusterID;
            var oldIDCount = oldIDCountByNewID[clusterID.Value];

            // 条件を満たすもっとも量が多い旧クラスタをトラッキング
            var trackedID = oldIDCount
                .Where(values =>
                {
                    return oldSet.ContainsKey(values.Key);
                })
                .Select(countOld =>
                {
                    //新旧クラスタ一致量
                    var sameCount = countOld.Value;
                    //旧クラスタ総量
                    var oldCount = oldSet[countOld.Key].Count;

                    var p = (double)sameCount / oldCount;
                    var r = (double)sameCount / cluster.Count;

                    return (oldId: countOld.Key, sameCount, p, r);
                })
                .Where(values =>
                {
                    //Fowlkes-Mallows index
                    return values.p * values.r >= continuousRatio * continuousRatio;

                    //Fowlkes-Mallows like condition
                    return values.p >= continuousRatio &&    // 旧クラスタ総量の continuousRatio 倍以上が新クラスタに残っている
                           values.r >= continuousRatio;      // 旧クラスタ由来量の continuousRatio 倍以上が旧クラスタ由来である
                })
                .OrderByDescending(values => values.sameCount)
                .FirstOrDefault();

            clusterID.LifeTime = 1;

            if (trackedID.sameCount == 0)
            {
                cluster.ClusterID.UpdateValue(GetNextID(clusters));
                //Debug.Log($"New {cluster}");
            }
            else
            {
                var trackedIDValue = trackedID.oldId;
                if (trackedClusters.TryGetValue(trackedIDValue, out var sisterCluster))
                {
                    var sisterClusterID = sisterCluster.ClusterID;
                    sisterClusterID.LifeTime = 1;
                    if (sisterClusterID.Value == trackedIDValue)
                    {
                        sisterClusterID.UpdateValue(GetNextID(clusters));
                        //Debug.Log($"Split {oldSet[trackedID.oldId]} to {sisterCluster}");
                    }

                    clusterID.UpdateValue(GetNextID(clusters));
                    //Debug.Log($"Split {oldSet[trackedID.oldId]} to {cluster}");
                }
                else
                {
                    clusterID.LifeTime = oldSet[trackedIDValue].ClusterID.LifeTime + 1;
                    clusterID.UpdateValue(trackedIDValue);
                    trackedClusters.Add(trackedIDValue, cluster);
                }
            }
        }
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