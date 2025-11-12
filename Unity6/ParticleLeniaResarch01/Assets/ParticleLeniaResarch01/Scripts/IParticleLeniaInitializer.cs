namespace Assets.ParticleLeniaResarch01.Scripts.Data
{
    public interface IParticleLeniaInitializer
    {
        public double DeltaTime { get; }

        public int ParticleCount { get; }

        public int Dimensions { get; }

        public ParticleInfoArray CreateParticles();
    }
}