using Framework;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体状态机上下文，保存移动与视角相关状态。
    /// </summary>
    public class EntityContext
    {
        public readonly ExtendableStateMachine<uint> StateMachine;
        public readonly EntityConfig Config;
        public readonly MovementModule Movement;
        public readonly ViewModule View;

        public Vector2 LastMoveInput;
        public Vector2 LastAimInput;
        public uint CurrentTick;

        #region 瞬时状态
        public bool RunToggleRequest;
        public bool JumpRequest;
        #endregion

        #region 持续状态
        public float LocomotionSpeed;

        public bool IsGrounded => Movement.IsGrounded;
        public bool IsSprinting;
        public bool IsRunning;
        public bool IsFocus;
        #endregion

        public EntityContext(EntityConfig config, MovementModule movement, ViewModule view)
        {
            Config = config;
            StateMachine = new ExtendableStateMachine<uint>();
            Movement = movement;
            View = view;
        }

        /// <summary>
        /// 在 Tick 末尾调用，清除瞬时边沿标志。
        /// 持续型输入（MoveInput/AimInput/IsSprintHeld/IsRunning）不清除。
        /// </summary>
        public void ResetTickFlags()
        {
            RunToggleRequest = false;
            JumpRequest = false;
        }
    }
}
