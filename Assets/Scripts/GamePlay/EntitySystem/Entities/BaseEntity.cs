using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体基础抽象类，负责实体通用数据与控制目标边界。
    /// </summary>
    public abstract class BaseEntity : MonoBehaviour, IEntityIntentReceiver, IEntitySimulation, IEntityStateView
    {
        protected NetEntitySyncRoot _syncRoot;
        protected EntityConfig _config;
        protected EntityContext _context;

        #region 属性
        public uint EntityId => _syncRoot != null ? _syncRoot.EntityId : 0;
        public bool HasIdentity => _syncRoot != null && _syncRoot.IsInitialized;
        public bool IsInitialized => _context != null;

        public abstract bool TickDrive { get; set; }
        public abstract uint CurrentState { get; }
        #endregion

        #region 实体数据
        /// <summary>
        /// 尝试获取实体配置。
        /// </summary>
        public bool TryGetConfig(out EntityConfig config)
        {
            config = _config;
            return config != null;
        }

        /// <summary>
        /// 尝试获取实体运行上下文。
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

        public virtual void Init()
        {
            InitConfig();
            InitComponents();
        }
        
        public abstract void Simulate(float deltaTime);
        
        #endregion
        
        /// <summary>
        /// 缓存实体通用组件与默认子节点。
        /// </summary>
        protected virtual void InitComponents()
        {
            if (!_syncRoot) _syncRoot = GetComponent<NetEntitySyncRoot>();
        }

        /// <summary>
        /// 加载实体默认配置。
        /// </summary>
        protected virtual void InitConfig()
        {
            if (_config == null) _config = EntityConfig.Instance;
        }
    }
}
