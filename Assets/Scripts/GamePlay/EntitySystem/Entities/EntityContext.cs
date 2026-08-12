using Framework.StateMachine;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体状态机上下文，保存移动相关状态。
    /// </summary>
    public class EntityContext
    {
        public readonly ExtendableStateMachine<uint> StateMachine;
        public readonly EntityConfig Config;
        
        public readonly MovementModule Movement;

        public Vector2 LastMoveInput;
        public Vector2 LastAimInput;

        // 瞬时状态需要每Tick结束重置
        #region 瞬时状态
        public bool RunToggleRequest;         
        public bool JumpRequest;
        #endregion

        // 持续状态：由 Controller/状态机写入，跨 Tick 保持
        #region 持续状态
        public float locomotionSpeed;

        public bool IsGrounded => Movement.IsGrounded;
        public bool IsSprinting;
        public bool IsRunning;
        public bool IsFocus;    // 是否专注瞄准某一个位置
        #endregion

        public EntityContext(EntityConfig config, MovementModule movement)
        {
            StateMachine = new ExtendableStateMachine<uint>();
            Movement = movement;
            Config = config;
        }

        /// <summary>
        /// 在 Tick 末尾调用，清除瞬时边沿标志
        /// 持续型输入（MoveInput/AimInput/IsSprintHeld/IsRunning）不清除
        /// </summary>
        public void ResetFrameFlags()
        {
            RunToggleRequest = false;
            JumpRequest = false;
        }
    }
}
