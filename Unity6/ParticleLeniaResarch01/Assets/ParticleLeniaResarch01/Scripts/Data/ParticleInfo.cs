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
        private IParticleFields _fields;
        private double[] _position;
        public double lastUt;
        public double lastGt;
        public double lastRt;

        public ParticleInfo(IParticleFields fields, double[] position, double lastGt, double lastRt, double lastUt)
        {
            this._fields = fields;
            this._position = position;
            this.lastGt = lastGt;
            this.lastRt = lastRt;
            this.lastUt = lastUt;
        }

        public IParticleFields fields => _fields;
        public Span<double> position => _position;
        public double LastE => lastRt - lastGt;

        public void CopyTo(ParticleInfo other)
        {
            other._fields = fields;
            if (other._position == null || other._position.Length != _position.Length)
            {
                other._position = _position.ToArray();
            }
            else
            {
                _position.AsSpan().CopyTo(other._position);
            }
            other.lastUt = lastUt;
            other.lastGt = lastGt;
            other.lastRt = lastRt;
        }

        public override string ToString()
        {
            var pos = string.Join(", ", _position.Select(Format));
            return $"{{ pos=[{pos}], ut={Format(lastUt)}, rt={Format(lastRt)}, gt={Format(lastGt)} }}";
        }

        private static string Format(double value)
        {
            return $"{value,8:F5}";
        }
    }
}