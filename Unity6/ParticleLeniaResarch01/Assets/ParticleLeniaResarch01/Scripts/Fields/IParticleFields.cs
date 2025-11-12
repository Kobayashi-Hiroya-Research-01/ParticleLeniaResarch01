namespace Assets.ParticleLeniaResarch01.Scripts.Fields
{
    public interface IParticleFields
    {
        public double K(double r);

        public double G(double u);

        public double R(double r);

        public double GetProperty(string name);
    }
}