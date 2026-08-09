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
        public readonly Transform Orientation; // 视角变换
        public readonly Transform Mesh;        // 模型变换

        public Vector2 LastMoveInput;
        public Vector2 LastAimInput;

        // 瞬时状态需要每Tick结束重置
        #region 瞬时状态
        public bool DashRequest;
        public bool JumpRequest;
        #endregion

        // 持续状态由外部进行控制
        #region 持续状态
        public bool IsGrounded => Controller.isGrounded;
        public bool IsSprinting;
        public bool IsDashing;
        #endregion

        public EntityContext(EntityConfig config, CharacterController controller,
            Transform orientationTransform, Transform meshTransform)
        {
            StateMachine = new ExtendableStateMachine<uint>();
            Controller = controller;
            Orientation = orientationTransform;
            Mesh = meshTransform;
            Config = config;
        }

        /// <summary>
        /// 在 Tick 末尾调用，清除瞬时边沿标志
        /// 持续型输入（MoveInput/AimInput/IsSprintPressed）不清除
        /// </summary>
        public void ResetFrameFlags()
        {
            DashRequest = false;
            JumpRequest = false;
        }
    }
}
