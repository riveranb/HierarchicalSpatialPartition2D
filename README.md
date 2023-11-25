# HierarchicalSpatialPartition2D
Hierarchical (double layered) spatial (uniform grid) partitioner for 2D case.

* References discussion article on StackOverflow (https://stackoverflow.com/questions/41946007/efficient-and-well-explained-implementation-of-a-quadtree-for-2d-collision-det#:~:text=5.%20Loose/Tight%20Double%2DGrid%20With%20500k%20Agents)

## Algorithm
* Create 2 uniform grids, coarse and tightly packed one versus detailed and loose boundary one. Both grids are imposed the same world space boundary.
* Coarse grid cells manages what detailed grid cells are occupied inside.
* Detailed grid cells manages real (game object) units, and cell-size are calculated precisely according all contained units.
* The grid-cell container should use continuous collection data structure (array/list). The (game object) unit container should use linked-list as data structure for the memory cache friendly purpose, and grid-cell only stores (linked-list) header of contained units to optimize memory allocation usage.

When searching for a region of interest (ROI), a rectangle.
1. Find out intersecting coarse grid cells.
2. For each coarse grid cell, find intersected detailed grid cells.
3. For each detailed grid cell, find intersected units as part of searching result.

### PackedLooseGrid<T> class
Hierarchical double grids data structure, from coarse to fine/detailed grid. Coarse grid is a container-based grid to store contained detailed loose grid cells. Detailed grid is a loose grid for each cell storing real target-elements with their center position inside and calculates real 2D bounds of all contained elements.

### SpatialPartitioner2D class
Spatial partition implementation class, to privide construct the grid, add/remove elements with using PackedLooseGrid class. The implementatioin in the repository focuses on parition management with renderers.
