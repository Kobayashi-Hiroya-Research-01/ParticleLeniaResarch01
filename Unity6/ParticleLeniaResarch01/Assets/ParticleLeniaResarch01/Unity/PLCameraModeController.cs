using Assets.ParticleLeniaResarch01.Analyzers;
using System;
using UnityEngine;

public class PLCameraModeController : MonoBehaviour
{
    [Serializable]
    public class GridOption
    {
        public bool enabled;

        [Range(1f, 100f)]
        public float space;

        public Color color;
    }

    public GridOption gridOption;

    public bool chaseCenter;

    [Range(0.001f, 1f)]
    public float chaseSpeed;

    public Vector2 offset;

    private Vector2? defaultOffset;

    [Min(0.001f)]
    public float scale = 10f;

    public void CameraUpdate(Camera camera, MinMaxAnalyzer minMaxAnalyzer)
    {
        if (minMaxAnalyzer.XMinMax.Range < 0) return;
        if (minMaxAnalyzer.YMinMax.Range < 0) return;

        if (defaultOffset == null) defaultOffset = camera.transform.position;

        if (chaseCenter && minMaxAnalyzer != null)
        {
            var center = new Vector3((float)minMaxAnalyzer.XMinMax.Center, (float)minMaxAnalyzer.YMinMax.Center);

            center.x += defaultOffset.Value.x + offset.x;
            center.y += defaultOffset.Value.y + offset.y;
            center.z = camera.transform.position.z;

            var center_view = camera.WorldToViewportPoint(center) - new Vector3(0.5f, 0.5f, 0.0f);
            var chaseSpeedX = 0f;
            var chaseSpeedY = 0f;
            var dx = Mathf.Abs(center_view.x) - 0.25f;
            var dy = Mathf.Abs(center_view.y) - 0.25f;
            if (dx > 0f) chaseSpeedX = Mathf.Clamp01(chaseSpeed + dx * 1f);
            if (dy > 0f) chaseSpeedY = Mathf.Clamp01(chaseSpeed + dy * 1f);
            var chaseSpeed2 = Mathf.Max(chaseSpeed, chaseSpeedX, chaseSpeedY);

            var dist = center - camera.transform.position;

            camera.transform.position += chaseSpeed2 * dist;
        }
        camera.orthographicSize = scale;
    }

    public void GridUpdate(Camera camera, Renderer grid, Material gridMaterial)
    {
        if (gridOption.enabled != grid.enabled)
        {
            grid.enabled = gridOption.enabled;
        }

        if (gridOption.enabled)
        {
            gridMaterial.SetColor("_Color", gridOption.color);
            gridMaterial.SetFloat("_Space", gridOption.space);
            var gridScale = grid.transform.localScale;
            gridScale.y = camera.orthographicSize * 2;
            gridScale.x = gridScale.y * camera.pixelWidth / camera.pixelHeight;
            grid.transform.localScale = gridScale;
        }
    }
}