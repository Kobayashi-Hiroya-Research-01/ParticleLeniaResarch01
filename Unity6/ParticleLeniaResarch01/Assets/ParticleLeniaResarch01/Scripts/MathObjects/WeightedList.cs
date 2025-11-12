using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Assets.ParticleLeniaResarch01.Scripts.MathObjects
{
    /// <summary>
    /// 重み付き総和の形状の決定と、計算量削減のための最適化機能を提供する構造体
    /// </summary>
    public class WeightedList<T> : IEnumerable<Weighted<T>>, IReadOnlyCollection<Weighted<T>>
    {
        private readonly List<Weighted<T>> list = new();

        public int Count => list.Count;

        public Weighted<T> this[int index] => list[index];

        public void AddRange(IEnumerable<Weighted<T>> other)
        {
            foreach (var itemB in other) Add(itemB);
        }

        public void Add(double w, T value) => Add(w, new T[] { value });

        public void Add(double w, T[] value) => Add(new Weighted<T>(w, value));

        public void Add(Weighted<T> item)
        {
            if (item.w == 0.0) return;

            var itemOld = list.FirstOrDefault(itemOld => itemOld.w == item.w);

            // itemB.w != 0.0 なので Default 確定
            if (itemOld.w == 0.0)
            {
                list.Add(item);
            }
            else
            {
                list.Remove(itemOld);
                var newArray = itemOld.array.Concat(item.array).ToArray();
                itemOld = new Weighted<T>(itemOld.w, newArray);
            }
        }

        public IEnumerator<Weighted<T>> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }
    }
}