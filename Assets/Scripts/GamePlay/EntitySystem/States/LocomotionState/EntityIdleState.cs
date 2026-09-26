using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体待机状态
    /// </summary>
    public class EntityIdleState : EntityLocomotionState
    {
        public override uint StateKey => EntityState.IDLE;

        public EntityIdleState(EntityContext context) : base(context) { }

        #region 状态控制

        public override void Enter()
        {
            Context.LocomotionSpeed = 0;
        }

        protected override void CheckStateChange()
        {
            if (CheckGroundTransitions()) return;

            // 有移动输入时按档位进入对应地面状态：Sprint 优先，其次 Run，默认 Walk
            if (LastMoveInput == Vector2.zero) return;
            if (IsSprinting) _stateMachine.ChangeState(EntityState.SPRINT);
            else if (IsRunning) _stateMachine.ChangeState(EntityState.RUN);
            else _stateMachine.ChangeState(EntityState.WALK);
        }

        #endregion
    }
}
