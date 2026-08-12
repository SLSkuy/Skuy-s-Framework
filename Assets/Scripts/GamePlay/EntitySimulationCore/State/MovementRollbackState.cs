using System;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 移动模块为确定性回放保存的内部状态。
    /// </summary>
    [Serializable]
    public struct MovementRollbackState
    {
        public Vector3 LastMoveDirection;
        public Vector3 LinearVelocity;
        public Vector3 DashDirection;
        public float LocomotionSpeed;
        public float VerticalVelocity;
        public float DashRemainingTime;
        public int JumpCount;
        public bool IsDashing;
    }
}
