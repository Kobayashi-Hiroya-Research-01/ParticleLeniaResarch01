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

        public void RemoveAll(ReadOnlySpan<int> particles)
        {
            var newArray = new ParticleInfo[array.Length - particles.Length];

            int particlesIndex = 0;
            int newArrayIndex = 0;
            for (int i = 0; i < array.Length; i++)
            {
                if (particlesIndex < particles.Length && i == particles[particlesIndex])
                {
                    particlesIndex++;
                }
                else
                {
                    newArray[newArrayIndex++] = array[i];
                }
            }

            array = newArray[..newArrayIndex];
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