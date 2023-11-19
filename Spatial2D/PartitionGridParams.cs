using UnityEngine;

namespace Spatial
{
    [CreateAssetMenu(fileName = "grid_levelmap_",
        menuName = "Ocean/2D/Map Spatial Partition Grid Parameters", order = 1)]
    public class PartitionGridParams : ScriptableObject
    {
        public int coarseRow;
        public int coarseColumn;
        public int detailRowMultiplier;
        public int detailColumnMultiplier;
    }
}

