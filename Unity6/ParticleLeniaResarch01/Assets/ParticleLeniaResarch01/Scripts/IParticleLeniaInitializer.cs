namespace Assets.ParticleLeniaResarch01.Scripts.Data
{
    public interface IParticleLeniaInitializer
    {
        double DeltaTime { get; }

        int ParticleCount { get; }

        int Dimensions { get; }

        ParticleInfoArray CreateParticles();
    }
}