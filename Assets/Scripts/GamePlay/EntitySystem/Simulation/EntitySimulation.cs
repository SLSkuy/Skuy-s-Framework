namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 单机、预测与权威模式共用的实体模拟实现。
    /// </summary>
    public sealed class EntitySimulation : IEntitySimulation, IEntityStateStore<EntitySnapshot>
    {
        private readonly EntityContext _context;

        /// <summary>
        /// 创建实体模拟并绑定运行上下文。
        /// </summary>
        public EntitySimulation(EntityContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 应用完整命令并推进一个固定 Tick。
        /// </summary>
        public void Step(uint tick, float deltaTime, in EntityCommand command)
        {
            _context.CurrentTick = tick;
            _context.LastMoveInput = command.move;
            _context.LastAimInput = command.aim;
            _context.IsSprinting = command.IsHeld(EntityCommandFlags.Sprint);
            _context.JumpRequest = command.IsPressed(EntityCommandFlags.Jump);
            _context.RunToggleRequest = command.IsPressed(EntityCommandFlags.ToggleRun);

            _context.View.Look(command.aim, deltaTime);
            _context.Movement.Rotate(command.move, _context.View.Yaw, deltaTime);
            _context.StateMachine.Update(deltaTime);
            _context.ResetTickFlags();
        }

        /// <summary>
        /// 捕获完整回滚状态。
        /// </summary>
        public EntitySnapshot CaptureRollbackState()
        {
            StateSnapshot state = new StateSnapshot()
            {
                entityState = _context.StateMachine.CurrentState,
            };
            
            MovementSnapshot movement = _context.Movement.CaptureRollbackState();
            movement.desiredLocomotionSpeed = _context.LocomotionSpeed;
            movement.isSprinting = _context.IsSprinting;
            movement.isRunning = _context.IsRunning;

            ViewSnapshot view = _context.View.CaptureRollbackState();
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
        public void RestoreRollbackState(in EntitySnapshot state)
        {
            _context.Movement.RestoreRollbackState(state.movement);
            _context.View.RestoreRollbackState(state.view);
            _context.LocomotionSpeed = state.movement.desiredLocomotionSpeed;
            _context.IsSprinting = state.movement.isSprinting;
            _context.IsRunning = state.movement.isRunning;
            _context.IsFocus = state.view.isFocus;
            _context.ResetTickFlags();
            _context.StateMachine.ChangeState(state.state.entityState);
        }
    }
}
