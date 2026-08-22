using System;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 基础实体对象，提供实体最基础的状态功能
    /// </summary>
    public abstract class EntityObject<TConfig, TContext> : MonoBehaviour
        where TConfig : class
        where TContext : EntityObjectContext<TConfig>
    {
        protected TConfig _config;
        protected TContext _context;
        private bool _isInitialized;

        #region 属性
        public TConfig Config => _config;
        public TContext Context => _context;
        public bool IsInitialized => _isInitialized;
        #endregion

        /// <summary>
        /// 初始化实体入口
        /// </summary>
        public virtual void Init()
        {
            if (_isInitialized) throw new InvalidOperationException($"实体 {name} 已初始化。 ");
            InitConfig();
            InitComponents();
            InitContext();
            if (_config == null) throw new InvalidOperationException($"实体 {name} 未提供有效配置。 ");
            if (_context == null) throw new InvalidOperationException($"实体 {name} 未创建有效上下文。 ");
            _isInitialized = true;
        }

        /// <summary>
        /// 初始化对象属性配置，由子类决定如何获取
        /// </summary>
        protected abstract void InitConfig();

        /// <summary>
        /// 初始化对象上下文
        /// </summary>
        protected abstract void InitContext();

        /// <summary>
        /// 初始化实体依赖的组件。具体实体可在这里装配模块并创建上下文。
        /// </summary>
        protected abstract void InitComponents();

        /// <summary>
        /// 设置实体运行时上下文。只能在初始化阶段调用。
        /// </summary>
        protected void SetContext(TContext context)
        {
            if (_isInitialized) throw new InvalidOperationException($"实体 {name} 初始化完成后不能修改上下文。 ");
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 清理实体运行时上下文。
        /// </summary>
        protected void ClearContext()
        {
            _context = null;
            _isInitialized = false;
        }
    }
}
