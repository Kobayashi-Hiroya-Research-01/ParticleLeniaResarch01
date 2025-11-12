using UnityEngine;

public class InitController : MonoBehaviour
{
    public ParticleLeniaController particleLenia;
    public PLColorModeController colorMode;
    public PLCameraModeController cameraMode;
    public Camera mainCamera;
    public Renderer grid;

    private void Awake()
    {
        Setup();
    }

    private void OnEnable()
    {
        Setup();
    }

    private void Setup()
    {
        particleLenia.colorModeController = colorMode;
        particleLenia.cameraModeController = cameraMode;
        cameraMode.camera = mainCamera;
        cameraMode.grid = grid;
    }
}