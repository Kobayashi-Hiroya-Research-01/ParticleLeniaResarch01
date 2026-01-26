using UnityEngine;
using System.IO;

#if UNITY_EDITOR

using UnityEditor;

#endif

public class FrameRecorder : MonoBehaviour
{
    public int frameInterval = 30;

    public string saveFolder = "Assets/Screenshots";
    public string fileName = "screenshot_frame";

    public bool captureEnabled = true;

    private int lastCapturedFrame = -1;

#if UNITY_EDITOR

    private void Update()
    {
        if (!Application.isPlaying) return;
        if (!captureEnabled) return;
        if (frameInterval <= 0) return;

        int currentFrame = Time.frameCount - 1;

        if (currentFrame % frameInterval == 0 && currentFrame != lastCapturedFrame)
        {
            Debug.Log($"test");

            Capture(currentFrame);
            lastCapturedFrame = currentFrame;
        }
    }

    private void Capture(int frame)
    {
        if (!Directory.Exists(saveFolder))
        {
            Directory.CreateDirectory(saveFolder);
            AssetDatabase.Refresh();
        }

        string filename = $"{saveFolder}/{fileName}_{frame:D06}.png";

        ScreenCapture.CaptureScreenshot(filename);
        Debug.Log($"Screenshot saved: {filename}");
    }

#endif
}