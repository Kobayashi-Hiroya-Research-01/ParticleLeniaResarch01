// This code was copied from https://github.com/viceroypenguin/DBSCAN
// and edited by this project.

using Assets.Packages.viceroypenguin.Dbscan.Custom;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Assets.Packages.viceroypenguin.Dbscan.Custom
{
    /// <summary>
    /// The result of running the DBSCAN algorithm on a set of data.
    /// </summary>
    /// <typeparam name="T">The type of elements in the cluster.</typeparam>
    public class ClusterSet<T> : IReadOnlyDictionary<int, Cluster<T>> where T : IPointData
    {
        private readonly ReadOnlyCollection<Cluster<T>> clusters;
        private readonly ReadOnlyCollection<Cluster<T>> orderByDescendingCountClusters;

        public ClusterSet(List<Cluster<T>> clusters, List<T> unclusteredObjects)
        {
            this.clusters = clusters.AsReadOnly();
            orderByDescendingCountClusters = new(
                clusters
                    .OrderByDescending(e => e.Count)
                    .ToArray()
            );
            ClustersDict = new ReadOnlyDictionary<int, Cluster<T>>(clusters.ToDictionary(e => e.ClusterID.Value));

            UnclusteredObjects = new Cluster<T>(ClusterID.UnclusteredID, unclusteredObjects);
        }

        /// <summary>
        /// A list of the clusters that have been identified.
        /// </summary>
        public ReadOnlyDictionary<int, Cluster<T>> ClustersDict { get; private set; }

        /// <summary>
        /// A list of the items that were not identified as being part of a cluster.
        /// </summary>
        public Cluster<T> UnclusteredObjects { get; }

        public ReadOnlyCollection<Cluster<T>> OrderByDescendingCountClusters => orderByDescendingCountClusters;

        public void UpdateDict()
        {
            ClustersDict = new ReadOnlyDictionary<int, Cluster<T>>(clusters.ToDictionary(e => e.ClusterID.Value));
        }

        public bool ContainsKey(int key)
        {
            return ((IReadOnlyDictionary<int, Cluster<T>>)ClustersDict).ContainsKey(key);
        }

        public bool TryGetValue(int key, out Cluster<T> value)
        {
            return ((IReadOnlyDictionary<int, Cluster<T>>)ClustersDict).TryGetValue(key, out value);
        }

        public IEnumerator<KeyValuePair<int, Cluster<T>>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<int, Cluster<T>>>)ClustersDict).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)ClustersDict).GetEnumerator();
        }

        public int Count => clusters.Count;

        public IEnumerable<int> Keys => ((IReadOnlyDictionary<int, Cluster<T>>)ClustersDict).Keys;

        public IEnumerable<Cluster<T>> Values => ((IReadOnlyDictionary<int, Cluster<T>>)ClustersDict).Values;

        public Cluster<T> this[int key] => ((IReadOnlyDictionary<int, Cluster<T>>)ClustersDict)[key];

        public double SilhouetteCoefficient()
        {
            // 全クラスタのポイント群を取り出し
            var clusters = OrderByDescendingCountClusters
                .Select(c => c.Objects)
                .ToList();

            double total = 0;
            int count = 0;

            // 各クラスタごと
            for (int c = 0; c < clusters.Count; c++)
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

                    for (int other = 0; other < clusters.Count; other++)
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

        // ユークリッド距離
        private static double Dist(T at, T bt)
        {
            var a = at.Point;
            var b = bt.Point;

            double sum = 0;
            for (int i = 0; i < a.Length; i++)
                sum += (a[i] - b[i]) * (a[i] - b[i]);
            return Math.Sqrt(sum);
        }
    }
}