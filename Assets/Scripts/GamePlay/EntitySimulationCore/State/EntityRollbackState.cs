using System;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 本地预测恢复与重放所需的完整实体状态。
    /// </summary>
    [Serializable]
    public struct EntityRollbackState
    {
        public EntitySimulationState TransformState;
        public MovementRollbackState MovementState;
        public uint StateKey;
        public Vector2 MoveInput;
        public Vector2 AimInput;
        public float LocomotionSpeed;
        public bool IsSprinting;
        public bool IsRunning;
        public bool IsFocus;
    }
}
