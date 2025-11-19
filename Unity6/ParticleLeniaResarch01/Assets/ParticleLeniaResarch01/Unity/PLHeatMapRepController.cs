using Assets.ParticleLeniaResarch01.Scripts.Data;
using Assets.ParticleLeniaResarch01.Scripts.Fields;
using UnityEngine;

public class PLHeatMapRepController : MonoBehaviour
{
    private new Renderer renderer;
    private Material material;

    private PLViewModeController viewModeController;

    private void OnEnable()
    {
        renderer = GetComponent<Renderer>();
        material = renderer.material;
    }

    public void Setup(ParticleInfo particleInfo, PLViewModeController viewModeController)
    {
        this.viewModeController = viewModeController;
        material.SetFloat("_CRep", (float)particleInfo.fields.GetProperty(ParticleFieldsOriginal.PropertyNames.C_Rep));
    }

    private void Update()
    {
        material.SetFloat("_Alpha", viewModeController.rtHeatMapAlpha);
    }
}