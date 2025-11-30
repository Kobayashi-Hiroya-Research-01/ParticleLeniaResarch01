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
    public class ClusterSet<T> : IReadOnlyDictionary<ClusterID, Cluster<T>>
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
            ClustersDict = new ReadOnlyDictionary<ClusterID, Cluster<T>>(clusters.ToDictionary(e => e.ClusterID));

            UnclusteredObjects = new Cluster<T>(ClusterID.UnclusteredID, unclusteredObjects);
        }

        public Cluster<T> this[ClusterID key] => key == ClusterID.UnclusteredID ? UnclusteredObjects : ClustersDict[key];

        /// <summary>
        /// A list of the clusters that have been identified.
        /// </summary>
        public ReadOnlyDictionary<ClusterID, Cluster<T>> ClustersDict { get; private set; }

        public void UpdateDict()
        {
            ClustersDict = new ReadOnlyDictionary<ClusterID, Cluster<T>>(clusters.ToDictionary(e => e.ClusterID));
        }

        public ReadOnlyCollection<Cluster<T>> OrderByDescendingCountClusters => orderByDescendingCountClusters;

        /// <summary>
        /// A list of the items that were not identified as being part of a cluster.
        /// </summary>
        public Cluster<T> UnclusteredObjects { get; }

        public IEnumerable<ClusterID> Keys => ClustersDict.Keys;

        public IEnumerable<Cluster<T>> Values => ClustersDict.Values;

        public int Count => ClustersDict.Count;

        public bool ContainsKey(ClusterID key)
        {
            return ClustersDict.ContainsKey(key);
        }

        public IEnumerator<KeyValuePair<ClusterID, Cluster<T>>> GetEnumerator()
        {
            return ClustersDict.GetEnumerator();
        }

        public bool TryGetValue(ClusterID key, out Cluster<T> value)
        {
            if (key == ClusterID.UnclusteredID)
            {
                value = UnclusteredObjects;
                return true;
            }
            else
            {
                return ClustersDict.TryGetValue(key, out value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)ClustersDict).GetEnumerator();
        }
    }
}