using Unity.Mathematics;

namespace Framework
{
    /// <summary>
    /// KD-Tree节点划分轴选择
    /// </summary>
    public enum KDTreePartitionAxis
    {
        Leaf = -1,  // 叶子节点，不分割
        X = 0,
        Y = 1,
        Z = 2
    }
    
    /// <summary>
    /// KD-Tree树节点定义
    /// </summary>
    public struct KDTreeNode
    {
        public KDTreeBound Bound;

        /// <summary>
        /// 当前节点的划分轴及区间
        /// </summary>
        public KDTreePartitionAxis PartitionAxis;
        
        /// <summary>
        /// 当前节点的划分区间，左闭又开 [Start, End)
        /// </summary>
        public int2 Boundary;

        /// <summary>
        /// 分割平面在PartitionAxis上的坐标值
        /// 坐标小于PartitionCoordinate 的点归入负子节点
        /// 坐标大于等于PartitionCoordinate 的点归入正子节点
        /// </summary>
        public float PartitionCoordinate;

        /// <summary>负子节点索引</summary>
        public int NegativeChildIndex;
        /// <summary>正子节点索引</summary>
        public int PositiveChildIndex;

        public int Count => Boundary.y - Boundary.x;
        public bool IsLeaf => PartitionAxis == KDTreePartitionAxis.Leaf;
    }
}