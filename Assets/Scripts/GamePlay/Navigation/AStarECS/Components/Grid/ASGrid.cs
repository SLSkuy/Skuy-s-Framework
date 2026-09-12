using Unity.Entities;

namespace GamePlay.Navigation
{
    /// <summary>
    /// A*网格，用于算法路径查找
    /// </summary>
    public struct ASGrid : IComponentData
    {
        /// <summary>X 方向包含多少个格子。</summary>
        public int XCount;
        
        /// <summary>Z 方向包含多少个格子。当前网格绘制在 Unity 的 XZ 平面。</summary>
        public int YCount;
        
        /// <summary>单个正方形格子的世界空间边长。</summary>
        public float CellSize;

        /// <summary>
        /// 障碍物惩罚值扩散范围
        /// </summary>
        public int BlurSize;

        /// <summary>
        /// 障碍物层，禁止通行
        /// </summary>
        public int ObstacleLayerMask;
        
        /// <summary>
        /// 惩罚层，增加节点通行代价
        /// </summary>
        public int PenaltyLayerMask;

        /// <summary>
        /// 当目标节点不可通行时，BFS搜索最近可通行节点的范围上限（节点数，推荐为10）
        /// </summary>
        public int TargetInaccessibleSearchTolerance;

        public bool NeedRefreshGrid;
        public bool NeedRefreshIsland;
        public bool IslandCreated;
    }
}