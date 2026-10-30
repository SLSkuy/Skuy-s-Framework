namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 单机、预测与权威模式共用的实体模拟实现。
    /// </summary>
    public sealed class EntitySimulator
    {
        private readonly EntityContext _context;
        private readonly EntityStateMachineModule _stateMachine;
        private readonly MovementModule _movement;
        private readonly ViewModule _view;

        /// <summary>
        /// 创建实体模拟并绑定运行上下文。
        /// </summary>
        public EntitySimulator(EntityContext context)
        {
            _context = context;
            _movement = context.Get<MovementModule>();
            _view = context.Get<ViewModule>();
            _stateMachine = context.Get<EntityStateMachineModule>();
        }

        /// <summary>
        /// 应用完整命令并推进一个固定 Tick。
        /// </summary>
        public void Step(uint tick, float deltaTime, in EntityControlCommand command)
        {
            _context.CurrentTick = tick;
            _context.LastMoveInput = command.move;
            _context.LastAimInput = command.aim;
            _context.IsSprinting = command.IsHeld(EntityCommandFlags.Sprint);
            _context.JumpRequest = command.IsPressed(EntityCommandFlags.Jump);
            _context.RunToggleRequest = command.IsPressed(EntityCommandFlags.ToggleRun);

            _view.Look(command.aim, deltaTime);
            _movement.Rotate(command.move, _view.Yaw, deltaTime);
            _stateMachine.Step(deltaTime);
            _context.ResetTickFlags();
        }

        /// <summary>
        /// 捕获完整回滚状态。
        /// </summary>
        public EntitySnapshot CaptureSnapshot()
        {
            StateSnapshot state = _stateMachine.CaptureSnapshot();
            
            MovementSnapshot movement = _movement.CaptureSnapshot();
            movement.locomotionSpeed = _context.LocomotionSpeed;
            movement.isSprinting = _context.IsSprinting;
            movement.isRunning = _context.IsRunning;

            ViewSnapshot view = _view.CaptureSnapshot();
            view.isFocus = _context.IsFocus;

            return new EntitySnapshot
            {
                state = state,
                movement = movement,
                view = view
            };
        }

        /// <summary>
        /// 一次性恢复完整回滚状态。
        /// </summary>
        public void RestoreSnapshot(in EntitySnapshot state)
        {
            _movement.RestoreSnapshot(state.movement);
            _view.RestoreSnapshot(state.view);
            _context.LocomotionSpeed = state.movement.locomotionSpeed;
            _context.IsSprinting = state.movement.isSprinting;
            _context.IsRunning = state.movement.isRunning;
            _context.IsFocus = state.view.isFocus;
            _context.ResetTickFlags();
            _stateMachine.RestoreSnapshot(state.state);
        }
    }
}
