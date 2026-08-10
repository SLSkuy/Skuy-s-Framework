using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体角色基类，装配状态机并暴露输入写入接口。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class EntityCharacter : MonoBehaviour
    {
        [SerializeField] protected EntityConfig config;

        private EntityContext _context;

        /// <summary>
        /// 外部驱动开关 true 时 Update 不自动 Simulate，由网络驱动器在 Tick 边界用 TickDeltaTime 驱动。
        /// 单机模式 false（默认）
        /// </summary>
        public bool tickDrive;

        #region 实体控制

        public void Move(Vector2 dir)
        {
            _context.LastMoveInput = dir;
        }

        public void Aim(Vector2 dir)
        {
            _context.LastAimInput = dir;
        }

        public void Jump()
        {
            _context.JumpRequest = true;
        }

        /// <summary>
        /// 按下 Sprint 键（持续型）：进入疾跑，由状态机在 CheckStateChange 中决策是否进 SPRINT。
        /// </summary>
        public void StartSprint()
        {
            _context.IsSprinting = true;
        }

        /// <summary>
        /// 松开 Sprint 键：退出疾跑，回到当前档位（RUN/WALK/IDLE）。
        /// </summary>
        public void StopSprint()
        {
            _context.IsSprinting = false;
        }

        /// <summary>
        /// 切换奔跑模式（toggle）：按一次在 walk/run 之间切换。
        /// 实际切换由状态机 Tick 统一处理 IsRunning 翻转。
        /// </summary>
        public void ToggleRun()
        {
            _context.RunToggleRequest = true;
        }

        #endregion

        #region 模拟入口

        /// <summary>
        /// 推进状态机一帧，并清除瞬时输入标志
        /// 由 Update 自动调用；外部 Tick 驱动场景（如网络层）
        /// </summary>
        public void Simulate(float deltaTime)
        {
            _context.StateMachine.Update(deltaTime);
            _context.ResetFrameFlags();
        }

        /// <summary>
        /// 注册实体状态，可拓展注册状态。
        /// 注：Dash 状态当前已屏蔽（CheckGroundTransitions 中 DashRequest 分支注释），
        /// 同步移除注册以彻底禁用；恢复时取消注释并在此重新注册即可。
        /// </summary>
        protected void RegisterStates()
        {
            _context.StateMachine.RegisterState(new EntityIdleState(_context));
            _context.StateMachine.RegisterState(new EntityWalkState(_context));
            _context.StateMachine.RegisterState(new EntityRunState(_context));
            _context.StateMachine.RegisterState(new EntitySprintState(_context));
            // _context.StateMachine.RegisterState(new EntityDashState(_context));
            _context.StateMachine.RegisterState(new EntityAirborneState(_context));
        }

        private void OnAnimatorMove()
        {
            // 网络预测模式下禁用 root motion 位移，避免 Time.deltaTime 污染预测结果
            if (tickDrive || !config.rootMotion) return;

            // 开启 root motion 时由动画驱动位移，仍保留重力/跳跃物理
            _context?.Motor.ApplyRootMotion(_context.Animator.deltaPosition, Time.deltaTime);
        }

        #endregion

        #region 生命周期

        private void Start()
        {
            CharacterController controller = GetComponent<CharacterController>();
            Animator animator = GetComponent<Animator>();
            Transform orientation = transform.Find("orientation");
            Transform mesh = transform.Find("mesh");

            // 加载实体配置数据实例
            if (!config) config = EntityConfig.Instance;

            // 初始化组件：Motor 持有跨状态共享的物理状态，Context 聚合所有宿主数据
            EntityMotor motor = new EntityMotor(controller, config, orientation, mesh);
            _context = new EntityContext(config, controller, motor, animator);

            // 注入上下文到动画控制器，供其读取 locomotionSpeed 作为 Speed 参数来源
            EntityAnimator entityAnimator = GetComponent<EntityAnimator>();
            if (entityAnimator != null) entityAnimator.Init(_context);

            // 初始化状态
            RegisterStates();
            _context.StateMachine.ChangeState(EntityState.IDLE);
        }

        private void Update()
        {
            if (!tickDrive) Simulate(Time.deltaTime);
        }

        #endregion
    }
}
