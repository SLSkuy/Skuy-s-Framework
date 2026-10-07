using Framework;
using GamePlay.DataProxy;
using UnityEngine;
using Utils;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 场景角色实体：装配模块与状态机，并托管固定 Tick 模拟
    /// </summary>
    public class EntityCharacter : MonoBehaviour, IPoolable
    {
        [SerializeField] private EntityConfig config;
        
        private EntityContext _context;
        private bool _isInitialized;
        
        // 能力组件
        private EntityVisualPresentation _visualPresentation;
        private EntitySimulation _simulation;
        private MovementModule _movementModule;
        private ViewModule _viewModule;

        #region 属性
        public EntityContext Context => _context;
        public bool IsInitialized => _isInitialized;
        public EntityState CurrentState => _context?.StateMachine.CurrentState ?? EntityState.IDLE;
        #endregion

        /// <summary>
        /// 初始化实体入口。
        /// </summary>
        public void Init()
        {
            if (_isInitialized) return;

            InitConfig();
            InitCapacityModule();
            InitSimulationContext();
            RegisterStates();
            
            _isInitialized = true;
        }

        protected virtual void InitConfig()
        {
            if (config) return;
            
            config = Global.GetDataProxy<EntityConfigProxy>().Config;
        }
        
        /// <summary>
        /// 初始化能力组件
        /// </summary>
        protected virtual void InitCapacityModule()
        {
            _movementModule = gameObject.GetOrAddComponent<MovementModule>();
            _movementModule.Init(config);
            _movementModule.Bind(this);

            _viewModule = gameObject.GetOrAddComponent<ViewModule>();
            _viewModule.Init(config);
            _viewModule.Bind(this);

            _visualPresentation = gameObject.GetOrAddComponent<EntityVisualPresentation>();
            _visualPresentation.Init();
        }
        
        /// <summary>
        /// 初始化模拟上下文
        /// </summary>
        protected virtual void InitSimulationContext()
        {
            _context = new EntityContext(config, _movementModule, _viewModule);
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
            _context.StateMachine.ChangeState(EntityState.IDLE);
        }
        
        #region 模拟入口

        /// <summary>
        /// 使用完整命令推进一次固定 Tick 模拟
        /// </summary>
        public void Step(uint tick, float deltaTime, in EntityCommand command)
        {
            _simulation.Step(tick, deltaTime, command);
        }

        /// <summary>
        /// 步进前归位画面节点与权威旋转。
        /// </summary>
        public void PrepareSimulationTick()
        {
            _visualPresentation.PrepareSimulation();
        }

        /// <summary>
        /// 步进后收录权威位姿。
        /// </summary>
        public void CaptureAuthorityAfterTick()
        {
            _visualPresentation.CaptureAuthority();
        }

        /// <summary>
        /// 按残差写出画面位姿。
        /// </summary>
        public void PresentVisualPose(float alpha)
        {
            _visualPresentation.Present(alpha);
        }

        /// <summary>
        /// 按远端插值结果写出画面位姿，不改权威拍缓存。
        /// </summary>
        public void PresentReplicaPose(Vector3 rootPosition, Quaternion meshRotation, Quaternion viewRotation)
        {
            _visualPresentation.PresentPose(rootPosition, meshRotation, viewRotation);
        }

        /// <summary>
        /// 获取快照状态
        /// </summary>
        public EntitySnapshot CaptureSnapshot()
        {
            return _simulation.CaptureSnapshot();
        }

        /// <summary>
        /// 写回模拟状态，不改画面采样
        /// </summary>
        public void RestoreSimulation(in EntitySnapshot state)
        {
            _simulation.RestoreSnapshot(state);
        }

        /// <summary>
        /// 写回模拟状态并让画面立刻对齐。
        /// </summary>
        public void RestoreSnapshot(in EntitySnapshot state)
        {
            RestoreSimulation(state);
            _visualPresentation.SnapToAuthority();
        }

        #endregion
        
        /// <summary>
        /// 还池时清掉上一场的运动、视角和状态。
        /// </summary>
        void IPoolable.Reset()
        {
            if (!_isInitialized) return;

            _context.LastMoveInput = Vector2.zero;
            _context.LastAimInput = Vector2.zero;
            _context.CurrentTick = 0;
            _context.RunToggleRequest = false;
            _context.JumpRequest = false;
            _context.IsSprinting = false;
            _context.IsRunning = false;
            _context.IsFocus = false;

            _movementModule.ResetMotion();
            _movementModule.Restore(Quaternion.identity, Vector3.zero);
            _viewModule.Restore(Quaternion.identity, Vector3.zero);
            _context.StateMachine.ChangeState(EntityState.IDLE);
            _visualPresentation.SnapToAuthority();
        }
    }
}
