namespace Assets.ParticleLeniaResarch01.Analyzers
{
    using PointMode = ClusterTrackingDbscanAnalyzerPointMode;

    public class ClusterTrackingDbscanAnalyzerPointSettingData : ClusterTrackingDbscanAnalyzerPointSettingData.IOption
    {
        public interface IOption
        {
            PointMode PointMode { get; set; }
            double EWeight { get; set; }
            double UtWeight { get; set; }
            double RtWeight { get; set; }
            bool NormalizeStatusByPositionRange { get; set; }
            bool NormalizeStatusByDimension { get; set; }

            void CopyTo(IOption other)
            {
                other.PointMode = PointMode;
                other.EWeight = EWeight;
                other.UtWeight = UtWeight;
                other.RtWeight = RtWeight;
                other.NormalizeStatusByPositionRange = NormalizeStatusByPositionRange;
                other.NormalizeStatusByDimension = NormalizeStatusByDimension;
            }
        }

        public PointMode PointMode { get; set; } = PointMode.UtRt;
        public double EWeight { get; set; } = 1.0;
        public double UtWeight { get; set; } = 1.0;
        public double RtWeight { get; set; } = 1.0;
        public bool NormalizeStatusByPositionRange { get; set; } = true;
        public bool NormalizeStatusByDimension { get; set; } = true;
    }
}