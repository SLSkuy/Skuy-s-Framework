using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体角色基类，装配状态机并暴露输入写入接口。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class EntityCharacter : BaseEntity
    {
        #region 属性
        public override bool TickDrive { get; set; }
        public override uint CurrentState => _context?.StateMachine.CurrentState ?? EntityState.IDLE;
        public override float LocomotionSpeed => _context?.locomotionSpeed ?? 0f;
        public override bool IsGrounded => _context?.IsGrounded ?? false;
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
        /// <summary>
        /// 推进状态机一帧，并清除瞬时输入标记。
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

        private void OnAnimatorMove()
        {
            if (TickDrive || !_config.rootMotion) return;

            _context?.Motor.ApplyRootMotion(_context.Animator.deltaPosition, Time.deltaTime);
        }
        #endregion

        #region 生命周期
        private void Start()
        {
            InitBaseEntity();

            MovementModule movementModule = GetComponent<MovementModule>();
            if (movementModule == null) movementModule = gameObject.AddComponent<MovementModule>();
            movementModule.Init(_characterController, _config, _orientation, _mesh);
            movementModule.Bind(this);
            SetContext(new EntityContext(_config, _characterController, movementModule, _animator));

            AnimationModule animationModule = GetComponent<AnimationModule>();
            if (animationModule == null) animationModule = gameObject.AddComponent<AnimationModule>();
            animationModule.Bind(this);
            animationModule.Init(_context);

            RegisterStates();
            _context.StateMachine.ChangeState(EntityState.IDLE);
        }

        private void Update()
        {
            if (!TickDrive) Simulate(Time.deltaTime);
        }
        #endregion
    }
}
