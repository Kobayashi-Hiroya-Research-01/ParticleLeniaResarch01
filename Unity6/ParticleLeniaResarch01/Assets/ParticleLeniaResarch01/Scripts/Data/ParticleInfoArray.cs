using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Assets.ParticleLeniaResarch01.Scripts.Data
{
    /// <summary>
    /// åªç›ÇÃó±éqèÛë‘
    /// </summary>
    public class ParticleInfoArray : IEnumerable<ParticleInfo>
    {
        /// <summary>
        /// ëSó±éqÇÃèÛë‘
        /// </summary>
        private ParticleInfo[] array;

        public ParticleInfoArray(ParticleInfo[] array, double time, int step)
        {
            this.array = array;
            Time = time;
            Step = step;
        }

        public ParticleInfoArray(int length, double time, int frame) : this(new ParticleInfo[length], time, frame)
        {
        }

        public Span<ParticleInfo> Array => array;

        public double TotalE => array.Sum(e => e.LastE);

        public double Time { get; set; }
        public int Step { get; set; }

        public ref ParticleInfo this[int index] => ref array.AsSpan()[index];

        public void CopyTo(ParticleInfoArray other)
        {
            other.Time = Time;
            other.Step = Step;

            if (other.array == null || other.array.Length != array.Length)
            {
                other.array = new ParticleInfo[array.Length];
            }

            for (int i = 0; i < other.array.Length; i++)
            {
                var selfItem = array[i];
                ref var otherItem = ref other.array[i];
                if (otherItem == null)
                {
                    otherItem = new ParticleInfo(selfItem.fields, selfItem.position.ToArray(), selfItem.lastGt, selfItem.lastRt, selfItem.lastUt);
                }
                else
                {
                    selfItem.CopyTo(otherItem);
                }
            }
        }

        public void RemoveAll(IEnumerable<ParticleInfo> particles)
        {
            var list = array.ToList();
            foreach (var p in particles)
            {
                list.Remove(p);
            }
            array = list.ToArray();
        }

        public IEnumerator<ParticleInfo> GetEnumerator()
        {
            return ((IEnumerable<ParticleInfo>)array).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return array.GetEnumerator();
        }
    }
}