using Unity.Entities;

namespace Framework
{
    /// <summary>
    /// 障碍物组件定义
    /// </summary>
    public struct ASObstacle : IComponentData
    {
        /// <summary>
        /// 障碍物惩罚值，影响障碍物附近节点的惩罚值
        /// 用于控制 Agent 远离障碍物的程度
        /// </summary>
        public int PenaltyValue;
    }
}