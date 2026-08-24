using System;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体对象整体状态集合
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
