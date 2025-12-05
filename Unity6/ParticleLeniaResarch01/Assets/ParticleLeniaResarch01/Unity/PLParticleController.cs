using Assets.ParticleLeniaResarch01.Analysis;
using Assets.ParticleLeniaResarch01.Scripts.Data;
using System.Collections.Generic;
using UnityEngine;

public class PLParticleController : MonoBehaviour
{
    public int particleIndex = -1;
    public PLHeatMapGaussController heatMapU;
    public PLHeatMapRepController heatMapR;
    public Renderer core;
    public PLClusterInfo clusterInfo;

    public float e;
    public float g;
    public float r;

    private PLMinMax pLMinMax;
    private Material material;
    private ParticleInfo particleInfo;
    private PLViewModeController viewModeController;

    public ParticleInfo ParticleInfo => particleInfo;

    public PLClusterInfo ClusterInfo => clusterInfo;

    private void OnEnable()
    {
        if (material == null) material = core.material;
    }

    private void OnDestroy()
    {
        Destroy(material);
    }

    public void InfoUpdate(ParticleInfo particleInfo)
    {
        this.particleInfo = particleInfo;
    }

    public void InitPostAction(ParticleInfo particleInfo, PLViewModeController viewModeController, PLMinMax pLMinMax, int particleIndex)
    {
        this.particleInfo = particleInfo;
        this.viewModeController = viewModeController;
        this.pLMinMax = pLMinMax;
        this.particleIndex = particleIndex;
        heatMapU.Setup(particleInfo, viewModeController);
        heatMapR.Setup(particleInfo, viewModeController);
    }

    public void Update()
    {
        var pos = particleInfo.position;
        var posV = Vector3.zero;
        for (int i = 0; i < Mathf.Min(3, pos.Length); i++)
        {
            posV[i] = (float)pos[i];
        }

        var list = clusterInfo.clusterInfoList;
        while (list.Count < 2)
        {
            list.Add(new ClusterInfo());
        }

        var color1 = viewModeController.GetColor(particleInfo, clusterInfo.clusterInfoList[0], pLMinMax);
        var color2 = viewModeController.GetColor(particleInfo, clusterInfo.clusterInfoList[1], pLMinMax);

        material.color = color1;
        material.SetColor("_Color2", color2);

        if (pos.Length < 3)
        {
            var z = viewModeController.GetRate(particleInfo, PLViewModeController.ColorMode.E, pLMinMax);
            //posV.z = 10f * (1.0f - z);
        }

        transform.position = posV;
        var size = viewModeController.coreSize;
        core.transform.localScale = size * Vector3.one;
        e = (float)particleInfo.LastE;
        g = (float)particleInfo.lastGt;
        r = (float)particleInfo.lastRt;

        if (Input.GetKeyUp(KeyCode.Q) && particleIndex == 0)
        {
            Debug.Log("Pos : " + string.Join(",", particleInfo.position));
        }
    }
}