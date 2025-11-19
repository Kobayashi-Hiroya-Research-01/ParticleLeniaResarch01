using System;
using System.Collections.Generic;
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

    private List<PLParticleController> particles;

    public void Setup(List<PLParticleController> particles)
    {
        this.particles = particles;
    }

    public void CameraUpdate(Camera camera)
    {
        if (chaseCenter && particles != null && particles.Count > 0)
        {
            var n = particles.Count;
            var center = Vector3.zero;

            foreach (var particle in particles)
            {
                center.x += particle.transform.position.x;
                center.y += particle.transform.position.y;
            }

            center = 1.0f / n * center;
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
            var scale = grid.transform.localScale;
            scale.y = camera.orthographicSize * 2;
            scale.x = scale.y * camera.pixelWidth / camera.pixelHeight;
            grid.transform.localScale = scale;
        }
    }
}