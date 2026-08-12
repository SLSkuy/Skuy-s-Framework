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
        public void Step(uint tick, float deltaTime, in EntityInputCommand command)
        {
            _context.CurrentTick = tick;
            _context.LastMoveInput = command.Move;
            _context.LastAimInput = command.Aim;
            _context.IsSprinting = command.IsHeld(EntityCommandButtons.Sprint);
            _context.JumpRequest = command.IsPressed(EntityCommandButtons.Jump);
            _context.RunToggleRequest = command.IsPressed(EntityCommandButtons.ToggleRun);

            _context.Rotation.Rotate(command.Move, command.Aim, deltaTime);
            _context.StateMachine.Update(deltaTime);
            _context.ResetTickFlags();
        }

        /// <summary>
        /// 捕获当前网络可见 Transform 状态。
        /// </summary>
        public EntitySimulationState CaptureSimulationState()
        {
            return new EntitySimulationState
            {
                Position = _context.Movement.Position,
                Rotation = _context.Rotation.Rotation,
                LinearVelocity = _context.Movement.LinearVelocity,
                AngularVelocity = _context.Rotation.AngularVelocity
            };
        }

        /// <summary>
        /// 捕获完整回滚状态。
        /// </summary>
        public EntityRollbackState CaptureRollbackState()
        {
            return new EntityRollbackState
            {
                TransformState = CaptureSimulationState(),
                MovementState = _context.Movement.CaptureRollbackState(),
                StateKey = _context.StateMachine.CurrentState,
                MoveInput = _context.LastMoveInput,
                AimInput = _context.LastAimInput,
                LocomotionSpeed = _context.LocomotionSpeed,
                IsSprinting = _context.IsSprinting,
                IsRunning = _context.IsRunning,
                IsFocus = _context.IsFocus
            };
        }

        /// <summary>
        /// 一次性恢复完整回滚状态。
        /// </summary>
        public void RestoreRollbackState(in EntityRollbackState state)
        {
            _context.Movement.RestoreRollbackState(state.TransformState.Position, state.MovementState);
            _context.Rotation.Restore(state.TransformState.Rotation, state.TransformState.AngularVelocity);
            _context.LastMoveInput = state.MoveInput;
            _context.LastAimInput = state.AimInput;
            _context.LocomotionSpeed = state.LocomotionSpeed;
            _context.IsSprinting = state.IsSprinting;
            _context.IsRunning = state.IsRunning;
            _context.IsFocus = state.IsFocus;
            _context.ResetTickFlags();
            _context.StateMachine.ChangeState(state.StateKey);
        }
    }
}
