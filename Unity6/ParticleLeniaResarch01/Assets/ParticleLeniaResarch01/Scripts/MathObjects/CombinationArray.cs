namespace Assets.ParticleLeniaResarch01.Scripts.MathObjects
{
    public static class CombinationArray
    {
        public static T[] New<T>(int Length)
        {
            var newDistanceLength = GetLength(Length);
            return new T[newDistanceLength];
        }

        public static void Resize<T>(ref T[] array, int newLength)
        {
            var newDistanceLength = GetLength(newLength);
            if (array == null || array.Length != newDistanceLength)
            {
                array = new T[newDistanceLength];
            }
        }

        public static int GetLength(int length)
        {
            //return GetCombinationIndex(length1 - 1, length1 - 2) + 1;
            return (length - 1) * length >> 1;
        }

        public static int GetIndex(int indexA, int indexB)
        {
            int max, range;
            if (indexA < indexB)
            {
                max = indexB;
                range = indexB - indexA;
            }
            else
            {
                max = indexA;
                range = indexA - indexB;
            }

            return (max * (max + 1) >> 1) - range;
        }

        public static ref T CombinationElement<T>(this T[] array, int indexA, int indexB)
        {
            return ref array[GetIndex(indexA, indexB)];
        }
    }
}