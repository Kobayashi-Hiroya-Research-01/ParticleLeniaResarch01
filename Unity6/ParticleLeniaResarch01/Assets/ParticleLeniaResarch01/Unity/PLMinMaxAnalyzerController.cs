using Assets.ParticleLeniaResarch01.Analyzers;
using MathObjects = Assets.ParticleLeniaResarch01.Scripts.MathObjects;
using System;

public class PLMinMaxAnalyzerController : PLAnalyzerController
{
    [Serializable]
    public class MinMax
    {
        public double min;
        public double max;

        public void Set(MathObjects.MinMax minMax)
        {
            min = minMax.Min;
            max = minMax.Max;
        }
    }

    public MinMax eMinMax = new();
    public MinMax rtMinMax = new();
    public MinMax gtMinMax = new();
    public MinMax utMinMax = new();
    public MinMax xMinMax = new();
    public MinMax yMinMax = new();

    private MinMaxAnalyzer minMaxAnalyzer;

    public override IParticleLeniaAnalyzer Analyzer => minMaxAnalyzer ??= new();

    private void Update()
    {
        minMaxAnalyzer ??= new();

        eMinMax.Set(minMaxAnalyzer.EMinMax);
        rtMinMax.Set(minMaxAnalyzer.RtMinMax);
        gtMinMax.Set(minMaxAnalyzer.GtMinMax);
        utMinMax.Set(minMaxAnalyzer.UtMinMax);
        xMinMax.Set(minMaxAnalyzer.XMinMax);
        yMinMax.Set(minMaxAnalyzer.YMinMax);
    }
}