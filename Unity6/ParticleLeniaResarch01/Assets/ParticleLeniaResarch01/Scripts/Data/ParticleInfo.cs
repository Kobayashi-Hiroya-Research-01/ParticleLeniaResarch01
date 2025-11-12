using Assets.ParticleLeniaResarch01.Scripts.Fields;
using System;
using System.Linq;

namespace Assets.ParticleLeniaResarch01.Scripts.Data
{
    /// <summary>
    /// ó±éqíPëÃÇÃèÛë‘
    /// </summary>
    public class ParticleInfo
    {
        [Serializable]
        public struct Record
        {
            public string fieldsJson;
            public double[] position;
            public double ut;
            public double gt;
            public double rt;
            public double e;

            public Record(ParticleInfo particleInfo, Func<IParticleFields, string> jsonWriter)
            {
                fieldsJson = jsonWriter(particleInfo.fields);
                position = particleInfo.position.ToArray();
                ut = particleInfo.lastUt;
                gt = particleInfo.lastGt;
                rt = particleInfo.lastRt;
                e = particleInfo.LastE;
            }

            public readonly string FieldsJson => fieldsJson;
            public readonly Span<double> Position => position;
            public readonly double Ut => ut;
            public readonly double Gt => gt;
            public readonly double Rt => rt;
            public readonly double E => e;

            public readonly ParticleInfo ToRaw(Func<string, IParticleFields> jsonReader)
            {
                return new ParticleInfo(this, jsonReader);
            }
        }

        public readonly IParticleFields fields;
        public readonly double[] position;
        public double lastUt;
        public double lastGt;
        public double lastRt;
        public double LastE => lastRt - lastGt;

        public ParticleInfo(in Record record, Func<string, IParticleFields> jsonReader) :
            this(jsonReader(record.fieldsJson), record.position.ToArray(), record.gt, record.rt, record.ut)
        {
        }

        public ParticleInfo(IParticleFields fields, double[] position, double lastGt, double lastRt, double lastUt)
        {
            this.fields = fields;
            this.position = position;
            this.lastGt = lastGt;
            this.lastRt = lastRt;
            this.lastUt = lastUt;
        }

        public Record ToRecord(Func<IParticleFields, string> jsonWriter)
        {
            return new Record(this, jsonWriter);
        }
    }
}