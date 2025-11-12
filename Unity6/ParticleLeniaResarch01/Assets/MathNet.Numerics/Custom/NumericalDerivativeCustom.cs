using System;
using MathNet.Numerics.Differentiation;
using MathNet.Numerics;

namespace Assets.MathNet.Numerics.Custom
{
    /// <summary>
    /// Copy from <see cref="Numerics.Differentiation.NumericalDerivative"/>
    /// </summary>
    internal abstract class NumericalDerivativeCustom
    {
        protected readonly int _points;

        private int _center;

        private double _stepSize = Math.Pow(2.0, -10.0);

        private double _epsilon = Precision.PositiveMachineEpsilon;

        private double _baseStepSize = Math.Pow(2.0, -26.0);

        protected readonly FiniteDifferenceCoefficients _coefficients;

        private int _order;

        //
        // 概要:
        //     Sets and gets the finite difference step size. This value is for each function
        //     evaluation if relative step size types are used. If the base step size used in
        //     scaling is desired, see MathNet.Numerics.Differentiation.NumericalDerivative.Epsilon.
        //
        //
        // 注釈:
        //     Setting then getting the StepSize may return a different value. This is not unusual
        //     since a user-defined step size is converted to a base-2 representable number
        //     to improve finite difference accuracy.
        public double StepSize
        {
            get
            {
                return _stepSize;
            }
            set
            {
                double a = Math.Log(Math.Abs(value)) / Math.Log(2.0);
                _stepSize = Math.Pow(2.0, Math.Round(a));
            }
        }

        //
        // 概要:
        //     Sets and gets the base finite difference step size. This assigned value to this
        //     parameter is only used if MathNet.Numerics.Differentiation.NumericalDerivative.StepType
        //     is set to RelativeX. However, if the StepType is Relative, it will contain the
        //     base step size computed from MathNet.Numerics.Differentiation.NumericalDerivative.Epsilon
        //     based on the finite difference order.
        public double BaseStepSize
        {
            get
            {
                return _baseStepSize;
            }
            set
            {
                double a = Math.Log(Math.Abs(value)) / Math.Log(2.0);
                _baseStepSize = Math.Pow(2.0, Math.Round(a));
            }
        }

        //
        // 概要:
        //     Sets and gets the base finite difference step size. This parameter is only used
        //     if MathNet.Numerics.Differentiation.NumericalDerivative.StepType is set to Relative.
        //     By default this is set to machine epsilon, from which MathNet.Numerics.Differentiation.NumericalDerivative.BaseStepSize
        //     is computed.
        public double Epsilon
        {
            get
            {
                return _epsilon;
            }
            set
            {
                double a = Math.Log(Math.Abs(value)) / Math.Log(2.0);
                _epsilon = Math.Pow(2.0, Math.Round(a));
            }
        }

        //
        // 概要:
        //     Sets and gets the location of the center point for the finite difference derivative.
        public int Center
        {
            get
            {
                return _center;
            }
            set
            {
                if (value >= _points || value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", "Center must lie between 0 and points -1");
                }

                _center = value;
            }
        }

        //
        // 概要:
        //     Number of times a function is evaluated for numerical derivatives.
        public int Evaluations { get; protected set; }

        //
        // 概要:
        //     Type of step size for computing finite differences. If set to absolute, dx =
        //     h. If set to relative, dx = (1+abs(x))*h^(2/(order+1)). This provides accurate
        //     results when h is approximately equal to the square-root of machine accuracy,
        //     epsilon.
        public StepType StepType { get; set; } = StepType.Relative;

        public int Order
        {
            get => _order;
            private set
            {
                _order = value;
                double num = _points - _order;
                BaseStepSize = Math.Pow(Epsilon, 1.0 / (num + _order));
            }
        }

        //
        // 概要:
        //     Initializes a NumericalDerivative class with the default 3 point center difference
        //     method.
        public NumericalDerivativeCustom()
            : this(3, 1, 1)
        {
        }

        //
        // 概要:
        //     Initialized a NumericalDerivative class.
        //
        // パラメーター:
        //   points:
        //     Number of points for finite difference derivatives.
        //
        //   center:
        //     Location of the center with respect to other points. Value ranges from zero to
        //     points-1.
        public NumericalDerivativeCustom(int points, int center, int order)
        {
            if (points < 2)
            {
                throw new ArgumentOutOfRangeException("points", "Points must be two or greater.");
            }

            _center = center;
            _points = points;
            Center = center;
            _coefficients = new FiniteDifferenceCoefficients(points);

            Order = order;
        }

        protected double EvaluateDerivative(double sum_mul_coefficients_points, double stepSize, ReadOnlySpan<double> coefficients)
        {
            if (_order >= _points || _order < 0)
            {
                throw new ArgumentOutOfRangeException("order", "Order must be between zero and points-1.");
            }

            return sum_mul_coefficients_points * Math.Pow(stepSize, -_order);

            //return Enumerable.Select(_coefficients.GetCoefficients(Center, order), (double t, int i) => t * points[i]).Sum() / Math.Pow(stepSize, order);
        }

        protected double CalculateStepSize(double x)
        {
            if (StepType == StepType.RelativeX)
            {
                StepSize = BaseStepSize * (1.0 + Math.Abs(x));
            }
            else if (StepType == StepType.Relative)
            {
                StepSize = BaseStepSize * (1.0 + Math.Abs(x));
            }

            return StepSize;
        }
    }
}