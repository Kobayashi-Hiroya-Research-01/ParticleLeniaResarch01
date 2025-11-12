using System;

namespace Assets.ParticleLenia.Scripts
{
    public readonly ref struct DistanceCache
    {
        public readonly Span<double> distances;
        public readonly Span2D<double> positions;

        /// <summary>
        ///
        /// </summary>
        /// <param name="distances">result : GetCombinationLength() でサイズを取得して初期化してください。</param>
        /// <param name="positions">length1 = 粒子数, length2 = 次元数</param>
        ///
        public DistanceCache(Span<double> distances, Span2D<double> positions)
        {
            if (distances.Length != GetCombinationLength(positions.length1))
            {
                throw new ArgumentException("DistanceCache distances.Length != GetCombinationLength(positions.length1)");
            }

            this.distances = distances;
            this.positions = positions;
        }

        public void Reset()
        {
            for (int i = 0; i < distances.Length; i++)
            {
                distances[i] = -1.0;
            }
        }

        public readonly double Distance(int index, ReadOnlySpan<double> position)
        {
            return Distance(positions[index], position);
        }

        public readonly double Distance(int indexA, int indexB)
        {
            if (indexA == indexB) return 0.0;

            int index = GetCombinationIndex(indexA, indexB);
            var distance = distances[index];

            if (distance < 0.0f)
            {
                distance = Distance(positions[indexA], positions[indexB]);
                distances[index] = distance;
            }

            return distance;
        }

        public static int GetCombinationLength(int length1)
        {
            return (length1 * (length1 - 1)) >> 1;
        }

        public static int GetCombinationIndex(int indexA, int indexB)
        {
            if (indexA < indexB)
            {
                (indexB, indexA) = (indexA, indexB);
            }

            return (indexA * (indexA + 1) >> 1) - indexA + indexB;
        }

        public static double Distance(ReadOnlySpan<double> a, ReadOnlySpan<double> b)
        {
            double sum = 0.0;

            for (int i = 0; i < a.Length; i++)
            {
                var ai = a[i];
                var bi = b[i];
                var a_minus_b = ai - bi;
                sum += a_minus_b * a_minus_b;
            }

            return Math.Sqrt(sum);
        }
    }
}