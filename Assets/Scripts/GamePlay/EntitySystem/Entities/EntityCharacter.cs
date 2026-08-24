using UnityEngine;
using Utils;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 场景角色实体：装配模块与状态机，并托管固定 Tick 模拟。
    /// </summary>
    public class EntityCharacter : MonoBehaviour
    {
        [SerializeField] 
        private EntityConfig config;
        private EntityContext _context;
        private bool _isInitialized;
        
        // 身份识别
        private IEntityObjectIdentity _identity;
        
        // 能力组件
        private EntitySimulation _simulation;
        private MovementModule _movementModule;
        private RotationModule _rotationModule;

        #region 属性
        public EntityContext Context => _context;
        public bool IsInitialized => _identity.IsInitialized;
        public uint EntityId => _identity?.EntityId ?? 0;
        public uint CurrentState => _context?.StateMachine.CurrentState ?? EntityState.IDLE;
        #endregion

        /// <summary>
        /// 初始化实体入口。
        /// </summary>
        public void Init()
        {
            if (_isInitialized) return;
            
            _identity = GetComponent<IEntityObjectIdentity>();
            
            InitConfig();
            InitSimulationContext();
            InitCapacityModule();
            RegisterStates();
            
            
            _isInitialized = true;
        }

        protected virtual void InitConfig()
        {
            if (!config) config = EntityConfig.Instance;
        }
        
        /// <summary>
        /// 初始化能力组件
        /// </summary>
        protected virtual void InitCapacityModule()
        {
            _movementModule = gameObject.GetOrAddComponent<MovementModule>();
            _movementModule.Init(config);
            _movementModule.Bind(this);

            _rotationModule = gameObject.GetOrAddComponent<RotationModule>();
            _rotationModule.Init(config);
            _rotationModule.Bind(this);
        }
        
        /// <summary>
        /// 初始化模拟上下文
        /// </summary>
        protected virtual void InitSimulationContext()
        {
            _context = new EntityContext(config, _movementModule, _rotationModule);
            _simulation = new EntitySimulation(_context);
        }
        
        /// <summary>
        /// 注册角色默认状态。子类可追加或替换角色专属状态。
        /// </summary>
        protected virtual void RegisterStates()
        {
            _context.StateMachine.RegisterState(new EntityIdleState(_context));
            _context.StateMachine.RegisterState(new EntityWalkState(_context));
            _context.StateMachine.RegisterState(new EntityRunState(_context));
            _context.StateMachine.RegisterState(new EntitySprintState(_context));
            _context.StateMachine.RegisterState(new EntityAirborneState(_context));
        }
        
        #region 模拟入口

        /// <summary>
        /// 使用完整命令推进一次固定 Tick 模拟
        /// </summary>
        public void Step(uint tick, float deltaTime, in EntityCommand command)
        {
            _simulation?.Step(tick, deltaTime, command);
        }

        /// <summary>
        /// 捕获当前状态
        /// </summary>
        /// <returns></returns>
        public EntitySimulationState CaptureSimulationState()
        {
            return _simulation?.CaptureSimulationState() ?? default;
        }

        /// <summary>
        /// 获取回退状态
        /// </summary>
        /// <returns></returns>
        public EntityRollbackState CaptureRollbackState()
        {
            return _simulation?.CaptureRollbackState() ?? default;
        }

        /// <summary>
        /// 缓存回退状态
        /// </summary>
        /// <param name="state"></param>
        public void RestoreRollbackState(in EntityRollbackState state)
        {
            _simulation?.RestoreRollbackState(state);
        }

        #endregion
    }
}
