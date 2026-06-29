using Unity.Entities;

namespace Framework
{
    /// <summary>
    /// 惩罚区域实体，与障碍物不同，不会禁止Agent通过，但会增加通过代价
    /// </summary>
    public struct ASPenaltyArea : IComponentData
    {
        /// <summary>
        /// 惩罚区域惩罚值，影响惩罚区域附近节点的惩罚值
        /// </summary>
        public int PenaltyValue;
    }
}