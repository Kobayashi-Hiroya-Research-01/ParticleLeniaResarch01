using Assets.ParticleLeniaResarch01.Analysis;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PLClusterInfo : MonoBehaviour
{
    public List<ClusterInfo> clusterInfoList;

    public int[] iDList;

    private void OnEnable()
    {
        for (int i = 0; i < clusterInfoList.Count; i++)
        {
            clusterInfoList[i] ??= new ClusterInfo();
        }
    }

    private void Update()
    {
        iDList = clusterInfoList.Select(e => e.clusterID.Value).ToArray();
    }
}