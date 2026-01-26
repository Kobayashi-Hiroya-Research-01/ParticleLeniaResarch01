// This code was copied from https://github.com/viceroypenguin/DBSCAN
// and edited by this project.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Assets.Packages.viceroypenguin.Dbscan.Custom
{
    /// <summary>
    /// A collection of items that have been clustered by the algorithm.
    /// </summary>
    /// <typeparam name="T">The type of elements in the cluster.</typeparam>
    public class Cluster<T> : ICluster, IReadOnlyList<T> where T : IPointData
    {
        private List<T> objects;

        public ClusterID ClusterID { get; private set; }

        /// <summary>
        /// The items that have been clustered.
        /// </summary>
        public ReadOnlyCollection<T> Objects { get; private set; }

        public int Count => Objects.Count;

        public T this[int index] => Objects[index];

        public void Init(ClusterID clusterID, List<T> objects)
        {
            this.objects = objects;
            ClusterID = clusterID;
            Objects = objects.AsReadOnly();
        }

        public void RemoveAll(ReadOnlySpan<int> pointIndexs)
        {
            for (int i = objects.Count - 1; i >= 0; i--)
            {
                var point = objects[i];
                foreach (var index in pointIndexs)
                {
                    if (point.Index == index)
                    {
                        objects.RemoveAt(i);
                    }
                }
            }
        }

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
            return $"{{ID = {ClusterID}, Count = {Count}}}";
        }
    }
}