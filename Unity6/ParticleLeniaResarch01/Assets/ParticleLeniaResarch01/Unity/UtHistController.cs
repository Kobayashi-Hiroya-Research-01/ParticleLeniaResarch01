using Assets.ParticleLeniaResarch01.Analyzers;
using Assets.ParticleLeniaResarch01.Scripts.Data;
using Assets.ParticleLeniaResarch01.Scripts.MathObjects;
using System;
using System.Collections.Generic;
using UnityEngine;
using XCharts.Runtime;

public class UtHistController : MonoBehaviour
{
    public LineChart chart;
    public MinMaxAnalyzer minMax;

    [Range(10f, 200f)]
    public int count;

    public bool zeroToOne;

    [Range(0, 30)]
    public int movingAverageRange = 0;

    private List<SerieData> utData;
    private List<SerieData> eData;
    private List<SerieData> uteData;
    private MinMax utMinMax;
    private MinMax eMinMax;

    private double utRange;
    private double eRange;

    private double weight;

    private void Awake()
    {
        /*
        if (minMax == null) minMax = FindFirstObjectByType<PLMinMax>();
        utMinMax ??= new(0, 1);
        eMinMax ??= new(0, 1);
        */
    }

    private void OnEnable()
    {
        /*
        if (minMax == null) minMax = FindFirstObjectByType<PLMinMax>();
        utMinMax ??= new(0, 1);
        eMinMax ??= new(0, 1);
        */
    }

    public void SimulationInit()
    {
        weight = 1.0 / (movingAverageRange + 1);

        utMinMax ??= new(minMax.UtMinMax);
        eMinMax ??= new(minMax.EMinMax);
        utRange = utMinMax.Range;
        eRange = eMinMax.Range;

        uteData = chart.series[0].data;
        utData = chart.series[1].data;
        eData = chart.series[2].data;

        ResetInfo(utRange, utMinMax.Min, utData);
        ResetInfo(eRange, eMinMax.Min, eData);
        uteData.Clear();
    }

    private void ResetInfo(double range, double min, List<SerieData> data)
    {
        double dx = zeroToOne ? (1.0 / count) : (range / count);
        double _min = zeroToOne ? 0.0 : min;
        const double Y = 0.0;

        var listCount = count + movingAverageRange + 3;

        Resize(data, listCount);

        for (int i = 0; i < data.Count; i++)
        {
            var x = _min + (i - movingAverageRange * 0.5) * dx;
            var d = data[i];

            d.data ??= new List<double>();

            while (d.data.Count < 2)
            {
                d.data.Add(0.0);
            }

            d.data[0] = x;
            d.data[1] = Y;
        }
    }

    private static void Resize(List<SerieData> data, int count)
    {
        while (data.Count > count)
        {
            data.RemoveAt(data.Count - 1);
        }

        while (data.Count < count)
        {
            data.Add(new SerieData());
        }
    }

    public void LastPostAction(ParticleInfo particleInfo)
    {
        PostAction(particleInfo);
    }

    public void InitPostAction(ParticleInfo particleInfo)
    {
        PostAction(particleInfo);
    }

    public void PostAction(ParticleInfo particleInfo)
    {
        var ut = particleInfo.lastUt;
        var utMin = utMinMax.Min;
        var ut_01 = (ut - utMin) / utRange;
        var utIndex = Mathf.Clamp((int)Math.Floor(ut_01 * count) + 1, 1, utData.Count - 2);
        utData[utIndex].data[1] += weight;

        var e = particleInfo.LastE;
        var eMin = eMinMax.Min;
        var e_01 = (e - eMin) / eRange;
        var eIndex = Mathf.Clamp((int)Math.Floor(e_01 * count) + 1, 1, eData.Count - 2);
        eData[eIndex].data[1] += weight;

        if (zeroToOne)
        {
            var px = particleInfo.position[0];
            var py = particleInfo.position[1];
            var pos = new List<double>() { ut_01, px + py * 100 };
            uteData.Add(new SerieData() { data = pos });
        }
        else
        {
            var px = particleInfo.position[0];
            var py = particleInfo.position[1];
            var pos = new List<double>() { particleInfo.lastUt, px + py * 100 };
            uteData.Add(new SerieData() { data = pos, itemStyle = { color = Color.blue } });
        }

        for (int i = 1; i < movingAverageRange; i++)
        {
            var ut_i1 = Mathf.Clamp(utIndex + i, 1, utData.Count - 2);
            utData[ut_i1].data[1] += weight;

            var e_i1 = Mathf.Clamp(eIndex + i, 1, eData.Count - 2);
            eData[e_i1].data[1] += weight;
        }
    }
}