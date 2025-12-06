using System;
using System.Linq;

namespace Assets.ParticleLeniaResarch01.Scripts.Data
{
    /// <summary>
    /// Œ»İ‚Ì—±qó‘Ô
    /// </summary>
    public class ParticleInfoArray
    {
        [Serializable]
        public struct Record
        {
            public double time;
            public int frame;
            public double totalE;
            public ParticleInfo.Record[] particles;

            public Record(ParticleInfo.Record[] particles, double time, int frame)
            {
                this.time = time;
                this.frame = frame;
                totalE = particles.Sum(e => e.E);
                this.particles = particles;
            }

            public readonly ParticleInfoArray ToRaw(IParticleFieldsSerializer serializer)
            {
                var array = particles.Select(e => e.ToRaw(serializer)).ToArray();
                return new ParticleInfoArray(array, time, frame);
            }
        }

        /// <summary>
        /// ‘S—±q‚Ìó‘Ô
        /// </summary>
        private readonly ParticleInfo[] array;

        public Span<ParticleInfo> Array => array;

        public double TotalE => array.Sum(e => e.LastE);

        public double Time { get; set; }
        public int Step { get; set; }

        public ref ParticleInfo this[int index] => ref array[index];

        public ParticleInfoArray(ParticleInfo[] array, double time, int step)
        {
            this.array = array;
            Time = time;
            Step = step;
        }

        public ParticleInfoArray(int length, double time, int frame) : this(new ParticleInfo[length], time, frame)
        {
        }

        public Record ToRecord(IParticleFieldsSerializer serializer)
        {
            var array = this.array.Select(e => e.ToRecord(serializer)).ToArray();
            return new Record(array, Time, Step);
        }
    }
}