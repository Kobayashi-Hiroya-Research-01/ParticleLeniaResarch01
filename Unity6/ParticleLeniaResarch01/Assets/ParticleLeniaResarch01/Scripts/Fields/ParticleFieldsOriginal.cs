using Assets.ParticleLeniaResarch01.Scripts.MathObjects;
using System;

namespace Assets.ParticleLeniaResarch01.Scripts.Fields
{
    public class ParticleFieldsOriginal : IParticleFields
    {
        public static class PropertyNames
        {
            public const string K_mu = "KGauss.mu";
            public const string K_sigma = "KGauss.sigma";
            public const string WK = "Wk";
            public const string G_mu = "GGauss.mu";
            public const string G_sigma = "GGauss.sigma";
            public const string C_Rep = "Rep.C_rep";
        }

        [Serializable]
        public struct State
        {
            public Gauss.State kernelGauss;
            public double wk;
            public Gauss.State growthGauss;
            public double cRep;

            public State(in Gauss.State kGauss, double wk, in Gauss.State gGauss, double cRep)
            {
                this.kernelGauss = kGauss;
                this.wk = wk;
                this.growthGauss = gGauss;
                this.cRep = cRep;
            }
        }

        public Gauss KGauss { get; set; }
        public double Wk { get; set; }
        public Gauss GGauss { get; set; }
        public RepulsionField Rep { get; set; }

        public ParticleFieldsOriginal(in State state)
        {
            KGauss = new(state.kernelGauss);
            Wk = state.wk;
            GGauss = new(state.growthGauss);
            Rep = new(state.cRep);
        }

        public double G(double u)
        {
            return GGauss.F(u);
        }

        public double K(double r)
        {
            return Wk * KGauss.F(r);
        }

        public double R(double r)
        {
            return Rep.R(r);
        }

        public double GetProperty(string name)
        {
            return name switch
            {
                PropertyNames.K_mu => KGauss.mu,
                PropertyNames.K_sigma => KGauss.sigma,
                PropertyNames.WK => Wk,
                PropertyNames.G_mu => GGauss.mu,
                PropertyNames.G_sigma => GGauss.sigma,
                PropertyNames.C_Rep => Rep.C_rep,
                _ => throw new ArgumentException(nameof(name)),
            };
        }
    }
}