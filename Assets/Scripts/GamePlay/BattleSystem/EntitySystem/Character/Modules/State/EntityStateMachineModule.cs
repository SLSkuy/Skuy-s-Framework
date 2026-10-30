using Framework;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 托管角色状态注册、固定 Tick 更新与状态回滚。
    /// </summary>
    public class EntityStateMachineModule : EntitySnapshotModuleBase<StateSnapshot>
    {
        private readonly StateMachine<EntityState> _stateMachine = new();

        #region 属性
        public override ModuleType ModuleType => ModuleType.StateMachine;
        public EntityState CurrentState => _stateMachine.CurrentState;
        public StateMachine<EntityState> StateMachine => _stateMachine;
        #endregion

        /// <summary>
        /// 模块绑定完成后注册角色状态并进入初始状态
        /// </summary>
        protected override void OnBound(EntityCharacter target)
        {
            RegisterStates();
            ChangeState(EntityState.IDLE);
        }
        
        /// <summary>
        /// 切换到已注册的角色状态。
        /// </summary>
        public void ChangeState(EntityState state)
        {
            _stateMachine.ChangeState(state);
        }
        
        /// <summary>
        /// 注册默认状态，派生模块可追加或替换角色专属状态。
        /// </summary>
        protected virtual void RegisterStates()
        {
            _stateMachine.RegisterState(new EntityIdleState(Context));
            _stateMachine.RegisterState(new EntityWalkState(Context));
            _stateMachine.RegisterState(new EntityRunState(Context));
            _stateMachine.RegisterState(new EntitySprintState(Context));
            _stateMachine.RegisterState(new EntityAirborneState(Context));
        }

        /// <summary>
        /// 由实体模拟推进一次状态更新。
        /// </summary>
        public void Step(float deltaTime)
        {
            _stateMachine.Update(deltaTime);
        }

        public override StateSnapshot CaptureSnapshot()
        {
            return new StateSnapshot { entityState = CurrentState };
        }

        public override void RestoreSnapshot(in StateSnapshot state)
        {
            ChangeState(state.entityState);
        }
    }
}
