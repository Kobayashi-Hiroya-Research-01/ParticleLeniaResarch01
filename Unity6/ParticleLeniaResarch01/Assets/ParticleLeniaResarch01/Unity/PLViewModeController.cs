using Assets.ParticleLeniaResarch01.Scripts.Data;
using System;
using UnityEngine;

public class PLViewModeController : MonoBehaviour
{
    public enum ColorMode
    {
        E,
        Gt,
        Rt,
        GtRt,
        Ut,
        Solid,
    }

    [Serializable]
    public class MinMax
    {
        public float min;
        public float max;

        public MinMax(float min, float max)
        {
            this.min = min;
            this.max = max;
        }

        public void Reset()
        {
            min = float.MaxValue;
            max = float.MinValue;
        }

        public void Update(float value)
        {
            if (value < min) min = value;
            if (value > max) max = value;
        }
    }

    [Serializable]
    public class AfterEffectOptions
    {
        public Material afterEffect;

        public enum Mode
        {
            None,
            E,
            Gt,
        }

        public Mode mode;
        public float mu = 0.6f;
        public float sigma = 0.15f;
        public float offset = 0f;
        public float weight = 1f;

        public static string Name_mu => $"_{nameof(mu)}";
        public static string Name_sigma => $"_{nameof(sigma)}";
        public static string Name_offset => $"_{nameof(offset)}";
        public static string Name_weight => $"_{nameof(weight)}";
        public static string Name_through => "_through";

        public void EffectUpdate(PLViewModeController feedback)
        {
            if (mode == Mode.None)
            {
                afterEffect.SetFloat(Name_through, 1.0f);
                return;
            }

            afterEffect.SetFloat(Name_through, 0.0f);
            afterEffect.SetFloat(Name_mu, mu);
            afterEffect.SetFloat(Name_sigma, sigma);
            afterEffect.SetFloat(Name_offset, offset);
            afterEffect.SetFloat(Name_weight, weight);

            switch (mode)
            {
                case Mode.E:
                    feedback.rtHeatMapAlpha = 1.0f;
                    feedback.utHeatMapAlpha = 1.0f;
                    break;

                case Mode.Gt:
                    feedback.rtHeatMapAlpha = 0.0f;
                    feedback.utHeatMapAlpha = 1.0f;
                    break;
            }
        }
    }

    public ColorMode colorMode;

    public Color solidColor;
    public Gradient eGradient;
    public Gradient gtGradient;
    public Gradient rtGradient;
    public Gradient utGradient;

    [Range(0f, 100f)]
    public float utHeatMapAlpha = 1.0f;

    [Range(0f, 100f)]
    public float rtHeatMapAlpha = 1.0f;

    [Range(0f, 1f)]
    public float coreSize = 1.0f;

    public bool autoRange = true;

    public MinMax eMinMax = new(0, 1);
    public MinMax rtMinMax = new(0, 1);
    public MinMax gtMinMax = new(0, 1);
    public MinMax utMinMax = new(0, 1);

    public AfterEffectOptions afterEffectOptions;

    public void ColorReset()
    {
        if (autoRange)
        {
            eMinMax.Reset();
            rtMinMax.Reset();
            gtMinMax.Reset();
            utMinMax.Reset();
        }
    }

    public void ColorUpdate(ParticleInfo particle)
    {
        if (autoRange)
        {
            var e = (float)particle.LastE;
            var g = (float)particle.lastGt;
            var r = (float)particle.lastRt;
            var u = (float)particle.lastUt;
            eMinMax.Update(e);
            gtMinMax.Update(g);
            rtMinMax.Update(r);
            utMinMax.Update(u);
        }
    }

    public Color GetColor(ParticleInfo info)
    {
        switch (colorMode)
        {
            case ColorMode.Solid:
                return solidColor;

            case ColorMode.GtRt:
                var rateGt = GetRate(info, ColorMode.Gt);
                var rateRt = GetRate(info, ColorMode.Rt);

                var colorGt = gtGradient.Evaluate(rateGt);
                var colorRt = rtGradient.Evaluate(rateRt);

                return colorGt + colorRt;
        }

        var rate = GetRate(info);

        return colorMode switch
        {
            ColorMode.E => eGradient.Evaluate(rate),
            ColorMode.Rt => rtGradient.Evaluate(rate),
            ColorMode.Gt => gtGradient.Evaluate(rate),
            ColorMode.Ut => utGradient.Evaluate(rate),
            _ => throw new System.ArgumentOutOfRangeException(nameof(colorMode)),
        };
    }

    public float GetRate(ParticleInfo info)
    {
        return GetRate(info, colorMode);
    }

    public float GetRate(ParticleInfo info, ColorMode colorMode)
    {
        float value;
        MinMax minMax;

        switch (colorMode)
        {
            case ColorMode.Solid:
                return 0;

            case ColorMode.E:
                value = (float)info.LastE;
                minMax = eMinMax;
                break;

            case ColorMode.Rt:
                value = (float)info.lastRt;
                minMax = rtMinMax;
                break;

            case ColorMode.Gt:
                value = (float)info.lastGt;
                minMax = gtMinMax;
                break;

            case ColorMode.Ut:
                value = (float)info.lastUt;
                minMax = utMinMax;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(colorMode));
        }

        var range = minMax.max - minMax.min;
        if (range == 0.0f) return 0.0f;
        var rate = Mathf.Clamp01((value - minMax.min) / range);
        return rate;
    }

    private void OnEnable()
    {
        afterEffectOptions.EffectUpdate(this);
    }

    private void OnValidate()
    {
        afterEffectOptions.EffectUpdate(this);
    }
}