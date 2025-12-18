// This code was copied from https://github.com/mathnet/mathnet-numerics
// and edited by this project.

using System;

namespace Assets.Packages.MathNet.Numerics.Custom
{
    /// <summary>
    /// Copy from <see cref="Numerics.Differentiation.NumericalDerivative"/>
    /// </summary>
    internal class Grad : NumericalDerivativeCustom
    {
        public interface IValueAtPoint<TArg>
        {
            double GetValueAt(ReadOnlySpan<double> point, TArg arg);
        }

        private readonly double[] coefficients;

        private double[] lastResult;

        public Grad() : this(3, 1)
        {
        }

        public Grad(int points, int center) : base(points, center, 1)
        {
            coefficients = _coefficients.GetCoefficients(center, 1);
        }

        public ReadOnlySpan<double> GetGrad<TValue, TArg>(ReadOnlySpan<double> point, TValue valueAtPoint, TArg arg) where TValue : IValueAtPoint<TArg>
        {
            if (lastResult == null || lastResult.Length != point.Length)
            {
                lastResult = new double[point.Length];
            }

            Span<double> pointsArray = stackalloc double[_points];
            Span<double> x2 = stackalloc double[point.Length];

            for (int i = 0; i < lastResult.Length; i++)
            {
                lastResult[i] = EvaluatePartialDerivative(point, valueAtPoint, arg, i, x2, pointsArray);
            }

            return lastResult;
        }

        /// <summary>
        /// Copy from <see cref="Numerics.Differentiation.NumericalDerivative.EvaluatePartialDerivative(Func{double[], double}, double[], int, int, double?)"/>
        /// </summary>
        private double EvaluatePartialDerivative<TValue, TArg>(ReadOnlySpan<double> point, TValue valueAtPoint, TArg arg, int parameterIndex, Span<double> x2, Span<double> pointsArray)
            where TValue : IValueAtPoint<TArg>
        {
            double x_parameterIndex = point[parameterIndex];
            double stepSize = CalculateStepSize(x_parameterIndex);
            double sum_mul_coefficients_points = 0.0;

            point.CopyTo(x2);

            for (int i = 0; i < _points; i++)
            {
                ref var p = ref pointsArray[i];
                var coefficient = coefficients[i];

                if (coefficient != 0.0)
                {
                    x2[parameterIndex] = x_parameterIndex + (i - Center) * stepSize;
                    p = valueAtPoint.GetValueAt(x2, arg);
                    Evaluations++;
                }

                sum_mul_coefficients_points += coefficient * p;
            }

            return EvaluateDerivative(sum_mul_coefficients_points, stepSize);
        }
    }
}