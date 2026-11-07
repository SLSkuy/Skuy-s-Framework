using Framework;

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
        private readonly AbilityModule _ability;
        private readonly CombatModule _combat;

        /// <summary>
        /// 创建实体模拟并绑定运行上下文。
        /// </summary>
        public EntitySimulator(EntityContext context)
        {
            _context = context;
            _movement = context.Get<MovementModule>();
            _view = context.Get<ViewModule>();
            _ability = context.Get<AbilityModule>();
            _stateMachine = context.Get<EntityStateMachineModule>();
            _combat = context.Get<CombatModule>();
        }

        /// <summary>
        /// 应用完整命令并推进一个固定 Tick。
        /// </summary>
        public void Step(uint tick, float deltaTime, in EntityControlCommand command)
        {
            _context.CurrentTick = tick;
            _context.LastMoveInput = command.move;
            _context.LastAimInput = command.aim;
            _context.IsRunning = command.IsHeld(EntityCommandFlags.Run);
            _context.JumpRequest = command.IsPressed(EntityCommandFlags.Jump);
            
            // 处理战斗控制输入，内部处理是否需要进入Action状态
            _combat.CheckAction(command);
            
            // 先推进上一拍已存在的技能，再由状态机处理本拍新施法请求。
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
            ViewSnapshot view = _view.CaptureSnapshot();
            AbilitySnapshot ability = _ability.CaptureSnapshot();

            return new EntitySnapshot
            {
                state = state,
                movement = movement,
                view = view,
                ability = ability,
            };
        }

        /// <summary>
        /// 一次性恢复完整回滚状态。
        /// </summary>
        public void RestoreSnapshot(in EntitySnapshot state)
        {
            _stateMachine.RestoreSnapshot(state.state);
            _movement.RestoreSnapshot(state.movement);
            _view.RestoreSnapshot(state.view);
            _ability.RestoreSnapshot(state.ability);
            _context.ResetTickFlags();
        }
    }
}