using System;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 可跨网络比较和应用的实体 Transform 模拟状态。
    /// </summary>
    [Serializable]
    public struct EntitySimulationState
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 LinearVelocity;
        public Vector3 AngularVelocity;
        public uint LocomotionState;
        public bool IsGrounded;

        /// <summary>
        /// 判断状态中的 Transform 数值是否可安全进入网络协议。
        /// </summary>
        public bool IsFinite()
        {
            return IsFinite(Position) && IsFinite(Rotation) && IsFinite(LinearVelocity) && IsFinite(AngularVelocity);
        }

        private static bool IsFinite(Vector3 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        }

        private static bool IsFinite(Quaternion value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z) && float.IsFinite(value.w) &&
                value.x * value.x + value.y * value.y + value.z * value.z + value.w * value.w > 0.000001f;
        }
    }
}
