using Assets.ParticleLenia.Scripts;
using Assets.ParticleLeniaResarch01.Scripts.Fields;
using System;
using System.Linq;
using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

namespace Assets.ParticleLeniaResarch01.Scripts.Data
{
    /// <summary>
    /// åªç›ÇÃó±éqèÛë‘
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

            public readonly ParticleInfoArray ToRaw(Func<string, IParticleFields> jsonReader)
            {
                var array = particles.Select(e => e.ToRaw(jsonReader)).ToArray();
                return new ParticleInfoArray(array, time, frame);
            }
        }

        /// <summary>
        /// ëSó±éqÇÃèÛë‘
        /// </summary>
        private readonly ParticleInfo[] array;

        public Span<ParticleInfo> Array => array;

        public double TotalE => array.Sum(e => e.LastE);

        public double Time { get; set; }
        public int Step { get; set; }

        public ref ParticleInfo this[int index] => ref array[index];

        public ParticleInfoArray(ParticleInfo[] array, double time, int frame)
        {
            this.array = array;
            Time = time;
            Step = frame;
        }

        public ParticleInfoArray(int length, double time, int frame) : this(new ParticleInfo[length], time, frame)
        {
        }

        public Record ToRecord(Func<IParticleFields, string> jsonWriter)
        {
            var array = this.array.Select(e => e.ToRecord(jsonWriter)).ToArray();
            return new Record(array, Time, Step);
        }
    }
}