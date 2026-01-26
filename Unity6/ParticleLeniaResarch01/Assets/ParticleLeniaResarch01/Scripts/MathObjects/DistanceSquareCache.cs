using System;

namespace Assets.ParticleLeniaResarch01.Scripts.MathObjects
{
    public struct DistanceSquareCache
    {
        private double[] distances;
        public readonly bool IsNull => distances == null;

        public readonly int Length => distances.Length;

        public readonly ReadOnlySpan<double> Distances => distances;

        public readonly double this[int indexA, ReadOnlySpan<double> posA, int indexB, ReadOnlySpan<double> posB]
        {
            get
            {
                if (indexA == indexB) return 0.0;

                ref var value = ref distances.CombinationElement(indexA, indexB);
                if (value < 0) value = DistanceSquare(posA, posB);
                return value;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="distances">result : GetCombinationLength() でサイズを取得して初期化してください。</param>
        /// <param name="positions">length1 = 粒子数, length2 = 次元数</param>
        ///
        public DistanceSquareCache(int length)
        {
            distances = CombinationArray.New<double>(length);
            Reset();
        }

        public void Reset(int newLength)
        {
            CombinationArray.Resize(ref distances, newLength);
            Reset();
        }

        public readonly void Reset()
        {
            distances.AsSpan().Fill(-1.0);
        }

        public static double DistanceSquare(ReadOnlySpan<double> a, ReadOnlySpan<double> b)
        {
            double sum = 0.0;

            for (int i = 0; i < a.Length; i++)
            {
                var ai = a[i];
                var bi = b[i];
                var d = ai - bi;
                sum += d * d;
            }

            return sum;
        }
    }
}