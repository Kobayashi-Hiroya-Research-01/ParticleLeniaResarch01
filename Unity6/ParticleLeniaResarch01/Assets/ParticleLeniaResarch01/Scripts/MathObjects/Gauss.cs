namespace Assets.ParticleLeniaResarch01.Scripts.MathObjects
{
    using System;
    using Math = System.Math;

    /// <summary>
    /// ガウス型の形状を決定する構造体
    /// </summary>
    [Serializable]
    public readonly struct Gauss
    {
        [Serializable]
        public struct State
        {
            public double mu;
            public double sigma;

            public State(double mu, double sigma)
            {
                this.mu = mu;
                this.sigma = sigma;
            }
        }

        public readonly double mu;
        public readonly double sigma;

        private readonly double b;

        public Gauss(in State state) : this(state.mu, state.sigma)
        {
        }

        public Gauss(double mu, double sigma)
        {
            this.mu = mu;
            this.sigma = sigma;
            b = 1.0 / (sigma * sigma);
        }

        public double F(double x)
        {
            var a = x - mu;
            return Math.Exp(-a * a * b);
        }
    }
}