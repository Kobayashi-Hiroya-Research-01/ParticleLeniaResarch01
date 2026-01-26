using Assets.ParticleLeniaResarch01.Scripts.Data;

namespace Assets.ParticleLeniaResarch01.Scripts
{
    public interface IParticleLeniaInitializer
    {
        double DeltaTime { get; }

        int ParticleCount { get; }

        int Dimensions { get; }

        ParticleInfoArray CreateParticles();
    }
}