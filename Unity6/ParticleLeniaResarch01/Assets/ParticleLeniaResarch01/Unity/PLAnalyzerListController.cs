using Assets.ParticleLeniaResarch01.Analyzers;
using System;
using System.Linq;
using UnityEngine;

public class PLAnalyzerListController : MonoBehaviour
{
    public PLAnalyzerController[] particleLeniaAnalyzers = Array.Empty<PLAnalyzerController>();

    public IParticleLeniaAnalyzer[] ParticleLeniaAnalyzers
    {
        get
        {
            var list = particleLeniaAnalyzers ?? Array.Empty<PLAnalyzerController>();
            return list.Where(e => e != null).Select(e => e.Analyzer).ToArray();
        }
    }
}