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
            _locomotionSpeed = 0;
        }

        protected override void CheckStateChange()
        {
            if (CheckGroundTransitions()) return;

            // 有移动输入时按当前档位进入对应地面状态（WALK/RUN/SPRINT）
            _stateMachine.ChangeState(GetGroundMoveState());
        }

        #endregion
    }
}
