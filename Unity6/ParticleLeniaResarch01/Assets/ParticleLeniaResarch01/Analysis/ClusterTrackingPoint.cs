using Assets.Packages.viceroypenguin.Dbscan.Custom;
using Assets.ParticleLeniaResarch01.Scripts.Data;
using System;
using System.Linq;

namespace Assets.ParticleLeniaResarch01.Analysis
{
    public class ClusterTrackingPoint : IClusterTrackingPointData
    {
        public delegate Span<double> GetPoint(ParticleInfo particleInfo, Span<double> point);

        private readonly GetPoint getPoint;
        private readonly double[] point;

        public ClusterTrackingPoint(ParticleInfo particleInfo, int index, GetPoint getPoint)
        {
            this.getPoint = getPoint;
            point = new double[particleInfo.position.Length + 2];
            ParticleInfo = particleInfo;
            Index = index;
        }

        public ClusterID ClusterID { get; set; } = ClusterID.UnclusteredID;

        public int Index { get; private set; }

        public ParticleInfo ParticleInfo { get; private set; }

        ReadOnlySpan<double> IPointData.Point
        {
            get
            {
                getPoint(ParticleInfo, point);
                return point;
            }
        }

        public void IndexReset(int newIndex)
        {
            Index = newIndex;
        }

        public override string ToString()
        {
            getPoint(ParticleInfo, point);
            return $"[{Index}:{string.Join(", ", point)}]";
        }
    }
}