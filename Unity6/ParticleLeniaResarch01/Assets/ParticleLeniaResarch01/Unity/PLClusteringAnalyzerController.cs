using Assets.Packages.viceroypenguin.Dbscan.Custom;
using Assets.ParticleLeniaResarch01.Analysis;
using Assets.ParticleLeniaResarch01.Analyzers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PLClusteringAnalyzerController : PLAnalyzerController, ClusterTrackingDbscanAnalyzer.IOption
{
    [Serializable]
    public class DbscanOptionData : Dbscan.IOption
    {
        [SerializeField]
        [Range(0, 10)]
        private double epsilon;

        [SerializeField]
        [Range(0, 10)]
        private int minimumPointsPerCluster;

        public double Epsilon { get => epsilon; set => epsilon = value; }
        public int MinimumPointsPerCluster { get => minimumPointsPerCluster; set => minimumPointsPerCluster = value; }
    }

    [Serializable]
    public class ClusterTrackingOptionData : ClusterTrackingDbscan.IOption
    {
        [SerializeField]
        private DbscanOptionData dbscanOption;

        [SerializeField]
        private ClusterTrackingMode trackingMode;

        [SerializeField]
        [Range(0, 1)]
        private double continuousRatio;

        [SerializeField]
        [Range(0, 1000)]
        private int startIndex;

        public Dbscan.IOption DbscanOption
        {
            get => dbscanOption;
            set => value.CopyTo(dbscanOption ??= new());
        }

        public ClusterTrackingMode TrackingMode { get => trackingMode; set => trackingMode = value; }
        public int StartIndex { get => startIndex; set => startIndex = value; }
        public double ContinuousRatio { get => continuousRatio; set => continuousRatio = value; }
        public int LastID { get; set; } = -1;
    }

    [Serializable]
    public class PointSettingsOptionData : ClusterTrackingDbscanAnalyzerPointSettingData.IOption
    {
        [field: SerializeField]
        public ClusterTrackingDbscanAnalyzerPointMode PointMode { get; set; }

        [field: SerializeField]
        [field: Range(0, 50)]
        public double EWeight { get; set; }

        [field: SerializeField]
        [field: Range(0, 50)]
        public double UtWeight { get; set; }

        [field: SerializeField]
        [field: Range(0, 50)]
        public double RtWeight { get; set; }

        [field: SerializeField]
        public bool NormalizeStatusByPositionRange { get; set; }

        [field: SerializeField]
        public bool NormalizeStatusByDimension { get; set; }
    }

    [Serializable]
    public class DebugInfo
    {
        public enum OrderBy
        {
            LifeTime,
            ID,
            Count,
        }

        [Serializable]
        public class Item
        {
            public int id;
            public int count;
            public int lifeTime;
        }

        public OrderBy orderBy;

        public int count;
        public double clusterSizeAverage;
        public double lifeTimeAverage;

        public Item[] clusterInfoList;

        public void Update(Span<ClusterTrackingCluster<ClusterTrackingPoint>> clusters)
        {
            var items = new List<Item>(clusters.Length);
            count = clusters.Length;
            clusterSizeAverage = lifeTimeAverage = 0.0;

            foreach (var e in clusters)
            {
                clusterSizeAverage += e.Count;
                lifeTimeAverage += e.LifeTime;

                var newItem = new Item()
                {
                    id = e.ClusterID.Value,
                    count = e.Count,
                    lifeTime = e.LifeTime
                };

                items.Add(newItem);
            }

            clusterSizeAverage /= count;
            lifeTimeAverage /= count;

            switch (orderBy)
            {
                case OrderBy.LifeTime:
                    clusterInfoList = items.OrderByDescending(e => e.lifeTime).ToArray();
                    break;

                case OrderBy.ID:
                    clusterInfoList = items.OrderByDescending(e => e.id).ToArray();
                    break;

                case OrderBy.Count:
                    clusterInfoList = items.OrderByDescending(e => e.count).ToArray();
                    break;
            }
        }
    }

    public DebugInfo debugInfo;

    [SerializeField]
    private string clusteringName = "New Clustering";

    [SerializeField]
    private ClusterTrackingOptionData clusterTrackingOption = new()
    {
        ContinuousRatio = 0.5,
        StartIndex = 0,
        TrackingMode = ClusterTrackingMode.Continuous_FowlkesMallowsIndex,
        DbscanOption = new DbscanOptionData()
        {
            Epsilon = 2.0,
            MinimumPointsPerCluster = 2,
        }
    };

    [SerializeField]
    private PointSettingsOptionData pointSettingsOption;

    private ClusterTrackingDbscanAnalyzer analyzer;

    public ClusterTrackingDbscan.IOption ClusterTrackingOption
    {
        get => clusterTrackingOption;
        set => value.CopyTo(clusterTrackingOption ??= new());
    }

    public ClusterTrackingDbscanAnalyzerPointSettingData.IOption PointSettingsOption
    {
        get => pointSettingsOption;
        set => value.CopyTo(pointSettingsOption ??= new());
    }

    public string ClusteringName
    {
        get => clusteringName;
        set => clusteringName = value;
    }

    public ClusterTrackingDbscanAnalyzer.IOption AsOption => this;

    public ClusterTrackingDbscanAnalyzer ClusterTrackingDbscanAnalyzer
    {
        get
        {
            if (analyzer == null)
            {
                analyzer = new ClusterTrackingDbscanAnalyzer();

                AsOption.CopyTo(analyzer);
            }

            return analyzer;
        }
    }

    public override IParticleLeniaAnalyzer Analyzer => ClusterTrackingDbscanAnalyzer;

    public void Update()
    {
        var result = ClusterTrackingDbscanAnalyzer?.ClusterTrackingDbscan?.LastResult;

        if (result != null)
        {
            debugInfo ??= new DebugInfo();

            var values = result.Values;
            debugInfo.Update(values);
        }
    }

    private void OnValidate()
    {
        var @this = (ClusterTrackingDbscanAnalyzer.IOption)this;
        @this.CopyTo(ClusterTrackingDbscanAnalyzer);
    }
}