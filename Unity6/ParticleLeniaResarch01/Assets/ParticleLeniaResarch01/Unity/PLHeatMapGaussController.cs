using Assets.ParticleLeniaResarch01.Scripts.Data;
using Assets.ParticleLeniaResarch01.Scripts.Fields;
using UnityEngine;

public class PLHeatMapGaussController : MonoBehaviour
{
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
        material.SetFloat("_mu", (float)particleInfo.fields.GetProperty(ParticleFieldsOriginal.PropertyNames.K_mu));
        material.SetFloat("_sigma", (float)particleInfo.fields.GetProperty(ParticleFieldsOriginal.PropertyNames.K_sigma));
        material.SetFloat("_sigma", (float)particleInfo.fields.GetProperty(ParticleFieldsOriginal.PropertyNames.K_sigma));
        material.SetFloat("_Alpha", (float)particleInfo.fields.GetProperty(ParticleFieldsOriginal.PropertyNames.WK));
    }

    private void Update()
    {
        var wk = (float)particleInfo.fields.GetProperty(ParticleFieldsOriginal.PropertyNames.WK);
        material.SetFloat("_Alpha", wk * colorModeController.utHeatMapAlpha);
    }
}