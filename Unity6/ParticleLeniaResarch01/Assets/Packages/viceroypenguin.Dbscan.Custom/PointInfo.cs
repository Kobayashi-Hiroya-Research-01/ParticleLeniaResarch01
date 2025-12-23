// This code was copied from https://github.com/viceroypenguin/DBSCAN
// and edited by this project.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Packages.viceroypenguin.Dbscan.Custom
{
    /// <summary>
    /// A holding class for algorithm related information about a point.
    /// </summary>
    /// <typeparam name="T">The type of the element this object is holding.</typeparam>
    public class PointInfo<T> where T : IPointData
    {
        /// <summary>
        /// Initializes a new <see cref="PointInfo{T}"/> with the object it is holding.
        /// </summary>
        /// <param name="item"></param>
        public PointInfo(T item)
        {
            Item = item;
        }

        /// <summary>
        /// The object being held by this holder
        /// </summary>
        public T Item { get; }

        /// <summary>
        /// Whether or not this point has been clustered
        /// </summary>
        public bool Clustered { get; set; }

        /// <summary>
        /// Whether or not this point has been visited
        /// </summary>
        public bool Visited { get; set; }

        private readonly List<PointInfo<T>> near = new();

        public void AddNear(PointInfo<T> item)
        {
            near.Add(item);
        }

        public void MakeNearArray()
        {
            nearArray = near.ToArray();
            near.Clear();
        }

        private PointInfo<T>[] nearArray;

        public ReadOnlySpan<PointInfo<T>> NearArray => nearArray;

        public override string ToString()
        {
            return $"[{Item.Index}, ({string.Join(", ", Item.Point.ToArray().Select(e => $"{e,8:F5}"))}), {nearArray.Length,4}]";
        }
    }
}