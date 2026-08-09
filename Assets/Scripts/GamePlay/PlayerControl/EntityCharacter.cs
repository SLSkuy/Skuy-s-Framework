using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体角色基类，装配状态机并暴露输入写入接口。
    /// 旧版的动作型接口（Move/Aim/Jump/Dash/StartSprint/StopSprint）已被
    /// "输入数据 + 状态机自驱"模式取代：Controller 只写入输入数据，行为决策权在状态机。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class EntityCharacter : MonoBehaviour
    {
        [SerializeField] protected EntityConfig config;

        private EntityContext _context;
        
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

        public void StartSprint()
        {
            _context.IsSprinting = true;
            _context.DashRequest = true;
        }

        public void StopSprint()
        {
            _context.IsSprinting = false;
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
        /// 注册实体状态，可拓展注册状态
        /// </summary>
        protected virtual void RegisterStates()
        {
            _context.StateMachine.RegisterState(new EntityIdleState(_context));
            _context.StateMachine.RegisterState(new EntityWalkState(_context));
            _context.StateMachine.RegisterState(new EntitySprintState(_context));
            _context.StateMachine.RegisterState(new EntityDashState(_context));
            _context.StateMachine.RegisterState(new EntityAirborneState(_context));
        }

        #endregion

        #region 生命周期

        private void Start()
        {
            CharacterController controller = GetComponent<CharacterController>();
            Transform orientation = transform.Find("orientation");
            Transform mesh = transform.Find("mesh");

            if (config == null) config = EntityConfig.Instance;

            // 初始化组件：Motor 持有跨状态共享的物理状态，Context 聚合所有宿主数据
            EntityMotor motor = new EntityMotor(controller, config, orientation, mesh);
            _context = new EntityContext(config, controller, motor);

            // 初始化状态
            RegisterStates();
            _context.StateMachine.ChangeState(EntityState.IDLE);
        }
        
        private void Update()
        {
            Simulate(Time.deltaTime);
        }

        #endregion
    }
}
