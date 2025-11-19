using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets.ParticleLeniaResarch01.Scripts.Data
{
    /// <summary>
    /// ó±éqèÛë‘ÇÃéûånóÒ
    /// </summary>
    public class ParticleLeniaTimeLine
    {
        [Serializable]
        public struct Record
        {
            public string information;
            public List<ParticleInfoArray.Record> timeLine;

            public Record(string information, IEnumerable<ParticleInfoArray.Record> timeLine)
            {
                this.information = information;
                this.timeLine = timeLine.ToList();
            }

            public readonly string Information => information;

            public readonly List<ParticleInfoArray.Record> TimeLine => timeLine;

            public readonly ParticleLeniaTimeLine ToRaw(IParticleFieldsSerializer serializer)
            {
                return new ParticleLeniaTimeLine(information, timeLine, serializer);
            }
        }

        public string Information { get; }
        public List<ParticleInfoArray> TimeLine { get; }

        public ParticleLeniaTimeLine(string information)
        {
            Information = information;
            TimeLine = new();
        }

        public ParticleLeniaTimeLine(string information, IEnumerable<ParticleInfoArray> timeLine) : this(information)
        {
            TimeLine.Capacity = timeLine.Count();
            foreach (var item in timeLine)
            {
                TimeLine.Add(item);
            }
        }

        public ParticleLeniaTimeLine(string information, IEnumerable<ParticleInfoArray.Record> timeLine, IParticleFieldsSerializer serializer) : this(information)
        {
            TimeLine.Capacity = timeLine.Count();
            foreach (var item in timeLine)
            {
                TimeLine.Add(item.ToRaw(serializer));
            }
        }

        public Record ToRecord(IParticleFieldsSerializer serializer)
        {
            var timeLine = TimeLine.Select(e => e.ToRecord(serializer));
            return new Record(Information, timeLine);
        }
    }
}