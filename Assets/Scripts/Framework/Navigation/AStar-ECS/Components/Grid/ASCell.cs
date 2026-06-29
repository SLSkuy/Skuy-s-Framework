using System;
using Unity.Entities;

namespace Framework
{
    /// <summary>
    /// 单个网格节点数据定义
    /// </summary>
    public struct ASCell : IBufferElementData, IEquatable<ASCell>, INativeHeapItem<ASCell>
    {
        /// <summary>角色是否可以经过该节点</summary>
        public bool IsAccessible;

        /// <summary>节点在二维网格中的 X 坐标</summary>
        public int X;

        /// <summary>节点在二维网格中的 Y 坐标，对应 Unity 世界空间的 Z 轴</summary>
        public int Y;

        /// <summary>G 代价：起点到此节点的实际移动代价</summary>
        public int GCost;

        /// <summary>H 代价：此节点到目标节点的启发式估计代价</summary>
        public int HCost;

        /// <summary>当前节点的惩罚值，避免走到该节点上</summary>
        public float Penalty;

        /// <summary>
        /// 当前节点所属于的岛屿ID
        /// </summary>
        public int IslandID;

        /// <summary>
        /// A*回溯路径时使用：记录父节点在Nodes数组中的一维索引
        /// </summary>
        public int PreviousNodeIndex;
        
        /// <summary>
        /// 堆索引
        /// </summary>
        public int HeapIndex { get; set; }

        #region 常用运算方法

        public int CompareTo(ASCell other)
        {
            int compare = GridUtils.CellFCost(this).CompareTo(GridUtils.CellFCost(other));
            if (compare == 0)
            {
                compare = HCost.CompareTo(other.HCost);
            }
            return -compare;
        }
        
        public bool Equals(ASCell other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is ASCell other && Equals(other);
        }
        
        public override string ToString()
        {
            return (GCost + HCost).ToString();
        }

        public override int GetHashCode()
        {
            return X ^ (Y << 16) ^ (Y >> 16);
        } 

        #endregion
    }
}