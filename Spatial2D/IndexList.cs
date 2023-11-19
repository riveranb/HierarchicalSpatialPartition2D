// Reference: https://stackoverflow.com/questions/41946007/efficient-and-well-explained-implementation-of-a-quadtree-for-2d-collision-det

using System.Collections.Generic;
using UnityEngine;

namespace Spatial
{
    public struct LinkedIndex
    {
        public int index;
        public int next;
    }

    // freeable with contant-time removal
    public class IntIndexList
    {
        #region variables
        private int[] data = null;
        private int numFields = 0;
        private int num = 0;
        private int cap;
        private int freeElementHeader = -1;
        #endregion

        public int Size => num;

        public IntIndexList()
        {
            numFields = 1;
            cap = 128;
            data = new int[cap];
        }

        // Creates a new list of elements which each consist of integer fields.
        // 'start_num_fields' specifies the number of integer fields each element has.
        public IntIndexList(int start_num_fields, int capacity)
        {
            numFields = start_num_fields;
            cap = capacity;
            data = new int[cap];
        }

        // Returns the value of the specified field for the nth element.
        public int Get(int n, int field)
        {
            if (n < 0 || n >= num || field < 0 || field >= numFields)
            {
                throw new System.Exception("Invalid element - field.");
            }
            return data[n * numFields + field];
        }

        // Sets the value of the specified field for the nth element.
        public void Set(int n, int field, int val)
        {
            if (n < 0 || n >= num || field < 0 || field >= numFields)
            {
                throw new System.Exception("Invalid element - field.");
            }
            data[n * numFields + field] = val;
        }

        // Clears the list, making it empty.
        public void Clear()
        {
            num = 0;
            freeElementHeader = -1;
        }

        // Inserts an element to the back of the list and returns an index to it.
        public int PushBack()
        {
            int new_pos = (num + 1) * numFields;

            // If the list is full, we need to reallocate the buffer to make room
            // for the new element.
            if (new_pos > cap)
            {
                // Use double the size for the new capacity.
                int new_cap = new_pos * 2;

                // Allocate new array and copy former contents.
                int[] new_array = new int[new_cap];
                System.Array.Copy(data, new_array, cap);
                data = new_array;

                // Set the old capacity to the new capacity.
                cap = new_cap;
            }
            return num++;
        }

        // Removes the element at the back of the list.
        public void PopBack()
        {
            // Just decrement the list size.
            if (num <= 0)
            {
                throw new System.Exception("Pop from empty list.");
            }
            --num;
        }

        // Inserts an element to a vacant position in the list and returns an index to it.
        public int Insert()
        {
            // If there's a free index in the free list, pop that and use it.
            if (freeElementHeader != -1)
            {
                int index = freeElementHeader;
                int pos = index * numFields;

                // Set the free index to the next free index.
                freeElementHeader = data[pos];

                // Return the free index.
                return index;
            }
            // Otherwise insert to the back of the array.
            return PushBack();
        }

        // Removes the nth element in the list.
        public void Erase(int n)
        {
            // Push the element to the free list.
            int pos = n * numFields;
            data[pos] = freeElementHeader;
            freeElementHeader = n;
        }
    }

    /// Provides an linked indexed list with constant-time removals from anywhere
    /// in the list without invalidating indices. T must be trivially constructible 
    /// and destructible.
    public class IndexList<T>
    {
        public struct FreeableElement
        {
            public int next;
            public T element;
        }

        #region variables
        private FreeableElement[] data = null;
        private int freeHeader = -1;
        private int total = 0;
        #endregion

        public int Size => total;

        public IndexList()
        {
            data = new FreeableElement[128];
        }
        
        public IndexList(int capacity)
        {
            data = new FreeableElement[capacity];
        }

        // Removes the nth element from the free list.
        public void Erase(int n)
        {
            data[n].next = freeHeader;
            freeHeader = n;
        }

        // Inserts an element to the free list and returns an index to it.
        public int Insert(T input)
        {
            if (freeHeader != -1)
            {
                int index = freeHeader;
                freeHeader = data[freeHeader].next;
                data[index].element = input;
                data[index].next = -2;
                return index;
            }
            else
            {
                FreeableElement fe = new FreeableElement()
                {
                    element = input,
                    next = -2,
                };
                PushBackData(fe);
                return total - 1;
            }
        }

        // not work for linked indexed list
        public int FindIndex(T input)
        {
            for (int i = 0; i < total; ++i)
            {
                if (data[i].next != -2) // freed
                {
                    continue;
                }
                if (data[i].element.Equals(input))
                {
                    return i;
                }
            }
            return -1;
        }

        public T GetElement(int n)
        {
            return data[n].element;
        }

        public int GetLink(int n)
        {
            return data[n].next;
        }

        public void SetLink(int n, int link)
        {
            data[n].next = link;
        }

        public void Clear()
        {
            total = 0;
            freeHeader = -1;
        }

        public void DumpLog()
        {
            for (int i = 0; i < total; ++i)
            {
                Debug.Log($"{i}: next-link={data[i].next}\n{data[i].element}");
            }
        }

        private void PushBackData(FreeableElement fe)
        {
            if (total >= data.Length - 1) // already full, expand
            {
                var expandData = new FreeableElement[data.Length + data.Length];
                System.Array.Copy(data, expandData, total);
                data = expandData; // replace
            }
            // append at last
            data[total] = fe;
            total++;
        }
    }
}
