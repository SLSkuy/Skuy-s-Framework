using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体基础抽象类，负责实体通用组件缓存与控制目标边界。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public abstract class BaseEntity : MonoBehaviour, IEntityIntentReceiver, IEntitySimulation, IEntityStateView
    {
        protected NetEntityIdentity _identity;
        protected EntityConfig _config;
        protected EntityContext _context;
        protected CharacterController _characterController;
        protected Animator _animator;
        protected Transform _orientation;
        protected Transform _mesh;

        #region 属性
        public uint EntityId => _identity != null ? _identity.EntityId : 0;
        public bool HasIdentity => _identity != null && _identity.IsInitialized;
        public bool IsInitialized => _context != null;

        public abstract bool TickDrive { get; set; }
        public abstract uint CurrentState { get; }
        public abstract float LocomotionSpeed { get; }
        #endregion

        #region 实体数据
        /// <summary>
        /// 尝试获取实体配置。
        /// </summary>
        public bool TryGetConfig(out EntityConfig config)
        {
            config = _config;
            return config;
        }

        /// <summary>
        /// 尝试获取实体运行上下文，兼容旧状态机与动画路径。
        /// </summary>
        public bool TryGetContext(out EntityContext context)
        {
            context = _context;
            return context != null;
        }

        /// <summary>
        /// 设置实体运行上下文。
        /// </summary>
        protected void SetContext(EntityContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 清理实体运行上下文。
        /// </summary>
        protected void ClearContext()
        {
            _context = null;
        }
        #endregion

        #region 实体控制
        public abstract void Move(Vector2 dir);
        public abstract void Aim(Vector2 dir);
        public abstract void Jump();
        public abstract void StartSprint();
        public abstract void StopSprint();
        public abstract void ToggleRun();
        #endregion

        #region 模拟入口
        public abstract void Simulate(float deltaTime);
        #endregion

        #region 生命周期
        /// <summary>
        /// 缓存实体通用组件与默认子节点。
        /// </summary>
        protected virtual void CacheCommonComponents()
        {
            if (_characterController == null) _characterController = GetComponent<CharacterController>();
            if (_animator == null) _animator = GetComponent<Animator>();
            if (_identity == null) _identity = GetComponent<NetEntityIdentity>();
            if (_orientation == null) _orientation = transform.Find("orientation");
            if (_mesh == null) _mesh = transform.Find("mesh");
        }

        /// <summary>
        /// 加载实体默认配置。
        /// </summary>
        protected virtual void LoadDefaultConfig()
        {
            if (_config == null) _config = EntityConfig.Instance;
        }

        /// <summary>
        /// 初始化基础实体数据，派生类可在装配自身逻辑前调用。
        /// </summary>
        protected virtual void InitBaseEntity()
        {
            CacheCommonComponents();
            LoadDefaultConfig();
        }
        #endregion
    }
}
