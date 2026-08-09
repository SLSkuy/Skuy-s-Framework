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

            // 松开 Sprint 键或停止移动时，回到当前档位对应地面状态（RUN/WALK/IDLE）；
            // 仍按住 Sprint 且在移动则留在 SPRINT。
            _stateMachine.ChangeState(GetGroundMoveState());
        }

        #endregion
    }
}
