using System;
using UnityEngine;

namespace Spatial
{
    [Serializable]
    public struct SpatialUnit2D : ISpatial2D, IEquatable<SpatialUnit2D>
    {
        #region variables
        public string namekey;
        public Transform parent;
        public Vector2 worldPos;
        public Vector2Int cell;
        public sbyte priority;
        public Rect bbrect;
        #endregion

        #region ISpatial2D
        public int Priority => priority;
        public Rect RectBound
        {
            get => bbrect;
            set => bbrect = value;
        }

        public void CalculateBounds()
        {
        }
        #endregion

        public override int GetHashCode()
        {
            if (namekey != null)
            {
                return namekey.GetHashCode();
            }
            return 0;
        }

        public override bool Equals(object obj)
        {
            if (obj is SpatialUnit2D)
            {
                return Equals((SpatialUnit2D)obj);
            }
            return false;
        }

        public bool Equals(SpatialUnit2D other)
        {
            return string.Equals(namekey, other.namekey);
        }

        public void Invalidate()
        {
            namekey = string.Empty;
            parent = null;
            cell.x = cell.y = -1;
            priority = 0;
            bbrect = new Rect();
        }
    }
}


