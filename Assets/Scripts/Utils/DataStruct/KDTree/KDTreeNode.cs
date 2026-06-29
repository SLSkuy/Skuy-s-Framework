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
    /// Node 记录：
    /// 1. 我管 permutation 的哪一段
    /// 2. 我的空间范围 Bound 是什么
    /// 3. 如果我被分割了，分割轴和分割坐标是什么
    /// 4. 我的左右子节点是谁
    /// </summary>
    public struct KDTreeNode
    {
        public KDTreeBound Bound;

        /// <summary>
        /// 当前节点的划分轴及区间
        /// </summary>
        public KDTreePartitionAxis PartitionAxis;
        
        /// <summary>
        /// 当前节点的划分区间点索引值，左闭又开 [Start, End)
        /// </summary>
        public int2 Boundary;

        /// <summary>
        /// 分割平面在PartitionAxis上的坐标值
        /// 构建时按划分轴排序，并选择中间点作为分割点
        /// </summary>
        public float PartitionCoordinate;

        /// <summary>负子节点索引</summary>
        public int NegativeChildIndex;
        /// <summary>正子节点索引</summary>
        public int PositiveChildIndex;

        public int Count => Boundary.y - Boundary.x;
        public bool IsLeaf => PartitionAxis == KDTreePartitionAxis.Leaf;
        
        /// <summary>
        /// 计算当前节点应该使用的分割坐标轴
        /// </summary>
        public KDTreePartitionAxis GetPartitionAxis()
        {
            float3 size = Bound.Size;
            KDTreePartitionAxis partitionAxis = KDTreePartitionAxis.X;
            float axisSize = size.x;
            if (axisSize < size.y)
            {
                partitionAxis = KDTreePartitionAxis.Y;
                axisSize = size.y;
            }
            if (axisSize < size.z)
            {
                partitionAxis = KDTreePartitionAxis.Z;
            }
            return partitionAxis;
        }
    }
}
