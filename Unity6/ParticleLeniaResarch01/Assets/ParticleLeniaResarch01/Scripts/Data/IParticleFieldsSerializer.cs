using Assets.ParticleLeniaResarch01.Scripts.Fields;

namespace Assets.ParticleLeniaResarch01.Scripts.Data
{
    public interface IParticleFieldsSerializer
    {
        IParticleFields Read(string text);

        string Write(IParticleFields fields);
    }
}
