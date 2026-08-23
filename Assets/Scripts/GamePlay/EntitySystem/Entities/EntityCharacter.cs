using System;
using UnityEngine;
using Utils;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 场景角色实体：装配模块与状态机，并托管固定 Tick 模拟。
    /// </summary>
    public class EntityCharacter : MonoBehaviour
    {
        protected EntityConfig _config;
        protected EntityContext _context;
        private IEntityObjectIdentity _identity;
        private MovementModule _movementModule;
        private RotationModule _rotationModule;
        protected EntitySimulation _simulation;
        private bool _isInitialized;

        #region 属性
        public EntityConfig Config => _config;
        public EntityContext Context => _context;
        public bool IsInitialized => _isInitialized;
        public uint EntityId => _identity != null ? _identity.EntityId : 0;
        public uint CurrentState => _context?.StateMachine.CurrentState ?? EntityState.IDLE;
        #endregion

        /// <summary>
        /// 初始化实体入口。
        /// </summary>
        public void Init()
        {
            if (_isInitialized) throw new InvalidOperationException($"实体 {name} 已初始化。 ");
            _config ??= EntityConfig.Instance;
            _identity = GetComponent<IEntityObjectIdentity>();

            _movementModule = gameObject.GetOrAddComponent<MovementModule>();
            _movementModule.Init(_config);
            _movementModule.Bind(this);

            _rotationModule = gameObject.GetOrAddComponent<RotationModule>();
            _rotationModule.Init(_config);
            _rotationModule.Bind(this);

            _context = new EntityContext(_config, _movementModule, _rotationModule);
            RegisterStates();
            _context.StateMachine.ChangeState(EntityState.IDLE);
            _simulation = new EntitySimulation(_context);
            if (_config == null) throw new InvalidOperationException($"实体 {name} 未提供有效配置。 ");
            _isInitialized = true;
        }

        /// <summary>
        /// 使用完整命令推进一次固定 Tick 模拟。
        /// </summary>
        public void Step(uint tick, float deltaTime, in EntityInputCommand command)
        {
            _simulation?.Step(tick, deltaTime, command);
        }

        public EntitySimulationState CaptureSimulationState()
        {
            return _simulation != null ? _simulation.CaptureSimulationState() : default;
        }

        public EntityRollbackState CaptureRollbackState()
        {
            return _simulation != null ? _simulation.CaptureRollbackState() : default;
        }

        public void RestoreRollbackState(in EntityRollbackState state)
        {
            _simulation?.RestoreRollbackState(state);
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
    }
}
