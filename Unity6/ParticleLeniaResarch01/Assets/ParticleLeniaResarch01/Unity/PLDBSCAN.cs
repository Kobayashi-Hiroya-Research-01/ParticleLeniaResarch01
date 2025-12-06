using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Assets.ParticleLeniaResarch01.Analysis;
using System;
using System.Collections.ObjectModel;
using Assets.Packages.viceroypenguin.Dbscan.Custom;
using Cluster = Assets.ParticleLeniaResarch01.Analysis.ClusterTrackingCluster<PLDBSCAN.Point>;

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

        public void Update(ClusterSet<Point, Cluster> oldSet)
        {
            clusterCount = oldSet.Count;
            if (calcLifeTime)
            {
                lifeTime = oldSet.Clusters
                    .Select(e => new LifeTime() { id = e.ClusterID.Value, lifeTime = e.LifeTime })
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

    public int clusterInfoIndex;
    public PointMode pointMode;
    public ClusterTrackingMode clusterTrackingMode;

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

    public DebugData debugData;

    public PLMinMax mimmax;

    private ClusterTrackingDbscan<Point> dbscan;

    public class Point : IClusterTrackingPointData
    {
        private readonly PLParticleController particle;
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

            set
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

        public int Index => particle.particleIndex;
    }

    private void OnEnable()
    {
        debugData = new();
    }

    private void OnValidate()
    {
        dbscan ??= new();
        dbscan.Epsilon = eps;
        dbscan.MinimumPointsPerCluster = minPtr;
        dbscan.TrackingMode = clusterTrackingMode;
        dbscan.StartIndex = clusterInfoIndex * 1000;
        dbscan.ContinuousRatio = continuousRatio;

        UpdateSimulationAfter();
    }

    public void SimulationInit()
    {
        dbscan = new()
        {
            NextID = clusterInfoIndex * 1000,
            Epsilon = eps,
            MinimumPointsPerCluster = minPtr,
            TrackingMode = clusterTrackingMode,
            StartIndex = clusterInfoIndex * 1000,
            ContinuousRatio = continuousRatio,
        };
    }

    public void InitPostAction(PLParticleController particle)
    {
        var point = new Point(particle, this);
        dbscan.AddPoint(point);
    }

    public void UpdateSimulationAfter()
    {
        var set = dbscan.CalculateClusters(null);
        debugData.Update(set);
    }
}