using Assets.ParticleLeniaResarch01.Analysis;
using Assets.ParticleLeniaResarch01.Scripts;
using Assets.ParticleLeniaResarch01.Scripts.Data;
using System;
using UnityEngine;

namespace Assets.ParticleLeniaResarch01.Analyzers
{
    using Cluster = ClusterTrackingCluster<ClusterTrackingPoint>;
    using Point = ClusterTrackingPoint;
    using PointMode = ClusterTrackingDbscanAnalyzerPointMode;
    using PointSettingData = ClusterTrackingDbscanAnalyzerPointSettingData;

    public class ClusterTrackingDbscanAnalyzer : IParticleLeniaAnalyzer, ClusterTrackingDbscanAnalyzer.IState
    {
        public interface IOption
        {
            string ClusteringName { get; set; }

            ClusterTrackingDbscan.IOption ClusterTrackingOption { get; }

            PointSettingData.IOption PointSettingsOption { get; }

            void CopyTo(IOption other)
            {
                ClusterTrackingOption.CopyTo(other.ClusterTrackingOption);
                PointSettingsOption.CopyTo(other.PointSettingsOption);
                other.ClusteringName = ClusteringName;
            }
        }

        public interface IState : IOption
        {
            ClusterTrackingDbscan.IState ClusterTrackingState { get; }

            void CopyTo(IState other)
            {
                AsOption.CopyTo(other);
                ClusterTrackingState.CopyTo(other.ClusterTrackingState);
            }

            IOption AsOption => this;
        }

        public delegate void OnClusteredEvent(Point point, Cluster cluster, string clusteringName);

        private MinMaxAnalyzer minMaxAnalyzer;
        private Point.GetPoint getPoint;

        private double utNormalizedWeight;
        private double rtNormalizedWeight;
        private double eNormalizedWeight;

        private readonly ParticleLeniaAnalyzerElement[] elements;

        public string ClusteringName { get; set; } = "New Clustering";

        public ClusterTrackingDbscan ClusterTrackingDbscan { get; private set; }

        public PointSettingData PointSettings { get; }

        public bool Enabled { get; set; } = true;

        public ClusterTrackingDbscan.IOption ClusterTrackingOption => ClusterTrackingDbscan;

        public PointSettingData.IOption PointSettingsOption => PointSettings;

        public ClusterTrackingDbscan.IState ClusterTrackingState => ClusterTrackingDbscan;

        public IOption AsOption => this;
        public IState AsState => this;

        public event OnClusteredEvent OnClustered;

        public ClusterTrackingDbscanAnalyzer()
        {
            elements = new ParticleLeniaAnalyzerElement[]
            {
                new(timing: ParticleLeniaAnalyzerTiming.ForeachCreated, @foreach: ForeachCreated),
                new(timing: ParticleLeniaAnalyzerTiming.ForeachUpdated, loopEnd: AllUpdated),
            };

            ClusterTrackingDbscan = new();
            PointSettings = new();
        }

        public void Reset()
        {
            OnClustered = null;
            ClusterTrackingDbscan.Reset();
            UpdateGetPoint();
        }

        private void UpdateGetPoint()
        {
            var settings = PointSettings;
            getPoint = settings.PointMode switch
            {
                PointMode.PositionOnly => (info, point) =>
                {
                    point[0] = info.position[0];
                    point[1] = info.position[1];
                    return point[..2];
                }
                ,
                PointMode.UtRt => (info, point) =>
                {
                    point[0] = info.position[0];
                    point[1] = info.position[1];
                    point[2] = utNormalizedWeight * info.lastUt;
                    point[3] = rtNormalizedWeight * info.lastRt;
                    return point[..4];
                }
                ,
                PointMode.Ut => (info, point) =>
                {
                    point[0] = info.position[0];
                    point[1] = info.position[1];
                    point[2] = utNormalizedWeight * info.lastUt;
                    return point[..3];
                }
                ,
                PointMode.E => (info, point) =>
                {
                    point[0] = info.position[0];
                    point[1] = info.position[1];
                    point[2] = eNormalizedWeight * info.LastE;
                    return point[..3];
                }
                ,
                PointMode.NoPosUtRt => (info, point) =>
                {
                    point[0] = utNormalizedWeight * info.lastUt;
                    point[1] = rtNormalizedWeight * info.lastRt;
                    return point[..2];
                }
                ,
                PointMode.NoPosUt => (info, point) =>
                {
                    point[0] = utNormalizedWeight * info.lastUt;
                    return point[..1];
                }
                ,
                PointMode.NoPosE => (info, point) =>
                {
                    point[0] = eNormalizedWeight * info.LastE;
                    return point[..1];
                }
                ,
                _ => throw new NotImplementedException(),
            };
        }

        public void SetupDependencies(ParticleLeniaAnalyzer dependencies, IParticleLenia particleLenia)
        {
            minMaxAnalyzer = dependencies.Search<MinMaxAnalyzer>();
        }

        public ReadOnlySpan<ParticleLeniaAnalyzerElement> GetAnalyzerElements()
        {
            return elements;
        }

        public void ForeachCreated(ParticleInfo info, int index)
        {
            ClusterTrackingDbscan.AddPoint(new Point(info, index, getPoint));
        }

        private const int _D_Start = 1;
        private const int _D_End = 20 * 60 * 5;
        private readonly DebugTextWriter _D_ParticleInfoArray = new("ParticleInfoArray", killed: true);
        private readonly DebugTextWriter _D_NormalizedWeight = new("NormalizedWeight", killed: true);

        public void AllUpdated(ParticleInfoArray particles)
        {
            _D_ParticleInfoArray.AppendWithLog(() => string.Join(", ", particles), _D_Start, _D_End);

            UpdateNormalizedWeight();

            _D_NormalizedWeight.AppendWithLog(() => $"{{ utw = {utNormalizedWeight}, rtw = {rtNormalizedWeight},  ew = {eNormalizedWeight}}}", _D_Start, _D_End);

            var set = ClusterTrackingDbscan.CalculateClusters();
            foreach (var cluster in set.Values)
            {
                foreach (var point in cluster)
                {
                    OnClustered?.Invoke(point, cluster, ClusteringName);
                }
            }

            foreach (var point in set.UnclusteredObjects)
            {
                OnClustered?.Invoke(point, set.UnclusteredObjects, ClusteringName);
            }
        }

        private static readonly double sqrt2 = Math.Sqrt(2.0);

        private void UpdateNormalizedWeight()
        {
            var settings = PointSettings;
            double normalizeByDimension2D = settings.NormalizeStatusByDimension ? sqrt2 : 1.0;
            double normalizeByPosRange;
            if (settings.NormalizeStatusByPositionRange)
            {
                var xRange = minMaxAnalyzer.XMinMax.Range;
                var yRange = minMaxAnalyzer.YMinMax.Range;
                normalizeByPosRange = Math.Sqrt(xRange * xRange + yRange * yRange);
            }
            else
            {
                normalizeByPosRange = 1.0;
            }

            switch (settings.PointMode)
            {
                case PointMode.UtRt:
                case PointMode.NoPosUtRt:
                    utNormalizedWeight = settings.UtWeight * normalizeByPosRange / (minMaxAnalyzer.UtMinMax.Range * normalizeByDimension2D);
                    rtNormalizedWeight = settings.RtWeight * normalizeByPosRange / (minMaxAnalyzer.RtMinMax.Range * normalizeByDimension2D);
                    break;

                case PointMode.Ut:
                case PointMode.NoPosUt:
                    utNormalizedWeight = settings.UtWeight * normalizeByPosRange / minMaxAnalyzer.UtMinMax.Range;
                    break;

                case PointMode.E:
                case PointMode.NoPosE:
                    eNormalizedWeight = settings.EWeight * normalizeByPosRange / minMaxAnalyzer.EMinMax.Range;
                    break;
            }
        }
    }
}