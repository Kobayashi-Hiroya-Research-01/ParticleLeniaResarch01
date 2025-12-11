using Assets.Packages.viceroypenguin.Dbscan.Custom;
using Assets.ParticleLeniaResarch01.Analysis;
using Assets.ParticleLeniaResarch01.Analyzers;
using Assets.ParticleLeniaResarch01.Scripts;
using Assets.ParticleLeniaResarch01.Scripts.Data;
using System.Collections.Generic;
using System.Linq;
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

    private ParticleLeniaAnalyzer analyzer;
    private Material material;
    private ParticleInfo particleInfo;
    private PLViewModeController viewModeController;
    private MinMaxAnalyzer pLMinMax;

    public ParticleInfo ParticleInfo => particleInfo;

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

    public void InitPostAction(ParticleInfo particleInfo, PLViewModeController viewModeController, ParticleLeniaAnalyzer analyzer, int particleIndex)
    {
        this.particleInfo = particleInfo;
        this.viewModeController = viewModeController;
        this.analyzer = analyzer;
        this.particleIndex = particleIndex;
        pLMinMax = analyzer.Search<MinMaxAnalyzer>();
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

        var dic = clusterInfo.clusterInfoDict;

        var color1 = viewModeController.GetColor(particleInfo, dic?.GetValueOrDefault("Component")?.ClusterID, pLMinMax);
        var color2 = viewModeController.GetColor(particleInfo, dic?.GetValueOrDefault("Unit")?.ClusterID, pLMinMax);

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
    }
}