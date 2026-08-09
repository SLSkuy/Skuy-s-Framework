using UnityEngine;
using Framework.StateMachine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体状态机上下文，聚合状态运行所需的全部宿主数据。
    /// </summary>
    public class EntityContext
    {
        public readonly ExtendableStateMachine<uint> StateMachine;
        public readonly CharacterController Controller;
        public readonly EntityConfig Config;
        public readonly EntityMotor Motor;     // 运动执行器（持有跨状态共享的物理状态）

        public Vector2 LastMoveInput;
        public Vector2 LastAimInput;

        // 瞬时状态需要每Tick结束重置
        #region 瞬时状态
        public bool RunToggleRequest;         // 奔跑模式切换请求（toggle：按一次在 walk/run 之间切换）
        public bool DashRequest;              // 暂时屏蔽 dash，保留字段供未来恢复
        public bool JumpRequest;
        #endregion

        // 持续状态：由 Controller/状态机写入，跨 Tick 保持
        #region 持续状态
        public float locomotionSpeed;
        
        public bool IsGrounded => Controller.isGrounded;
        public bool IsSprinting;
        public bool IsRunning;
        #endregion

        public EntityContext(EntityConfig config, CharacterController controller, EntityMotor motor)
        {
            StateMachine = new ExtendableStateMachine<uint>();
            Controller = controller;
            Motor = motor;
            Config = config;
        }

        /// <summary>
        /// 在 Tick 末尾调用，清除瞬时边沿标志
        /// 持续型输入（MoveInput/AimInput/IsSprintHeld/IsRunning）不清除
        /// </summary>
        public void ResetFrameFlags()
        {
            RunToggleRequest = false;
            DashRequest = false;
            JumpRequest = false;
        }
    }
}
