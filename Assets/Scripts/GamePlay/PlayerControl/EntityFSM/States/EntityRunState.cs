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

            // 档位变化（toggle→WALK / 按住 sprint→SPRINT / 停止→IDLE）时切换；
            // 仍处于 RUN 时 ChangeState 自身会被忽略。
            _stateMachine.ChangeState(GetGroundMoveState());
        }

        #endregion
    }
}
