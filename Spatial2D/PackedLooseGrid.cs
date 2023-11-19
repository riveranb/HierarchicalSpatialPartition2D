// Reference: https://stackoverflow.com/questions/41946007/efficient-and-well-explained-implementation-of-a-quadtree-for-2d-collision-det

using System.Collections.Generic;
using UnityEngine;

namespace Spatial
{
    /// <summary>
    /// Hierarchical double grids data structure, from coarse to fine/detailed grid. Coarse grid is a 
    /// container-based grid to store contained detailed loose grid cells. Detailed grid is a loose 
    /// grid for each cell storing real target-elements with their center position inside and calculates 
    /// real 2D bounds of all contained elements.
    /// </summary>
    public class PackedLooseGrid<T> where T : ISpatial2D
    {
        #region variables
        public ContainerGrid<Vector2Int> packedCoarseGrid = null;
        public LooseGrid<T> looseDetailGrid = null;
        private int detailRowMultiplier = 1;
        private int detailColMultiplier = 1;
        private byte[] looseGridFlags = null;
        private List<Vector2Int> queryLooseResults = new List<Vector2Int>(64);
        #endregion

        public PackedLooseGrid(Rect bounding, int containRow, int containCol, 
            int detailRowMultiply = 4, int detailColMultiply = 4)
        {
            packedCoarseGrid = new ContainerGrid<Vector2Int>(bounding, containRow, containCol);
            detailRowMultiplier = detailRowMultiply;
            detailColMultiplier = detailColMultiply;
            int detailRow = containRow * detailRowMultiplier;
            int detailCol = containCol * detailColMultiplier;
            looseDetailGrid = new LooseGrid<T>(bounding, detailRow, detailCol);
            int byteLen = (int)System.Math.Ceiling((detailRow * detailCol) / 8.0f);
            looseGridFlags = new byte[byteLen];
        }

        public bool AddElement(T element, Rect bound, out int r, out int c)
        {
            r = -1;
            c = -1;
            if (looseDetailGrid.AddElement(element, bound, out r, out c))
            {
                looseDetailGrid.TryGetBound(r, c, out Rect cellbb);
                packedCoarseGrid.AddElementWithBound(new Vector2Int(c, r), cellbb, false);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Remove an element only, not refresh all coarse grid-cells
        /// </summary>
        public bool RemoveAnElement(T element, int r, int c)
        {
            return looseDetailGrid.RemoveElementFromCell(element, r, c);
        }

        public void QueryBounds(Bounds bound, List<T> outResults)
        {
            SpatialHelper.Bounds2DToRect(bound, out Rect rt);
            QueryRectangle(rt, outResults);
        }

        public void FetchFromCoarseCell(int r, int c, ICollection<T> outResults, bool loose = true)
        {
            if (r < 0 || c < 0 || r >= packedCoarseGrid.rowSize || c >= packedCoarseGrid.columnSize)
            {
                return;
            }

            if (loose)
            {
                packedCoarseGrid.TryGetBound(r, c, out Rect containerRect);
                // iterate all contained elements (loose-cells)
                var iter = packedCoarseGrid.gridCells[packedCoarseGrid.GetCellArrayIndex(r, c)]
                    .GetEnumerator();
                while (iter.MoveNext())
                {
                    var cell = iter.Current;
                    if (looseDetailGrid.IsCellEmpty(cell.y, cell.x))
                    {
                        continue;
                    }
                    int index = looseDetailGrid.GetCellArrayIndex(cell.y, cell.x);
                    looseDetailGrid.TryGetBound(cell.y, cell.x, out Rect cellbb);
                    bool fullContained = containerRect == cellbb ||
                        (containerRect.Contains(cellbb.min) && containerRect.Contains(cellbb.max));
                    looseDetailGrid.ForEachElementInCell(index, element =>
                    {
                        if (fullContained || element.RectBound.Overlaps(containerRect))
                        {
                            outResults.Add(element);
                        }
                    });
                }
            }
            else
            {
                int beginRow = r * detailRowMultiplier;
                int beginCol = c * detailColMultiplier;
                for (int i = beginRow; i < beginRow + detailRowMultiplier; ++i)
                {
                    for (int j = beginCol; j < beginCol + detailColMultiplier; ++j)
                    {
                        int index = looseDetailGrid.GetCellArrayIndex(i, j);
                        looseDetailGrid.ForEachElementInCell(index, element => outResults.Add(element));
                    }
                }
            }
        }

        public void QueryRectangle(Rect inputRect, ICollection<T> outResults)
        {
            // TODO: parallel optimization (w/ job system)
            var allLooseCells = looseDetailGrid.gridCells;
            queryLooseResults.Clear();
            packedCoarseGrid.FindContactCellsRange(inputRect, out Vector2Int cellmin, 
                out Vector2Int cellmax);
            for (int i = cellmin.y; i <= cellmax.y; i++)
            {
                for (int j = cellmin.x; j <= cellmax.x; j++)
                {
                    if (packedCoarseGrid.IsCellEmpty(i, j))
                    {
                        continue;
                    }
                    packedCoarseGrid.TryGetBound(i, j, out Rect cellbb);
                    bool fullContained = inputRect == cellbb || 
                        (inputRect.Contains(cellbb.min) && inputRect.Contains(cellbb.max));
                    // iterate all contained elements (loose-cells)
                    var iter = packedCoarseGrid.gridCells[packedCoarseGrid.GetCellArrayIndex(i, j)]
                        .GetEnumerator();
                    while (iter.MoveNext())
                    {
                        var looseCell = iter.Current;
                        if (fullContained ||
                            // intersected
                            allLooseCells[looseDetailGrid.GetCellArrayIndex(looseCell.y, looseCell.x)]
                                .rect.Overlaps(inputRect))
                        {
                            queryLooseResults.Add(looseCell);
                        }
                    }
                }
            }

            System.Array.Clear(looseGridFlags, 0, looseGridFlags.Length);
            int count = queryLooseResults.Count;
            for (int i = 0; i < count; i++)
            {
                var cell = queryLooseResults[i];
                int index = looseDetailGrid.GetCellArrayIndex(cell.y, cell.x);
                if (ValidateByteFlagsOnce(looseGridFlags, index)) // processed
                {
                    continue;
                }

                looseDetailGrid.TryGetBound(cell.y, cell.x, out Rect cellbb);
                bool fullContained = inputRect == cellbb ||
                    (inputRect.Contains(cellbb.min) && inputRect.Contains(cellbb.max));
                looseDetailGrid.ForEachElementInCell(index, element =>
                {
                    if (fullContained || element.RectBound.Overlaps(inputRect))
                    {
                        outResults.Add(element);
                    }
                });
            }
        }

        private bool ValidateByteFlagsOnce(byte[] flags, int target)
        {
            int byteIdx = (target + 1) / 8;
            int bitIdx = target % 8;
            if (((flags[byteIdx] >> bitIdx) & 0x1) == 1) // already set flag
            {
                return true;
            }
            flags[byteIdx] |= (byte)(0x1 << bitIdx); // set flag
            return false;
        }
    }
}

