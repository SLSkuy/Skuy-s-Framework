using UnityEngine;
using Utils;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体角色基类，装配状态机并暴露输入写入接口。
    /// </summary>
    public class EntityCharacter : BaseEntity
    {
        private EntitySimulation _simulation;

        #region 属性
        public override uint CurrentState => _context?.StateMachine.CurrentState ?? EntityState.IDLE;
        #endregion

        #region 实体控制
        public override void Move(Vector2 dir)
        {
            _context.LastMoveInput = dir;
        }

        public override void Aim(Vector2 dir)
        {
            _context.LastAimInput = dir;
        }

        public override void Jump()
        {
            _context.JumpRequest = true;
        }

        /// <summary>
        /// 按下 Sprint 键，进入疾跑意图。
        /// </summary>
        public override void StartSprint()
        {
            _context.IsSprinting = true;
        }

        /// <summary>
        /// 松开 Sprint 键，退出疾跑意图。
        /// </summary>
        public override void StopSprint()
        {
            _context.IsSprinting = false;
        }

        /// <summary>
        /// 切换奔跑模式。
        /// </summary>
        public override void ToggleRun()
        {
            _context.RunToggleRequest = true;
        }
        #endregion

        #region 模拟入口
        protected override void InitComponents()
        {
            base.InitComponents();

            MovementModule movementModule = gameObject.GetOrAddComponent<MovementModule>();
            movementModule.Init(_config);
            movementModule.Bind(this);

            RotationModule rotationModule = gameObject.GetOrAddComponent<RotationModule>();
            rotationModule.Init(_config);
            rotationModule.Bind(this);

            SetContext(new EntityContext(_config, movementModule, rotationModule));

            RegisterStates();
            _context.StateMachine.ChangeState(EntityState.IDLE);
            _simulation = new EntitySimulation(_context);
        }

        /// <summary>
        /// 使用完整命令推进一次固定 Tick 模拟。
        /// </summary>
        public override void Step(uint tick, float deltaTime, in EntityInputCommand command)
        {
            _simulation?.Step(tick, deltaTime, command);
        }

        public override EntitySimulationState CaptureSimulationState()
        {
            return _simulation != null ? _simulation.CaptureSimulationState() : default;
        }

        public override EntityRollbackState CaptureRollbackState()
        {
            return _simulation != null ? _simulation.CaptureRollbackState() : default;
        }

        public override void RestoreRollbackState(in EntityRollbackState state)
        {
            _simulation?.RestoreRollbackState(state);
        }

        private void RegisterStates()
        {
            _context.StateMachine.RegisterState(new EntityIdleState(_context));
            _context.StateMachine.RegisterState(new EntityWalkState(_context));
            _context.StateMachine.RegisterState(new EntityRunState(_context));
            _context.StateMachine.RegisterState(new EntitySprintState(_context));
            _context.StateMachine.RegisterState(new EntityAirborneState(_context));
        }
        #endregion

    }
}
