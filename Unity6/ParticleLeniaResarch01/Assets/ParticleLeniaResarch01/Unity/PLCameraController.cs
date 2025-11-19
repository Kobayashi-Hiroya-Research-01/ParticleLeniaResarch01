using System.Collections.Generic;
using UnityEngine;

public class PLCameraController : MonoBehaviour
{
    public PLCameraModeController modeController;

    public new Camera camera;
    public Renderer grid;

    public void Setup(List<PLParticleController> particles)
    {
        modeController.Setup(particles);
    }

    public void Awake()
    {
        modeController = modeController != null ? modeController : FindFirstObjectByType<PLCameraModeController>();
    }

    private void OnEnable()
    {
        modeController = modeController != null ? modeController : FindFirstObjectByType<PLCameraModeController>();
    }

    // Update is called once per frame
    private void Update()
    {
        modeController.CameraUpdate(camera);
        modeController.GridUpdate(camera, grid, grid.sharedMaterial);
    }
}