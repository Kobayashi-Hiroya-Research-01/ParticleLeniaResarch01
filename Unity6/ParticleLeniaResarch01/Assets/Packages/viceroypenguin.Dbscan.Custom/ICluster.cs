// This code was copied from https://github.com/viceroypenguin/DBSCAN
// and edited by this project.

namespace Assets.Packages.viceroypenguin.Dbscan.Custom
{
    public interface ICluster
    {
        ClusterID ClusterID { get; }
        public int Count { get; }
    }
}