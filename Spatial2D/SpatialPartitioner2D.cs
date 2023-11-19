// Reference: https://stackoverflow.com/questions/41946007/efficient-and-well-explained-implementation-of-a-quadtree-for-2d-collision-det

using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Spatial
{
    [DisallowMultipleComponent]
    public class SpatialPartitioner2D : MonoBehaviour
    {
        #region variables
        private const float _UnitBigSize = 3;

        public int coarseRow = 4;
        public int coarseColumn = 4;
        public int detailRowMultiplier = 4;
        public int detailColumnMultiplier = 4;
        public GridPartitionResult partitionData = null;
        public System.Action<PackedLooseGrid<ISpatial2D>> overrideGridReconstruct = null;

#if UNITY_EDITOR
        private List<ISpatial2D> queryResults = new List<ISpatial2D>();
        private ISpatial2D[] sourcesInBuild = null;
#endif

        private PackedLooseGrid<ISpatial2D> grid = null;
        private Rect boundingRect;
        private Camera mainCam = null;
        #endregion

        private void Start()
        {
            if (mainCam == null)
            {
                mainCam = Camera.main;
            }

#if UNITY_EDITOR
            if (sourcesInBuild == null)
            {
                sourcesInBuild = GetComponentsInChildren<ISpatial2D>();
            }
            if (grid == null && partitionData != null && sourcesInBuild.Length > 0)
            {
                BuildPartitionInEditor();
            }
#endif
        }

#if UNITY_EDITOR
        protected virtual void OnDrawGizmosSelected()
        {
            if (grid == null)
            {
                return;
            }

            for (int r = 0; r < grid.packedCoarseGrid.rowSize; r++)
            {
                for (int c = 0; c < grid.packedCoarseGrid.columnSize; c++)
                {
                    grid.packedCoarseGrid.TryGetBound(r, c, out Rect rect);
                    EditorHelper.DrawRectGizmos(rect);
                }
            }
            Handles.color = new Color(1, 0.5f, 0.25f);
            Vector2 start = boundingRect.min;
            Vector2 end = start;
            end.x = boundingRect.xMax;
            for (int r = 1; r < grid.looseDetailGrid.rowSize; r++)
            {
                end.y = start.y += grid.looseDetailGrid.rowSpacing;
                Handles.DrawDottedLine(start, end, 2);
            }
            start.x = end.x = boundingRect.xMin;
            start.y = boundingRect.yMax;
            end.y = boundingRect.yMin;
            for (int c = 1; c < grid.looseDetailGrid.columnSize; c++)
            {
                end.x = start.x += grid.looseDetailGrid.columnSpacing;
                Handles.DrawDottedLine(start, end, 2);
            }
            Handles.color = Color.white;
        }

        private void OnDrawGizmos()
        {
            var spatial = Selection.activeGameObject?.GetComponent<SpatialObject2D>();
            if (grid == null || spatial == null)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            queryResults.Clear();
            grid.QueryRectangle(spatial.RectBound, queryResults);
            foreach (var result in queryResults)
            {
                EditorHelper.DrawRectGizmos(result.RectBound);
            }
            Gizmos.color = Color.white;
        }

        private void BuildPartitionInEditor()
        {
            if (sourcesInBuild == null || sourcesInBuild.Length == 0)
            {
                return;
            }
            InitializeBoundsInEditor(false);
            if (boundingRect.size == Vector2.zero)
            {
                return;
            }

            ConstructGridInEditor();
        }

        public void InitializeBoundsInEditor(bool square = false)
        {
            boundingRect.min = boundingRect.max = Vector2.zero;
            for (int i = 0; i < sourcesInBuild.Length; i++)
            {
                var rect = sourcesInBuild[i].RectBound;
                if (boundingRect.size == Vector2.zero)
                {
                    boundingRect = rect;
                }
                else // expand rect
                {
                    boundingRect.min = Vector2.Min(boundingRect.min, rect.min);
                    boundingRect.max = Vector2.Max(boundingRect.max, rect.max);
                }
            }
            if (square)
            {
                var oldsize = boundingRect.size;
                float range = Mathf.Max(oldsize.x, oldsize.y) * 0.5f;
                boundingRect.min = boundingRect.center - new Vector2(range, range);
                boundingRect.size = new Vector2(range + range, range + range);
            }
        }

        public void ConstructGridInEditor()
        {
            grid = new PackedLooseGrid<ISpatial2D>(boundingRect,
                coarseRow,
                coarseColumn,
                detailRowMultiplier,
                detailColumnMultiplier);
            for (int i = 0; i < sourcesInBuild.Length; i++)
            {
                grid.AddElement(sourcesInBuild[i], sourcesInBuild[i].RectBound, out int r, out int c);
            }

            UnityEngine.Debug.Log($"#Loose-Detail\n{grid.looseDetailGrid}\n");
            UnityEngine.Debug.Log($"#Pack-Coarse\n{grid.packedCoarseGrid}");
        }

        public GridPartitionResult GenerateResultSerializationInEditor()
        {
            var result = new GridPartitionResult();

            result.worldRect = boundingRect;

            result.gridParams = ScriptableObject.CreateInstance<PartitionGridParams>();
            result.gridParams.coarseRow = coarseRow;
            result.gridParams.coarseColumn = coarseColumn;
            result.gridParams.detailRowMultiplier = detailRowMultiplier;
            result.gridParams.detailColumnMultiplier = detailColumnMultiplier;

            result.sources = new SpatialUnit2D[sourcesInBuild.Length];
            for (int i = 0; i < sourcesInBuild.Length; i++)
            {
                result.sources[i] = ((SpatialObject2D)sourcesInBuild[i]).ToSpatialUnit();
                grid.looseDetailGrid.CalculateRowColumn(sourcesInBuild[i].RectBound.center,
                    out int r, out int c);
                result.sources[i].cell.y = r;
                result.sources[i].cell.x = c;
            }

            result.detailCellsRect = new Rect[grid.looseDetailGrid.rowSize * grid.looseDetailGrid.columnSize];
            int index = 0;
            for (int r = 0; r < grid.looseDetailGrid.rowSize; r++)
            {
                for (int c = 0; c < grid.looseDetailGrid.columnSize; c++)
                {
                    result.detailCellsRect[index] = grid.looseDetailGrid.gridCells[index].rect;
                    index++;
                }
            }

            var coarseElements = new List<Vector2Int>();
            result.coarseCellElementHeaders = new int[grid.packedCoarseGrid.gridCells.Length];
            int header = 0;
            for (int i = 0; i < grid.packedCoarseGrid.gridCells.Length; ++i)
            {
                result.coarseCellElementHeaders[i] = header;
                var iter = grid.packedCoarseGrid.gridCells[i].GetEnumerator();
                while (iter.MoveNext())
                {
                    coarseElements.Add(iter.Current);
                    header++;
                }
            }
            result.coarseCellElements = coarseElements.ToArray();

            return result;
        }

        public void PrepareSpatialObjects(bool init)
        {
            sourcesInBuild = GetComponentsInChildren<ISpatial2D>(true);
            if (init)
            {
                for (int i = 0; i < sourcesInBuild.Length; i++)
                {
                    sourcesInBuild[i].CalculateBounds();
                }
            }
        }


        public void ForEachBuildSources(System.Action<ISpatial2D> callback)
        {
            for (int i = 0; i < sourcesInBuild.Length; ++i)
            {
                callback.Invoke(sourcesInBuild[i]);
            }
        }
#endif

        public virtual void SetSourceUnits(SpatialUnit2D[] sources)
        {
        }

        protected virtual void ReconstructElementsFromSources()
        {
            if (partitionData == null || partitionData.sources == null)
            {
                return;
            }
            for (int i = 0; i < partitionData.sources.Length; i++)
            {
                // deseiralize element directly, skip bound recalculation
                // (SpatialUnit2D is struct), sources[i] is copied & added into grid
                grid.looseDetailGrid.AddElementToCell(partitionData.sources[i],
                    partitionData.sources[i].cell.y, partitionData.sources[i].cell.x);
            }
        }

        public void DeserializeFrom(GridPartitionResult result)
        {
            boundingRect = result.worldRect;

            ApplyParameters(result.gridParams);

            SetSourceUnits(result.sources);

            // force re-create grid
            grid = new PackedLooseGrid<ISpatial2D>(boundingRect,
                coarseRow,
                coarseColumn,
                detailRowMultiplier,
                detailColumnMultiplier);

            if (overrideGridReconstruct != null)
            {
                overrideGridReconstruct(grid);
            }
            else
            {
                ReconstructElementsFromSources();
            }

            // deserialize detailed grid cell bounds
            for (int i = 0; i < result.detailCellsRect.Length; ++i)
            {
                grid.looseDetailGrid.gridCells[i].rect = result.detailCellsRect[i];
            }

            // deserialzie coarse grid cells
            int total = result.coarseCellElementHeaders.Length;
            int index = 0;
            for (int r = 0; r < grid.packedCoarseGrid.rowSize; r++)
            {
                for (int c = 0; c < grid.packedCoarseGrid.columnSize; c++)
                {
                    int current = result.coarseCellElementHeaders[index];
                    int top = (index + 1 >= total) ? result.coarseCellElements.Length :
                        result.coarseCellElementHeaders[index + 1];
                    while (current < top)
                    {
                        grid.packedCoarseGrid.gridCells[index].Add(result.coarseCellElements[current]);
                        current ++;
                    }
                    index++;
                }
            }
        }

        public void ApplyParameters(PartitionGridParams parameters)
        {
            coarseRow = parameters.coarseRow;
            coarseColumn = parameters.coarseColumn;
            detailRowMultiplier = parameters.detailRowMultiplier;
            detailColumnMultiplier = parameters.detailColumnMultiplier;
        }

        public void QueryRect(Rect rect, ICollection<ISpatial2D> results)
        {
            grid.QueryRectangle(rect, results);
        }

        public void FetchFromCoarseCell(int r, int c, ICollection<ISpatial2D> results, bool loose)
        {
            grid.FetchFromCoarseCell(r, c, results, loose);
        }

        public void FindContactCoarseCells(Rect rect, out Vector2Int rangeMin, out Vector2Int rangeMax)
        {
            grid.packedCoarseGrid.FindContactCellsRange(rect, out rangeMin, out rangeMax);
        }

        public void LocateCoarseCell(Vector2 pos, out int row, out int col)
        {
            grid.packedCoarseGrid.CalculateRowColumn(pos, out row, out col);
        }

        public void LocateDetailCell(Vector2 pos, out int row, out int col)
        {
            grid.looseDetailGrid.CalculateRowColumn(pos, out row, out col);
        }

        public void AddElement(ISpatial2D element, Rect rt, out int r, out int c)
        {
            grid.AddElement(element, rt, out r, out c);
        }

        public void AddElementToLooseDetailCell(ISpatial2D element, int row, int col)
        {
            grid.looseDetailGrid.AddElementToCell(element, row, col);
        }

        public void AddElementToLooseDetailCell(ISpatial2D element, Rect rt, int row, int col)
        {
            grid.looseDetailGrid.AddElementToCell(element, row, col, rt);
        }

        public void RemoveElement(ISpatial2D target, int row, int col)
        {
            grid.RemoveAnElement(target, row, col);
        }

        public void FastUpdateElementBounds(ISpatial2D element, Rect newbb, int row, int col)
        {
            element.RectBound = newbb;
            if (row == -1 || col == -1)
            {
                return;
            }
            int cellIdx = grid.looseDetailGrid.GetCellArrayIndex(row, col);
            // assume for small unit this loose-detail grid rect-bound change will not affect whole all
            // coarse-packed grid-cells boundaries
            grid.looseDetailGrid.gridCells[cellIdx].Expand(newbb);
            if (newbb.width > _UnitBigSize || newbb.height > _UnitBigSize)
            {
                grid.packedCoarseGrid.AddElementWithBound(new Vector2Int(col, row),
                    grid.looseDetailGrid.gridCells[cellIdx].rect, false);
            }
        }

        public bool FindPackedCoarseCellBound(int row, int col, out Rect bb)
        {
            return grid.packedCoarseGrid.TryGetBound(row, col, out bb);
        }

        public bool FindLooseDetailCellBound(int row, int col, out Rect bb)
        {
            return grid.looseDetailGrid.TryGetBound(row, col, out bb);
        }
    }
}

