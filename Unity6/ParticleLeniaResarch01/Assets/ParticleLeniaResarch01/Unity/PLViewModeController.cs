using Assets.ParticleLeniaResarch01.Analysis;
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
        Cluster
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

    public AfterEffectOptions afterEffectOptions;

    public Color GetColor(ParticleInfo info, ClusterInfo clusterInfo, PLMinMax plMinMax)
    {
        switch (colorMode)
        {
            case ColorMode.Solid:
                return solidColor;

            case ColorMode.Cluster:

                var id = clusterInfo.clusterID.Value;
                if (id < 0) return Color.gray2;

                // ‰ºˆÊ2bit (0 to 3)
                var low = id & 0x03;

                // ãˆÊ14bit
                var high = id >> 2;

                float h = high * (1f / 16.123456789f);
                h = h % 1.0f;

                float s, v;

                switch (low)
                {
                    default:
                    case 0:
                        s = 0.5f;
                        v = 1.0f;
                        break;

                    case 1:
                        s = 1.0f;
                        v = 1.0f;
                        break;

                    case 2:
                        s = 1.0f;
                        v = 0.75f;
                        break;

                    case 3:
                        s = 1.0f;
                        v = 0.5f;
                        break;
                }

                return Color.HSVToRGB(h, s, v);

            case ColorMode.GtRt:
                var rateGt = GetRate(info, ColorMode.Gt, plMinMax);
                var rateRt = GetRate(info, ColorMode.Rt, plMinMax);

                var colorGt = gtGradient.Evaluate(rateGt);
                var colorRt = rtGradient.Evaluate(rateRt);

                return colorGt + colorRt;
        }

        var rate = GetRate(info, plMinMax);

        return colorMode switch
        {
            ColorMode.E => eGradient.Evaluate(rate),
            ColorMode.Rt => rtGradient.Evaluate(rate),
            ColorMode.Gt => gtGradient.Evaluate(rate),
            ColorMode.Ut => utGradient.Evaluate(rate),
            _ => throw new System.ArgumentOutOfRangeException(nameof(colorMode)),
        };
    }

    public float GetRate(ParticleInfo info, PLMinMax plMinMax)
    {
        return GetRate(info, colorMode, plMinMax);
    }

    public float GetRate(ParticleInfo info, ColorMode colorMode, PLMinMax plMinMax)
    {
        float value;
        PLMinMax.MinMax minMax;

        switch (colorMode)
        {
            case ColorMode.Solid:
                return 0;

            case ColorMode.E:
                value = (float)info.LastE;
                minMax = plMinMax.eMinMax;
                break;

            case ColorMode.Rt:
                value = (float)info.lastRt;
                minMax = plMinMax.rtMinMax;
                break;

            case ColorMode.Gt:
                value = (float)info.lastGt;
                minMax = plMinMax.gtMinMax;
                break;

            case ColorMode.Ut:
                value = (float)info.lastUt;
                minMax = plMinMax.utMinMax;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(colorMode));
        }

        var range = minMax.max - minMax.min;
        if (range == 0.0f) return 0.0f;
        var rate = Mathf.Clamp01((float)((value - minMax.min) / range));
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