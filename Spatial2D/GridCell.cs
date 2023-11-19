// Reference: https://stackoverflow.com/questions/41946007/efficient-and-well-explained-implementation-of-a-quadtree-for-2d-collision-det

using System.Collections.Generic;
using UnityEngine;

namespace Spatial
{
    public interface IGridCell
    {
        /// header index of first element contained, -1 means no elements
        int ElementHeader { get; set; }
        void Initialize();
        void Expand(Rect rect);
    }

    public struct GridCell : IGridCell
    {
        // header index of first element contained, -1 means no elements
        public int ElementHeader { get; set; }
        public bool IsEmpty => ElementHeader == -1;

        public void Initialize()
        {
            ElementHeader = -1;
        }

        // not expandable
        public void Expand(Rect rect) { }
    }

    public struct LooseGridCell : IGridCell
    {
        #region variables
        public Rect rect;
        #endregion

        // header index of first element contained, -1 means no elements
        public int ElementHeader { get; set; }
        public bool IsEmpty => ElementHeader == -1;

        public void Initialize()
        {
            ElementHeader -= 1;
            rect.size = Vector2.zero;
        }

        public void Expand(Rect bound)
        {
            if (rect.width == 0 || rect.height == 0)
            {
                rect = bound;
            }
            else
            {
                var newMin = Vector2.Min(rect.min, bound.min);
                var newMax = Vector2.Max(rect.max, bound.max);
                rect.min = newMin;
                rect.max = newMax;
            }
        }
    }

    public struct GridRow<T, C> where C : struct, IGridCell
    {
        #region variables
        public int columnSize;
        public C[] cells;
        /// <summery>
        /// Because one element only exist in one cell, they can be indexed-linked
        /// </summery>
        public IndexList<T> elementsHolder;
        #endregion

        public void Initialize(int cols, int elementCapacity = 128)
        {
            elementsHolder = new IndexList<T>(elementCapacity);

            columnSize = cols;
            if (columnSize >= 0)
            {
                for (int i = 0; i < cells.Length; i++)
                {
                    cells[i].Initialize();
                }
            }
        }

        public void AddElementToCell(T element, int index, Rect bound)
        {
            // add element into IndexList<T>
            int elementIdx = elementsHolder.Insert(element);
            // refresh linked indexing
            elementsHolder.SetLink(elementIdx, cells[index].ElementHeader);
            cells[index].ElementHeader = elementIdx;
            cells[index].Expand(bound);
        }

        public void AddElementToCell(T element, int index)
        {
            int elementIdx = elementsHolder.Insert(element);
            elementsHolder.SetLink(elementIdx, cells[index].ElementHeader);
            cells[index].ElementHeader = elementIdx;
        }

        public void ForEachElementInCell(int cellIndex, System.Action<T> callback)
        {
            int current = cells[cellIndex].ElementHeader;
            while (current != -1)
            {
                callback.Invoke(elementsHolder.GetElement(current));
                current = elementsHolder.GetLink(current);
            }
        }
    }
}
