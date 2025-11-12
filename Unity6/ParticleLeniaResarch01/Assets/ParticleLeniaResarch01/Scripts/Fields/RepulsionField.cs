using System;

namespace Assets.ParticleLeniaResarch01.Scripts.Fields
{
    /// <summary>
    /// Repulsion Field を決定する構造体。距離に応じた斥力のポテンシャルを決める。
    /// </summary>
    public class RepulsionField

    {
        private double c_rep;
        public double c_rep_half;

        public RepulsionField(double c_rep)
        {
            this.c_rep = c_rep;
            c_rep_half = c_rep * 0.5;
        }

        public double C_rep
        {
            get => c_rep;
            set
            {
                c_rep = value;
                c_rep_half = c_rep * 0.5;
            }
        }

        public double R(double r)
        {
            if (r <= -1.0 || r >= 1.0) return 0.0;
            var m = 1.0 - Math.Abs(r);
            return c_rep_half * m * m;
        }
    }
}