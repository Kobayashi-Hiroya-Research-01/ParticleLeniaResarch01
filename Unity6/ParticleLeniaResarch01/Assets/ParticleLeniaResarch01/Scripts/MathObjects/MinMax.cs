using System;

namespace Assets.ParticleLeniaResarch01.Scripts.MathObjects
{
    [Serializable]
    public class MinMax
    {
        public object Lock { get; } = new();

        public class Writerble : MinMax
        {
            public Writerble() : base()
            {
            }

            public Writerble(MinMax minmax) : base(minmax)
            {
            }

            public Writerble(double min, double max) : base(min, max)
            {
            }

            public void Reset()
            {
                lock (Lock)
                {
                    Min = double.MaxValue;
                    Max = double.MinValue;
                }
            }

            public void Update(double value)
            {
                lock (Lock)
                {
                    if (value < Min) Min = value;
                    if (value > Max) Max = value;
                }
            }
        }

        public double Min { get; protected set; }
        public double Max { get; protected set; }

        public double Range => Max - Min;
        public double Center => Min + Range * 0.5;

        public MinMax() : this(double.MaxValue, double.MinValue)
        {
        }

        public MinMax(MinMax minmax) : this(minmax.Min, minmax.Max)
        {
        }

        private MinMax(double min, double max)
        {
            Min = min;
            Max = max;
        }

        public double GetRate(double value)
        {
            if (Max == Min) return 0.0;
            return (value - Min) / (Max - Min);
        }

        public override string ToString()
        {
            return $"{{ min={Format(Min)}, max={Format(Max)} }}";
        }

        private static string Format(double value)
        {
            return $"{value,8:F5}";
        }
    }
}