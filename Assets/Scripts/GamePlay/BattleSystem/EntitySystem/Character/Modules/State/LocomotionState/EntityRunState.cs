using UnityEngine;

namespace GamePlay.BattleSystem
{
    public class EntityRunState : EntityLocomotionState
    {
        public override EntityState StateKey => EntityState.RUN;

        public EntityRunState(EntityContext context) : base(context) { }

        #region 状态控制

        public override void Enter()
        {
            Context.DesiredSpeed = Config.runSpeed;
        }

        protected override void CheckStateChange()
        {
            if (CheckGroundTransitions()) return;

            // 停止移动 → IDLE；按住 Sprint → SPRINT；toggle 关闭 → WALK；否则留 RUN
            if (LastMoveInput == Vector2.zero) _stateMachine.ChangeState(EntityState.IDLE);
            else if(!IsRunning) _stateMachine.ChangeState(EntityState.WALK);
        }

        #endregion
    }
}
