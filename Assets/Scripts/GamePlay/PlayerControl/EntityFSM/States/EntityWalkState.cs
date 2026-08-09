using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体行走状态
    /// </summary>
    public class EntityWalkState : EntityLocomotionState
    {
        public override uint StateKey => EntityState.WALK;

        public EntityWalkState(EntityContext context) : base(context) { }

        #region 状态控制

        public override void Enter()
        {
            _locomotionSpeed = Config.walkSpeed;
        }

        protected override void CheckStateChange()
        {
            if (CheckGroundTransitions()) return;

            // 停止移动 → IDLE；按住 Sprint → SPRINT；toggle 开启 → RUN；否则留 WALK
            if (LastMoveInput == Vector2.zero) _stateMachine.ChangeState(EntityState.IDLE);
            else if (IsSprinting) _stateMachine.ChangeState(EntityState.SPRINT);
            else if (IsRunning) _stateMachine.ChangeState(EntityState.RUN);
        }

        #endregion
    }
}
