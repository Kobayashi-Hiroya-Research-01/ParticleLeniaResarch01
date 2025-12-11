using Assets.ParticleLeniaResarch01.Scripts;
using Assets.ParticleLeniaResarch01.Scripts.Data;

namespace Assets.ParticleLeniaResarch01.Analyzers
{
    public delegate void ParticleLeniaAnalyzerEvent(ParticleInfoArray particles);

    public delegate void ParticleLeniaAnalyzerForeachEvent(ParticleInfo info, int index);
}