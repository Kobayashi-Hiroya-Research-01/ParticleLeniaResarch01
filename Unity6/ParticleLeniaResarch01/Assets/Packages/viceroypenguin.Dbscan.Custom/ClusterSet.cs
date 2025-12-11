// This code was copied from https://github.com/viceroypenguin/DBSCAN
// and edited by this project.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.VisualScripting;

namespace Assets.Packages.viceroypenguin.Dbscan.Custom
{
    /// <summary>
    /// The result of running the DBSCAN algorithm on a set of data.
    /// </summary>
    /// <typeparam name="TPoint">The type of elements in the cluster.</typeparam>
    public class ClusterSet<TPoint, TCluster>
        where TPoint : IPointData
        where TCluster : Cluster<TPoint>, new()
    {
        private Dictionary<int, TCluster> clusters;

        private int[] keys;
        private TCluster[] values;

        public ClusterSet(Dictionary<int, TCluster> clusters, TCluster unclusteredObjectsCluster)

        {
            this.clusters = clusters;
            UnclusteredObjects = unclusteredObjectsCluster;
        }

        /// <summary>
        /// A list of the items that were not identified as being part of a cluster.
        /// </summary>
        public TCluster UnclusteredObjects { get; }

        public Span<int> Keys => keys ??= clusters.Keys.ToArray();

        public Span<TCluster> Values => values ??= clusters.Values.ToArray();

        public int Count => clusters.Count;

        public TCluster this[int key] => clusters[key];

        public void UpdateIDs()
        {
            clusters = clusters.Values.ToDictionary(e => e.ClusterID.Value);
            keys = null;
            values = null;
        }

        public double SilhouetteCoefficient()
        {
            // ユークリッド距離
            static double Dist(TPoint at, TPoint bt)
            {
                var a = at.Point;
                var b = bt.Point;

                double sum = 0;
                for (int i = 0; i < a.Length; i++)
                    sum += (a[i] - b[i]) * (a[i] - b[i]);
                return Math.Sqrt(sum);
            }

            // 全クラスタのポイント群を取り出し
            var clusters = this.clusters.Values.ToArray().AsSpan();

            double total = 0;
            int count = 0;

            // 各クラスタごと
            for (int c = 0; c < clusters.Length; c++)
            {
                var own = clusters[c];

                // 自クラスタの各点に対して
                for (int i = 0; i < own.Count; i++)
                {
                    var p = own[i];

                    // a(i): 自クラスタ内平均距離
                    double a;
                    if (own.Count <= 1)
                    {
                        a = 0; // クラスタサイズ1の場合
                    }
                    else
                    {
                        double sum = 0;
                        for (int j = 0; j < own.Count; j++)
                        {
                            if (i == j) continue;
                            sum += Dist(p, own[j]);
                        }
                        a = sum / (own.Count - 1);
                    }

                    // b(i): 他クラスタの最小平均距離
                    double b = double.MaxValue;

                    for (int other = 0; other < clusters.Length; other++)
                    {
                        if (other == c) continue;

                        var oc = clusters[other];
                        double sum = 0;
                        for (int j = 0; j < oc.Count; j++)
                            sum += Dist(p, oc[j]);

                        double avg = sum / oc.Count;
                        if (avg < b)
                            b = avg;
                    }

                    // s(i) = (b - a) / max(a, b)
                    double s = (b - a) / Math.Max(a, b);
                    total += s;
                    count++;
                }
            }

            return count > 0 ? total / count : 0;
        }

        public bool TryGetValue(int key, out TCluster value)
        {
            return clusters.TryGetValue(key, out value);
        }
    }
}