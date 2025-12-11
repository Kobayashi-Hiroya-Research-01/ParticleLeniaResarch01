using Assets.Packages.viceroypenguin.Dbscan.Custom;
using Assets.ParticleLeniaResarch01.Analysis;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;

public class PLClusterInfo : MonoBehaviour
{
    [Serializable]
    public class Item
    {
        public string clusteringName;
        public int clusterID;
        public int lifeTime;

        public Item()
        {
        }

        public Item(string clusteringName, ClusterID clusterID, int lifeTime)
        {
            this.clusteringName = clusteringName;
            this.clusterID = clusterID.Value;
            this.lifeTime = lifeTime;
        }
    }

    public Item[] debugData;

    public Dictionary<string, IClusterTrackingCluster> clusterInfoDict;
    private readonly object clusterInfoDictLock = new();

    public void SetCluster(string clusteringName, IClusterTrackingCluster cluster)
    {
        lock (clusterInfoDictLock)
        {
            clusterInfoDict ??= new();
            clusterInfoDict[clusteringName] = cluster;
        }
    }

    private void Update()
    {
        lock (clusterInfoDictLock)
        {
            clusterInfoDict ??= new();
            debugData = clusterInfoDict
                .Select(e =>
                {
                    return new Item(e.Key, e.Value.ClusterID, e.Value.LifeTime);
                })
                .ToArray();
        }
    }
}