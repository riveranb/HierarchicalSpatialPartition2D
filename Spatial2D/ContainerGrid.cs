using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Spatial
{
    /// <summary>
    /// Packed grid cells with each cell owned a container of elements
    /// </summary>
    public class ContainerGrid<T> : GridBase
    {
        #region variables
        // TODO: better HashSet replacement?
        public readonly HashSet<T>[] gridCells = null;

#if UNITY_EDITOR
        private StringBuilder strbuild;
#endif
        #endregion

        /// <summary>
        /// (Rect)bounding represents whole target space, must contains all targets.
        /// </summary>
        public ContainerGrid(Rect bounding, int row, int col)
            : base(bounding, row, col, CellType.Packed)
        {
            gridCells = new HashSet<T>[row * col];
            int capacity = col;
            capacity = System.Math.Max(capacity, 8);
            for (int i = 0; i < gridCells.Length; i++)
            {
                gridCells[i] = new HashSet<T>();
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
                    strbuild.Append($"[{i}, {j}]: ");
                    var iter = gridCells[index].GetEnumerator();
                    while (iter.MoveNext())
                    {
                        strbuild.Append($" {iter.Current},");
                    }
                    
                    strbuild.AppendLine();
                    strbuild.AppendLine($"{gridCells[index].Count} elements");
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
                return gridCells[cellIdx].Count == 0;
            }
            return true;
        }

        public void AddElementWithBound(T element, Rect bound, bool ignoreInside = false)
        {
            Vector2Int rangeMin, rangeMax;
            FindContactCellsRange(bound, out rangeMin, out rangeMax);
            int rowOfCenter = -1, colOfCenter = -1;
            if (ignoreInside)
            {
                CalculateRowColumn(bound.center, out rowOfCenter, out colOfCenter);
            }

            for (int i = rangeMin.y; i <= rangeMax.y; ++i)
            {
                for (int j = rangeMin.x; j <= rangeMax.x; ++j)
                {
                    if (ignoreInside && i == rowOfCenter && j == colOfCenter)
                    {
                        continue; // this element-bound locate inside this cell originally
                    }

                    int index = i * columnSize + j;
                    gridCells[index].Add(element);
                }
            }
#if UNITY_EDITOR
            //Debug.Log($"Added into coarse-packed[{rangeMin.y}, {rangeMin.x}] ~ " +
            //    $"[{rangeMax.y}, {rangeMax.x}]");
#endif
        }
    }
}

