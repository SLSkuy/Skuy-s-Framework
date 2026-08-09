using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体奔跑状态：通过 walk/run toggle 进入，速度介于 walk 与 sprint 之间。
    /// Sprint 键按住时切到 SPRINT（最高优先级），停止移动回 IDLE，toggle 关闭回 WALK。
    /// </summary>
    public class EntityRunState : EntityLocomotionState
    {
        public override uint StateKey => EntityState.RUN;

        public EntityRunState(EntityContext context) : base(context) { }

        #region 状态控制

        public override void Enter()
        {
            _locomotionSpeed = Config.runSpeed;
        }

        protected override void CheckStateChange()
        {
            if (CheckGroundTransitions()) return;

            // 停止移动 → IDLE；按住 Sprint → SPRINT；toggle 关闭 → WALK；否则留 RUN
            if (LastMoveInput == Vector2.zero) _stateMachine.ChangeState(EntityState.IDLE);
            else if (IsSprinting) _stateMachine.ChangeState(EntityState.SPRINT);
            else if (!IsRunning) _stateMachine.ChangeState(EntityState.WALK);
        }

        #endregion
    }
}
