using Assets.ParticleLeniaResarch01.Analyzers;
using Assets.ParticleLeniaResarch01.Scripts;
using UnityEngine;

public class PLCameraController : MonoBehaviour
{
    public PLCameraModeController modeController;

    public new Camera camera;
    public Renderer grid;

    public void UpdateCamera(ParticleLeniaAnalyzer dependencies)
    {
        modeController = modeController != null ? modeController : FindFirstObjectByType<PLCameraModeController>();

        var minMaxAnalyzer = dependencies.Search<MinMaxAnalyzer>();
        modeController.CameraUpdate(camera, minMaxAnalyzer);
        modeController.GridUpdate(camera, grid, grid.sharedMaterial);
    }
}