using Assets.ParticleLeniaResarch01.Scripts.Data;
using UnityEngine;

public class PLColorModeController : MonoBehaviour
{
    public enum ColorMode
    {
        E,
        Rt,
        Gt,
        GtRt,
    }

    public ColorMode colorMode;
    public Gradient eGradient;
    public Gradient gtGradient;
    public Gradient rtGradient;

    [Range(0f, 100f)]
    public float utHeatMapAlpha = 1.0f;

    [Range(0f, 100f)]
    public float rtHeatMapAlpha = 1.0f;

    private float EMax = 0.0f;
    private float RtMax = 0.0f;
    private float GtMax = 0.0f;
    private float EMin = 0.0f;
    private float RtMin = 0.0f;
    private float GtMin = 0.0f;

    public void ColorReset()
    {
        EMax = RtMax = GtMax = float.MinValue;
        EMin = RtMin = GtMin = float.MaxValue;
    }

    public void ColorUpdate(ParticleInfo particle)
    {
        var e = (float)particle.LastE;
        EMax = Mathf.Max(EMax, e);
        EMin = Mathf.Min(EMin, e);
        var g = (float)particle.lastGt;
        GtMax = Mathf.Max(GtMax, g);
        GtMin = Mathf.Min(GtMin, g);
        var r = (float)particle.lastRt;
        RtMax = Mathf.Max(RtMax, r);
        RtMin = Mathf.Min(RtMin, r);
    }

    public Color GetColor(ParticleInfo info, out float rate)
    {
        if (colorMode == ColorMode.GtRt)
        {
            var rateGt = GetRate(info, ColorMode.Gt);
            var rateRt = GetRate(info, ColorMode.Rt);
            rate = 0.5f * (rateRt + rateGt);

            var colorGt = gtGradient.Evaluate(rateGt);
            var colorRt = rtGradient.Evaluate(rateRt);

            return colorGt + colorRt;
        }

        rate = GetRate(info);
        return colorMode switch
        {
            ColorMode.E => eGradient.Evaluate(rate),
            ColorMode.Rt => rtGradient.Evaluate(rate),
            ColorMode.Gt => gtGradient.Evaluate(rate),
            _ => throw new System.ArgumentOutOfRangeException(nameof(colorMode)),
        };
    }

    public float GetRate(ParticleInfo info)
    {
        return GetRate(info, colorMode);
    }

    public float GetRate(ParticleInfo info, ColorMode colorMode)
    {
        float value, max, min;

        switch (colorMode)
        {
            case ColorMode.E:
                value = (float)info.LastE;
                max = EMax;
                min = EMin;
                break;

            case ColorMode.Rt:
                value = (float)info.lastRt;
                max = RtMax;
                min = RtMin;
                break;

            case ColorMode.Gt:
                value = (float)info.lastGt;
                max = GtMax;
                min = GtMin;
                break;

            default:
                throw new System.ArgumentOutOfRangeException(nameof(colorMode));
        }

        if (max - min < float.Epsilon) return 0.0f;
        var rate = Mathf.Clamp01((value - min) / (max - min));
        return rate;
    }
}