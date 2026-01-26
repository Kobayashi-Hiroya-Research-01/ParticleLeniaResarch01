using Assets.ParticleLeniaResarch01.Scripts.Data;
using Assets.ParticleLeniaResarch01.Scripts.Fields;
using Assets.ParticleLeniaResarch01.Scripts.MathObjects;
using MathNet.Numerics;
using MathNet.Numerics.Integration;
using MathNet.Numerics.Random;
using System;
using System.Collections.Generic;

namespace Assets.ParticleLeniaResarch01.Scripts.Initializers
{
    public class MultiParticleInitializer : IParticleLeniaInitializer
    {
        [Serializable]
        public class State
        {
            public ParticleFieldsOriginal.State fields = new()
            {
                kernelGauss = new(4.0, 1.0),
                wk = 0.022,
                growthGauss = new(0.6, 0.15),
                cRep = 1.0,
            };

            public double[] muKValues = Array.Empty<double>();
            public double[] muGValues = Array.Empty<double>();

            public double deltaTime = 0.1;
            public int particleCount = 200;
            public int dimensions = 2;
            public double area = 12;
            public int seed = 210;
            public bool autoWk;

            public ParticleFieldsOriginal[] CreateParticleFields()
            {
                var list = new List<ParticleFieldsOriginal>(muKValues.Length * muGValues.Length);

                foreach (var muKVal in muKValues)
                {
                    var k = new Gauss.State(fields.kernelGauss.mu * muKVal, fields.kernelGauss.sigma);
                    var wk = autoWk ? CalcW(k.mu, k.sigma, dimensions) : fields.wk;

                    foreach (var muGVal in muGValues)
                    {
                        var g = new Gauss.State(fields.growthGauss.mu * muGVal, fields.growthGauss.sigma);
                        var f = new ParticleFieldsOriginal.State(k, wk, g, fields.cRep);
                        list.Add(new(f));
                    }
                }

                return list.ToArray();
            }

            public static double CalcW(double mu, double sigma, int dim)
            {
                // 標準正規分布の CDFΦ(n) は指数関数的に1に近づくので、
                // 1-Φ(10)≒e^50≒1.93 * 10^-22
                // 浮動小数の精度が10^−16である事から、10σで事実上の計算可能面積全てになる？
                // 実際に試すと、 10σで値の変化がerror以下になったので、これを採用する。
                const double area = 10;
                const double error = 1e-10;
                var min = mu - sigma * area; // Math.Max(0.0, mu - sigma * area);
                var max = mu + sigma * area;
                var sigma_Inv = 1.0 / sigma;

                // n次元超球座標系の体積要素（ヤコビアン）から、
                // n次元単位超球の（n-1次元）球表面要素 w_f、及び PDF(x) * r^(n-1) の面積から、境界体積（n-1次元）を求める。
                // GaussLegendreRule.Integrate() : ガウス・ルジャンドル求積法による数値積分。
                // デフォルトで15次オーダー
                // https://encyclopediaofmath.org/wiki/Gauss-Kronrod_quadrature_formula
                var w_f = 2 * Math.Pow(Constants.Pi, dim * 0.5) / SpecialFunctions.Gamma(dim * 0.5);
                var f = w_f * GaussKronrodRule.Integrate(g, min, max, out _, out _, error);
                var w = 1.0 / f;
                return w;

                // 実際に積分される関数
                double g(double r)
                {
                    var a = (r - mu) * sigma_Inv;
                    var g = Math.Exp(-a * a) * Math.Pow(Math.Abs(r), dim - 1);
                    return g;
                }
            }
        }

        private State state = new();

        public Gauss LeniaFieldGauss { get => new(state.fields.kernelGauss); }
        public double LeniaFieldWeight { get => state.fields.wk; }
        public Gauss GrowthFieldGauss { get => new(state.fields.growthGauss); }
        public double CRep { get => state.fields.cRep; }
        public double DeltaTime { get => state.deltaTime; }
        public int ParticleCount { get => state.particleCount; }
        public int Dimensions { get => state.dimensions; }
        public double Area { get => state.area; }
        public int Seed { get => state.seed; }

        public void SetState(State state)
        {
            this.state = state;
        }

        public ParticleInfoArray CreateParticles()
        {
            var fieldsList = state.CreateParticleFields();

            var ret = new ParticleInfo[ParticleCount];
            var random = new MersenneTwister(Seed);
            var random2 = new MersenneTwister(Seed * 2);
            for (int i = 0; i < ret.Length; i++)
            {
                var pos = new double[Dimensions];

                for (int j = 0; j < pos.Length; j++)
                {
                    var rnd = random.NextDouble();
                    pos[j] = Area * (rnd - 0.5);
                }

                ret[i] = new ParticleInfo(fieldsList[random2.Next(0, fieldsList.Length)], pos, 0.0, 0.0, 0.0);
            }

            return new ParticleInfoArray(ret, 0, 0);
        }
    }
}