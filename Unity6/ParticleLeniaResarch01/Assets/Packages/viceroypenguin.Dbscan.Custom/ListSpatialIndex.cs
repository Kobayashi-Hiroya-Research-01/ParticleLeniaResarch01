// This code was copied from https://github.com/viceroypenguin/DBSCAN
// and edited by this project.

using Assets.ParticleLeniaResarch01.Analysis;
using Assets.ParticleLeniaResarch01.Scripts.MathObjects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        private readonly List<PointInfo<T>> pointInfos;
        private PointInfo<T>[] array;
        private double epsilon;
        private double epsilon2;

        public Span<PointInfo<T>> Items => array;

        /// <summary>
        /// Initializes a <see cref="ListSpatialIndex{T}"/> with a collection of data, using the
        /// specified distanct function to search for points in a given neighborhood.
        /// </summary>
        /// <param name="data">The collection of data to put into the index</param>
        /// <param name="distanceFunction">The function used to determine if a point is within a specified distance of a given point.</param>
        public ListSpatialIndex()
        {
            pointInfos = new();
        }

        public PointInfo<T> this[int index] => array[index];

        public void Add(T item)
        {
            pointInfos.Add(new PointInfo<T>(item));
        }

        public void AddRange(List<T> points)
        {
            foreach (var point in points)
            {
                Add(point);
            }
        }

        public void Clear()
        {
            pointInfos.Clear();
            array = Array.Empty<PointInfo<T>>();
        }

        private const int _D_Start = 1;
        private const int _D_End = 20 * 60 * 5;
        private readonly DebugTextWriter _D_PointInfos = new("PointInfos", killed: true);

        public void Reset(double epsilon)
        {
            this.epsilon = epsilon;
            epsilon2 = epsilon * epsilon;
            array = pointInfos.ToArray();

            Parallel.ForEach(array, SearchNear);

            _D_PointInfos.AppendWithLog(() => string.Join(", ", pointInfos), _D_Start, _D_End);
        }

        private void SearchNear(PointInfo<T> point_i)
        {
            foreach (var point_j in array)
            {
                if (point_i != point_j && DistanceSquares(point_i.Item, point_j.Item) < epsilon2)
                {
                    point_i.AddNear(point_j);
                }
            }

            point_i.MakeNearArray();
        }

        private double DistanceSquares(in T a, in T b)
        {
            return DistanceSquareCache.DistanceSquare(a.Point, b.Point);
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
        public ReadOnlySpan<PointInfo<T>> Search(in T p)
        {
            return array[p.Index].NearArray;
        }
    }
}