using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体疾跑状态
    /// </summary>
    public class EntitySprintState : EntityLocomotionState
    {
        public override uint StateKey => EntityState.SPRINT;

        public EntitySprintState(EntityContext context) : base(context) { }

        #region 状态控制

        public override void Enter()
        {
            _locomotionSpeed = Config.sprintSpeed;
        }

        protected override void CheckStateChange()
        {
            if (CheckGroundTransitions()) return;

            // 仍按住 Sprint 且在移动 → 留 SPRINT
            if (IsSprinting && LastMoveInput != Vector2.zero) return;

            // 松开 Sprint 或停止移动 → 回到当前档位对应地面状态
            if (LastMoveInput == Vector2.zero) _stateMachine.ChangeState(EntityState.IDLE);
            else if (IsRunning) _stateMachine.ChangeState(EntityState.RUN);
            else _stateMachine.ChangeState(EntityState.WALK);
        }

        #endregion
    }
}
