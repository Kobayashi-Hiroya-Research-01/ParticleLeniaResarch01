// This code was copied from https://github.com/viceroypenguin/DBSCAN
// and edited by this project.

using Assets.ParticleLenia.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Packages.viceroypenguin.Dbscan.Custom
{
    /// <summary>
    /// An implementation of the <see cref="ISpatialIndex{T}"/> using a simple <see cref="List{T}"/>
    /// to hold the elements and a linear search of all items to <see cref="Search(in IPointData, double)"/>
    /// for nearby items.
    /// </summary>
    /// <typeparam name="T">The type of items in the index.</typeparam>
    public class ListSpatialIndex<T> where T : IPointData
    {
        public List<PointInfo<T>> List { get; }

        /// <summary>
        /// Initializes a <see cref="ListSpatialIndex{T}"/> with a collection of data, using the
        /// specified distanct function to search for points in a given neighborhood.
        /// </summary>
        /// <param name="data">The collection of data to put into the index</param>
        /// <param name="distanceFunction">The function used to determine if a point is within a specified distance of a given point.</param>
        public ListSpatialIndex()
        {
            List = new List<PointInfo<T>>();
        }

        /// <summary>
        /// Get all of the elements from this <see cref="ListSpatialIndex{T}"/>
        /// within a circle centered at the point <see cref="IPointData.Point"/>
        /// with a radius of <paramref name="epsilon"/>.
        /// </summary>
        /// <param name="p">The center of the search circle.</param>
        /// <param name="epsilon">The radius of the search circle.</param>
        /// <returns>
        /// A list of the points that are within the search area.
        /// </returns>
        public void Search(in T p, double epsilon, Action<PointInfo<T>> onFind)
        {
            foreach (var q in List)
            {
                if (Distance(p, q.Item) < epsilon)
                {
                    onFind(q);
                }
            }
        }

        private double Distance(in T a, in T b)
        {
            var ai = a.Point;
            var bi = b.Point;

            var sqDistance = 0.0;

            for (var i = 0; i < ai.Length; i++)
            {
                var dx = ai[i] - bi[i];
                sqDistance += dx * dx;
            }

            //return pos;
            return Math.Sqrt(sqDistance);
        }
    }
}