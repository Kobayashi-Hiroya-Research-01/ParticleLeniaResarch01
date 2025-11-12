using System;
using System.Collections;
using System.Collections.Generic;

namespace Assets.ParticleLenia.Scripts
{
    public readonly ref struct Span2D<T>
    {
        public readonly Span<T> array;

        public readonly int length1;

        public readonly int length2;

        public static int GetRawSize(int length1, int length2)
        {
            return length1 * length2;
        }

        public Span2D(Span<T> positions_array, int length1, int length2)
        {
            if (positions_array.Length != length1 * length2)
            {
                throw new ArgumentException("Span2D positions_array.Length != length1 * length2");
            }
            array = positions_array;
            this.length1 = length1;
            this.length2 = length2;
        }

        public Span<T> this[int i]
        {
            get
            {
                return array.Slice(i * length2, length2);
            }
            set
            {
                value.CopyTo(array.Slice(i * length2, length2));
            }
        }

        public ref T this[int i, int j] => ref this[i][j];
    }
}