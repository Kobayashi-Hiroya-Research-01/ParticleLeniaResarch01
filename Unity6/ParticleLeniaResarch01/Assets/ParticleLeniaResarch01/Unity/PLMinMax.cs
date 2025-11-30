using Assets.ParticleLeniaResarch01.Scripts.Data;
using System;
using System.Xml.Serialization;
using UnityEngine;
using static PLMinMax;

public class PLMinMax : MonoBehaviour
{
    [Serializable]
    public class MinMax
    {
        public double min;
        public double max;

        public double Range => max - min;
        public double Center => min + Range * 0.5;

        public MinMax(double min, double max)
        {
            this.min = min;
            this.max = max;
        }

        public void Reset()
        {
            min = double.MaxValue;
            max = double.MinValue;
        }

        public void Update(double value)
        {
            if (value < min) min = value;
            if (value > max) max = value;
        }

        public double GetRate(double value)
        {
            if (max == min) return 0.0;
            return (value - min) / (max - min);
        }
    }

    public MinMax eMinMax = new(0, 1);
    public MinMax rtMinMax = new(0, 1);
    public MinMax gtMinMax = new(0, 1);
    public MinMax utMinMax = new(0, 1);
    public MinMax xMinMax = new(0, 0);
    public MinMax yMinMax = new(0, 0);

    public void SimulationInit()
    {
        eMinMax.Reset();
        rtMinMax.Reset();
        gtMinMax.Reset();
        utMinMax.Reset();
        xMinMax.Reset();
        yMinMax.Reset();
    }

    public void LastPostAction(ParticleInfo info)
    {
        PostUpdate(info);
    }

    public void InitPostAction(ParticleInfo info)
    {
        PostUpdate(info);
    }

    public void IntermediatePostAction(ParticleInfo info)
    {
        PostUpdate(info);
    }

    private void PostUpdate(ParticleInfo info)
    {
        var e = info.LastE;
        var g = info.lastGt;
        var r = info.lastRt;
        var u = info.lastUt;
        var x = info.position[0];
        var y = info.position[1];
        eMinMax.Update(e);
        gtMinMax.Update(g);
        rtMinMax.Update(r);
        utMinMax.Update(u);
        xMinMax.Update(x);
        yMinMax.Update(y);
    }
}