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

            // 档位变化（toggle→RUN / 按住 sprint→SPRINT / 停止→IDLE）时切换；
            // 仍处于 WALK 时 ChangeState 自身会被忽略。
            _stateMachine.ChangeState(GetGroundMoveState());
        }

        #endregion
    }
}
