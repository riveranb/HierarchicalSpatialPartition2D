// Reference: https://stackoverflow.com/questions/41946007/efficient-and-well-explained-implementation-of-a-quadtree-for-2d-collision-det

using System.Text;
using UnityEngine;

namespace Spatial
{
    public enum CellType
    {
        Packed = 0,
        Loose,
    }

    public interface ISpatial2D
    {
        public int Priority { get; }
        public Rect RectBound { get; set; }

        public void CalculateBounds();
    }

    public class LooseGrid<T> : GridBase where T : ISpatial2D
    {
        #region variables
        public LooseGridCell[] gridCells = null;
        /// <summery>
        /// Because one element only exist in one cell, they can be indexed-linked
        /// </summery>
        public IndexList<T> elementsHolder;

#if UNITY_EDITOR
        private StringBuilder strbuild = null;
#endif
        #endregion

        /// <summary>
        /// (Rect)bounding represents whole target space, must contains all targets.
        /// </summary>
        public LooseGrid(Rect bounding, int row, int col) : 
            base(bounding, row, col, CellType.Loose)
        {
            // base() not executed yet
            gridCells = new LooseGridCell[row * col];
            elementsHolder = new IndexList<T>(512);
            int index = 0;
            for (int r = 0; r < row; ++r)
            {
                for (int c = 0; c < col; ++c)
                {
                    gridCells[index].Initialize();
                    gridCells[index].rect.center = CalculateCellCenter(r, c);
                    index++;
                }
            }
        }

#if UNITY_EDITOR
        public override string ToString()
        {
            if (strbuild == null)
            {
                strbuild = new StringBuilder();
            }
            else
            {
                strbuild.Clear();
            }

            for (int i = 0; i < rowSize; ++i)
            {
                for (int j = 0; j < columnSize; ++j)
                {
                    int index = GetCellArrayIndex(i, j);
                    strbuild.Append($"[{i}, {j}]: rect = {gridCells[index].rect}");
                    int count = 0;
                    int current = gridCells[index].ElementHeader;
                    while (current != -1)
                    {
                        strbuild.Append($" {elementsHolder.GetElement(current)},");
                        current = elementsHolder.GetLink(current);
                        count++;
                    }
                    strbuild.AppendLine();
                    strbuild.AppendLine($"{count} elements");
                }
            }

            return strbuild.ToString();
        }
#endif

        public override bool IsCellEmpty(int r, int c)
        {
            int cellIdx = GetCellArrayIndex(r, c);
            if (cellIdx >= 0 && cellIdx < gridCells.Length)
            {
                return gridCells[cellIdx].IsEmpty;
            }
            return true;
        }

        public bool AddElement(T element, Rect bound, out int row, out int column)
        {
            row = column = -1;
            CalculateRowColumn(bound.center, out row, out column);
            // add element to grid-cell according to bound.center
            return AddElementToCell(element, row, column, bound);
        }

        public override bool TryGetBound(int r, int c, out Rect bb)
        {
            if (IsOutsideCell(r, c))
            {
                bb = boundingRect;
                return false;
            }

            int index = GetCellArrayIndex(r, c);
            bb = gridCells[index].rect;
            return true;
        }

        public void ForEachElementInCell(int cellIndex, System.Action<T> callback)
        {
            int current = gridCells[cellIndex].ElementHeader;
            while (current != -1)
            {
                callback.Invoke(elementsHolder.GetElement(current));
                current = elementsHolder.GetLink(current);
            }
        }

        public bool AddElementToCell(T element, int row, int col)
        {
            if (IsOutsideCell(row, col))
            {
                return false;
            }
            int elementIdx = elementsHolder.Insert(element);
            int index = GetCellArrayIndex(row, col);
            elementsHolder.SetLink(elementIdx, gridCells[index].ElementHeader);
            gridCells[index].ElementHeader = elementIdx;
            return true;
        }

        public bool AddElementToCell(T element, int row, int col, Rect bound)
        {
            if (IsOutsideCell(row, col))
            {
                return false;
            }
            // add element into IndexList<T>
            int elementIdx = elementsHolder.Insert(element);
            int index = GetCellArrayIndex(row, col);
            // refresh linked indexing
            elementsHolder.SetLink(elementIdx, gridCells[index].ElementHeader);
            gridCells[index].ElementHeader = elementIdx;
            gridCells[index].Expand(bound);
            return true;
        }

        public bool RemoveElementFromCell(T element, int row, int col)
        {
            if (IsOutsideCell(row, col))
            {
                return false;
            }
            int index = GetCellArrayIndex(row, col);
            int current = gridCells[index].ElementHeader;
            int previous = -1;
            while (current != -1)
            {
                var iter = elementsHolder.GetElement(current);
                if (element.Equals(iter))
                {
                    if (previous != -1)
                    {
                        // set previous's next  = current's next
                        elementsHolder.SetLink(previous, elementsHolder.GetLink(current));
                    }
                    else // to erase header, move it to next
                    {
                        gridCells[index].ElementHeader = elementsHolder.GetLink(current);
                    }
                    elementsHolder.Erase(current); // then erase it
                    return true;
                }
                previous = current;
                current = elementsHolder.GetLink(current);
            }
            return false;
        }
    }
}

