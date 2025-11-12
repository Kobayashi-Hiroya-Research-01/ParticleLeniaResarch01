using Assets.ParticleLeniaResarch01.Scripts.Data;
using UnityEngine;

public class PLParticleController : MonoBehaviour
{
    public PLHeatMapGaussController heatMapU;
    public PLHeatMapRepController heatMapR;

    public float e;
    public float g;
    public float r;

    private new Renderer renderer;
    private Material material;
    private ParticleInfo particleInfo;
    private PLColorModeController colorModeController;

    private void OnEnable()
    {
        renderer = GetComponent<Renderer>();
        material = renderer.material;
    }

    public void Setup(ParticleInfo particleInfo, PLColorModeController colorModeController)
    {
        this.particleInfo = particleInfo;
        this.colorModeController = colorModeController;
        heatMapU.Setup(particleInfo, colorModeController);
        heatMapR.Setup(particleInfo, colorModeController);
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

        material.color = colorModeController.GetColor(particleInfo, out _);

        if (pos.Length < 3)
        {
            var z = colorModeController.GetRate(particleInfo, PLColorModeController.ColorMode.E);
            posV[2] = 10f * (1.0f - z);
        }

        transform.position = posV;

        e = (float)particleInfo.LastE;
        g = (float)particleInfo.lastGt;
        r = (float)particleInfo.lastRt;
    }
}