namespace Assets.ParticleLeniaResarch01.Scripts.Fields
{
    public interface IParticleFields
    {
        double K(double r);

        double G(double u);

        double R(double r);

        double GetProperty(string name);
    }
}