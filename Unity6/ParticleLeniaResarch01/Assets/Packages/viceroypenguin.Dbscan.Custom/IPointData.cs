// This code was copied from https://github.com/viceroypenguin/DBSCAN
// and edited by this project.

using System;

namespace Assets.Packages.viceroypenguin.Dbscan.Custom
{
    /// <summary>
    /// Exposes a <see cref="Point"/> that identifies where an object is.
    /// </summary>
    public interface IPointData
    {
        /// <summary>
        /// The location of the current object.
        /// </summary>
        ReadOnlySpan<double> Point { get; }

        int Index { get; }

        public void IndexReset(int newIndex);
    }
}