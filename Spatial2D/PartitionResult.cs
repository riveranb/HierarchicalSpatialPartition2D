using System.Collections.Generic;
using UnityEngine;

namespace Spatial
{
    [System.Serializable]
    public class GridPartitionResult
    {
        #region variables
        public Rect worldRect;
        public PartitionGridParams gridParams;
        public SpatialUnit2D[] sources = null;
        public Rect[] detailCellsRect = null;
        public Vector2Int[] coarseCellElements = null;
        public int[] coarseCellElementHeaders = null;
        #endregion
    }
}
