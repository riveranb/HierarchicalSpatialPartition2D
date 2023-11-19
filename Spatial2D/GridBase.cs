using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spatial
{
    public abstract class GridBase
    {
        #region variables
        public readonly CellType celltype;
        public int rowSize;
        public int columnSize;
        public Rect boundingRect; // target spatial total bound
        public float rowSpacing;
        public float columnSpacing;
        #endregion

        public GridBase(Rect bounding, int row, int col, CellType type)
        {
            celltype = CellType.Packed;
            boundingRect = bounding;
            rowSize = row;
            columnSize = col;
            rowSpacing = bounding.height / row;
            columnSpacing = bounding.width / col;
        }

        public abstract bool IsCellEmpty(int r, int c);

        public void CalculateRowColumn(Vector2 pos, out int row, out int col)
        {
            Vector2 offset = pos - boundingRect.position;
            col = Mathf.FloorToInt(offset.x / columnSpacing);
            row = Mathf.FloorToInt(offset.y / rowSpacing);
        }

        public Vector2 CalculateCellCenter(int r, int c)
        {
            return boundingRect.min + new Vector2((c + 0.5f) * columnSpacing, (r + 0.5f) * rowSpacing);
        }

        public virtual int GetCellArrayIndex(int r, int c)
        {
            return r * columnSize + c;
        }

        public virtual bool IsOutsideCell(int r, int c)
        {
            return r < 0 || c < 0 || r >= rowSize || c >= columnSize;
        }

        public virtual bool TryGetBound(int r, int c, out Rect bb)
        {
            bb = boundingRect;
            if (IsOutsideCell(r, c))
            {
                return false;
            }
            bb.min = boundingRect.min + new Vector2(c * columnSpacing, r * rowSpacing);
            bb.width = columnSpacing;
            bb.height = rowSpacing;
            return true;
        }

        public virtual void FindContactCellsRange(Rect bb, out Vector2Int cellmin, 
            out Vector2Int cellmax)
        {
            int row, col;
            CalculateRowColumn(bb.min, out row, out col);
            cellmin = Vector2Int.zero;
            cellmin.x = System.Math.Max(col, 0);
            cellmin.y = System.Math.Max(row, 0);
            CalculateRowColumn(bb.max, out row, out col);
            cellmax = Vector2Int.zero;
            cellmax.x = System.Math.Min(col, columnSize - 1);
            cellmax.y = System.Math.Min(row, rowSize - 1);
        }
    }
}

