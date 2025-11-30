// This code was copied from https://github.com/viceroypenguin/DBSCAN
// and edited by this project.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Assets.Packages.viceroypenguin.Dbscan.Custom
{
    /// <summary>
    /// A collection of items that have been clustered by the algorithm.
    /// </summary>
    /// <typeparam name="T">The type of elements in the cluster.</typeparam>
    public class Cluster<T> : IReadOnlyList<T>
    {
        public ClusterID ClusterID { get; }

        public Cluster(ClusterID clusterID, List<T> objects)
        {
            ClusterID = clusterID;
            Objects = objects.AsReadOnly();
        }

        /// <summary>
        /// The items that have been clustered.
        /// </summary>
        public ReadOnlyCollection<T> Objects { get; }

        public int Count => Objects.Count;

        public T this[int index] => Objects[index];

        public IEnumerator<T> GetEnumerator()
        {
            return Objects.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Objects.GetEnumerator();
        }

        public override string ToString()
        {
            return $"Cluster {{ID = {ClusterID}, Count = {Count}}}";
        }
    }
}