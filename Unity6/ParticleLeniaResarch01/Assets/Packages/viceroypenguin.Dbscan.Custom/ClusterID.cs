// This code was copied from https://github.com/viceroypenguin/DBSCAN
// and edited by this project.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Packages.viceroypenguin.Dbscan.Custom
{
    public class ClusterID : IEquatable<ClusterID>
    {
        public static ClusterID UnclusteredID { get; } = new ClusterID(-1);

        public ClusterID(int value)
        {
            Value = value;
        }

        public int Value { get; private set; }

        public int LifeTime { get; set; }

        public void UpdateValue(int value)
        {
            Value = value;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ClusterID);
        }

        public bool Equals(ClusterID other)
        {
            return other is not null &&
                   Value == other.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }

        public static bool operator ==(ClusterID left, ClusterID right)
        {
            return EqualityComparer<ClusterID>.Default.Equals(left, right);
        }

        public static bool operator !=(ClusterID left, ClusterID right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return Value == -1 ? "<UnclusteredID>" : Value.ToString();
        }
    }
}