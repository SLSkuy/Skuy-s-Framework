using Unity.Entities;
using Unity.Mathematics;

namespace Framework
{
    /// <summary>
    /// A*寻路配置组件
    /// </summary>
    public struct ASPathFindingConfig : IComponentData
    {
        /// <summary>
        /// 每帧最大A*寻路求解次数（推荐为10-20）
        /// </summary>
        public int MaxComputePerFrame;
    }

    /// <summary>
    /// A*寻路请求组件，需要重新计算路径时动态添加
    /// </summary>
    public struct ASRequester : IComponentData
    {
        /// <summary>
        /// 寻路目的地
        /// </summary>
        public float3 Destination;
    }

    /// <summary>
    /// A*寻路移动组件，拥有该组件才能根据寻路结果移动
    /// </summary>
    public struct ASFollower : IComponentData
    {
        public int TargetIndex;
        public bool PathAvailable;
        public bool DestinationReached;
    }

    /// <summary>
    /// 寻路操作命令实例，由请求系统临时创建，寻路完成后移除
    /// </summary>
    public struct ASOperation : IComponentData
    {
        public float3 StartPoint;
        public float3 TargetPoint;
    }

    /// <summary>
    /// 寻路结果，记录最近一次寻路计算状态
    /// </summary>
    public struct ASResult : IComponentData
    {
        public bool PathFounded;
        public bool FinishedSearch;
    }

    /// <summary>
    /// 寻路操作实例，记录当前正在进行的寻路操作
    /// </summary>
    public struct ASOperationsBuffer : IBufferElementData
    {
        public Entity RequestEntity;
        public ASOperation Operation;
    }

    /// <summary>
    /// 寻路结果单位节点，用于构成寻路节点链
    /// </summary>
    public struct ASPathBuffer : IBufferElementData
    {
        public float3 Point;
    }
}
