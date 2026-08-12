using UnityEngine;
using Utils;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体角色基类，装配状态机并暴露输入写入接口。
    /// </summary>
    public class EntityCharacter : BaseEntity
    {
        #region 属性
        public override bool TickDrive { get; set; }
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

            SetContext(new EntityContext(_config, movementModule));

            RegisterStates();
            _context.StateMachine.ChangeState(EntityState.IDLE);
        }

        /// <summary>
        /// 推进状态机一帧，并清理瞬时输入标记。
        /// </summary>
        public override void Simulate(float deltaTime)
        {
            _context.StateMachine.Update(deltaTime);
            _context.ResetFrameFlags();
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

        #region 生命周期
        private void Update()
        {
            if (!IsInitialized) return;
            if (!TickDrive) Simulate(Time.deltaTime);
        }
        #endregion
    }
}
