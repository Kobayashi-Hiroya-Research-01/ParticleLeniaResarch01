using Assets.ParticleLeniaResarch01.Analyzers;
using UnityEngine;

public abstract class PLAnalyzerController : MonoBehaviour
{
    public abstract IParticleLeniaAnalyzer Analyzer { get; }
}
