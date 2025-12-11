// This code was copied from https://github.com/viceroypenguin/DBSCAN
// and edited by this project.

using System;

namespace Assets.Packages.viceroypenguin.Dbscan.Custom
{
    public interface IDbscan<TPoint, TCluster>
        where TPoint : IPointData
        where TCluster : Cluster<TPoint>, new()
    {
        double Epsilon { get; set; }
        int MinimumPointsPerCluster { get; set; }

        void AddPoint(TPoint data);

        ClusterSet<TPoint, TCluster> CalculateClusters();

        ClusterSet<TPoint, TCluster> CalculateClusters(Action<TPoint, TCluster> onClustered);
    }
}