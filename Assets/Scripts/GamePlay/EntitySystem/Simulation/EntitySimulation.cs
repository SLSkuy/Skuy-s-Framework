namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 单机、预测与权威模式共用的实体模拟实现。
    /// </summary>
    public sealed class EntitySimulation : IEntitySimulation, IEntityStateStore
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
        /// 捕获实体基础状态（状态机）。
        /// </summary>
        public EntitySimulationState CaptureSimulationState()
        {
            return new EntitySimulationState
            {
                entityState = _context.StateMachine.CurrentState
            };
        }

        /// <summary>
        /// 捕获完整回滚状态。
        /// </summary>
        public EntityRollbackState CaptureRollbackState()
        {
            MovementRollbackState movementState = _context.Movement.CaptureRollbackState();
            movementState.desiredLocomotionSpeed = _context.LocomotionSpeed;
            movementState.isSprinting = _context.IsSprinting;
            movementState.isRunning = _context.IsRunning;
            movementState.isGrounded = _context.IsGrounded;

            ViewRollbackState viewState = _context.View.CaptureRollbackState();
            viewState.isFocus = _context.IsFocus;

            return new EntityRollbackState
            {
                simulationState = CaptureSimulationState(),
                movementState = movementState,
                viewState = viewState,
                moveInput = _context.LastMoveInput,
                aimInput = _context.LastAimInput
            };
        }

        /// <summary>
        /// 一次性恢复完整回滚状态。
        /// </summary>
        public void RestoreRollbackState(in EntityRollbackState state)
        {
            _context.Movement.RestoreRollbackState(state.movementState);
            _context.View.RestoreRollbackState(state.viewState);
            _context.LastMoveInput = state.moveInput;
            _context.LastAimInput = state.aimInput;
            _context.LocomotionSpeed = state.movementState.desiredLocomotionSpeed;
            _context.IsSprinting = state.movementState.isSprinting;
            _context.IsRunning = state.movementState.isRunning;
            _context.IsFocus = state.viewState.isFocus;
            _context.ResetTickFlags();
            _context.StateMachine.ChangeState(state.simulationState.entityState);
        }
    }
}
