using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体能力模块基础类，统一处理实体绑定与启用状态。
    /// </summary>
    public abstract class EntityModuleBase : MonoBehaviour, IEntityModule
    {
        private IEntityControlTarget _target;
        private bool _isEnabled = true;

        #region 属性
        public IEntityControlTarget Target => _target;
        public bool HasTarget => _target != null;
        public bool IsEnabled => _isEnabled;
        #endregion

        #region 模块绑定
        /// <summary>
        /// 绑定实体目标。
        /// </summary>
        public virtual void Bind(IEntityControlTarget target)
        {
            _target = target;
            OnBound(target);
        }

        /// <summary>
        /// 设置启用状态。
        /// </summary>
        public virtual void SetEnabled(bool isEnabled)
        {
            if (_isEnabled == isEnabled) return;
            _isEnabled = isEnabled;
            OnEnabledChanged(isEnabled);
        }

        /// <summary>
        /// 解绑实体目标。
        /// </summary>
        public virtual void Unbind()
        {
            IEntityControlTarget oldTarget = _target;
            _target = null;
            OnUnbound(oldTarget);
        }

        /// <summary>
        /// 绑定完成回调。
        /// </summary>
        protected virtual void OnBound(IEntityControlTarget target) { }

        /// <summary>
        /// 启用状态变化回调。
        /// </summary>
        protected virtual void OnEnabledChanged(bool isEnabled) { }

        /// <summary>
        /// 解绑完成回调。
        /// </summary>
        protected virtual void OnUnbound(IEntityControlTarget oldTarget) { }
        #endregion
    }
}

