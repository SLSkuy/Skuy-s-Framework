using Unity.Entities;
using Unity.Mathematics;

namespace Framework
{
    /// <summary>
    /// ORCA约束线。
    /// 候选速度必须处在所有约束线的可行侧，才能同时避开附近角色。
    /// 当前导航运行在Unity的XZ平面，所以这里用float3保存，计算时只取x/z。
    /// </summary>
    public struct ASORCALine : IBufferElementData
    {
        /// <summary>
        /// 速度空间中的线段参考点。
        /// ORCA算法会把“需要修正的速度”平分给双方，参考点就是当前速度加上一半修正量。
        /// </summary>
        public float3 Point;

        /// <summary>
        /// 约束线方向。
        /// 若 cross(Direction, Point - CandidateVelocity) <= 0，则候选速度位于可行侧。
        /// </summary>
        public float3 Direction;
    }
}
