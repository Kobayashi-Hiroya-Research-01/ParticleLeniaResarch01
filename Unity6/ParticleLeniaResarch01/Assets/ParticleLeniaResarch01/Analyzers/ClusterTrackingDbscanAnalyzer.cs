using Assets.Packages.viceroypenguin.Dbscan.Custom;
using Assets.ParticleLeniaResarch01.Analysis;
using Assets.ParticleLeniaResarch01.Scripts;
using Assets.ParticleLeniaResarch01.Scripts.Data;
using System;

namespace Assets.ParticleLeniaResarch01.Analyzers
{
    using PointMode = ClusterTrackingDbscanAnalyzerPointMode;
    using PointSettingData = ClusterTrackingDbscanAnalyzerPointSettingData;

    public class ClusterTrackingDbscanAnalyzer : IParticleLeniaAnalyzer, ClusterTrackingDbscanAnalyzer.IOption
    {
        public interface IOption
        {
            ClusterTrackingDbscan.IOption ClusterTrackingOption { get; set; }

            PointSettingData.IOption PointSettingsOption { get; set; }

            string ClusteringName { get; set; }

            Dbscan.IOption DbscanOption
            {
                get => ClusterTrackingOption.DbscanOption;
                set => value.CopyTo(ClusterTrackingOption.DbscanOption);
            }

            void CopyTo(IOption other)
            {
                ClusterTrackingOption.CopyTo(other.ClusterTrackingOption);
                other.PointSettingsOption = PointSettingsOption;
                other.ClusteringName = ClusteringName;
            }
        }

        public delegate Span<double> GetPoint(ParticleInfo particleInfo, Span<double> point);

        public delegate void OnClusteredEvent(Point point, ClusterTrackingCluster<Point> cluster, string clusteringName);

        public class Point : IClusterTrackingPointData
        {
            private readonly GetPoint getPoint;
            private readonly double[] point;

            public Point(ParticleInfo particleInfo, int index, ClusterTrackingDbscanAnalyzer analyzer)
            {
                ParticleInfo = particleInfo;
                Index = index;
                this.getPoint = analyzer.getPoint;
                point = new double[particleInfo.position.Length + 2];
            }

            public ClusterID ClusterID { get; set; } = ClusterID.UnclusteredID;

            public int Index { get; }

            public ParticleInfo ParticleInfo { get; }

            ReadOnlySpan<double> IPointData.Point
            {
                get
                {
                    getPoint(ParticleInfo, point);
                    return point;
                }
            }
        }

        private MinMaxAnalyzer minMaxAnalyzer;
        private GetPoint getPoint;

        private double utNormalizedWeight;
        private double rtNormalizedWeight;
        private double eNormalizedWeight;

        private readonly ParticleLeniaAnalyzerElement[] elements;

        public string ClusteringName { get; set; } = "New Clustering";

        public ClusterTrackingDbscan<Point> ClusterTrackingDbscan { get; private set; }

        public ClusterTrackingDbscan.IOption ClusterTrackingOption
        {
            get => ClusterTrackingDbscan;
            set => value.CopyTo(ClusterTrackingDbscan);
        }

        public PointSettingData PointSettings { get; }

        public PointSettingData.IOption PointSettingsOption
        {
            get => PointSettings;
            set => value.CopyTo(PointSettings);
        }

        public bool Enabled { get; set; } = true;

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

        public ClusterTrackingDbscanAnalyzer(IOption option) : this()
        {
            option.CopyTo(this);
        }

        public void Reset()
        {
            OnClustered = null;
            ClusterTrackingDbscan.Reset();
            UpdatePointSettings();
        }

        public void UpdatePointSettings()
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
            ClusterTrackingDbscan.AddPoint(new Point(info, index, this));
        }

        public void AllUpdated(ParticleInfoArray particles)
        {
            UpdateNormalizedWeight();
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

        private void OnClusteredWithMyName(Point point, ClusterTrackingCluster<Point> cluster)
        {
            OnClustered?.Invoke(point, cluster, ClusteringName);
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