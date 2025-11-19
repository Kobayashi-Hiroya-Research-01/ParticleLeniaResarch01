using Assets.ParticleLeniaResarch01.Scripts.Data;
using UnityEngine;

public class PLParticleController : MonoBehaviour
{
    public PLHeatMapGaussController heatMapU;
    public PLHeatMapRepController heatMapR;
    public Renderer core;

    public float e;
    public float g;
    public float r;

    private Material material;
    private ParticleInfo particleInfo;
    private PLViewModeController viewModeController;

    private void OnEnable()
    {
        material = core.material;
    }

    private void OnDisable()
    {
        Destroy(material);
    }

    public void Setup(ParticleInfo particleInfo, PLViewModeController viewModeController)
    {
        this.particleInfo = particleInfo;
        this.viewModeController = viewModeController;
        heatMapU.Setup(particleInfo, viewModeController);
        heatMapR.Setup(particleInfo, viewModeController);
    }

    public void Update()
    {
        var pos = particleInfo.position;
        var posV = Vector3.zero;
        for (int i = 0; i < pos.Length; i++)
        {
            if (i >= 3) break;

            posV[i] = (float)pos[i];
        }

        material.color = viewModeController.GetColor(particleInfo);

        if (pos.Length < 3)
        {
            var z = viewModeController.GetRate(particleInfo, PLViewModeController.ColorMode.E);
            posV[2] = 10f * (1.0f - z);
        }

        transform.position = posV;
        var size = viewModeController.coreSize;
        core.transform.localScale = size * Vector3.one;
        e = (float)particleInfo.LastE;
        g = (float)particleInfo.lastGt;
        r = (float)particleInfo.lastRt;
    }
}