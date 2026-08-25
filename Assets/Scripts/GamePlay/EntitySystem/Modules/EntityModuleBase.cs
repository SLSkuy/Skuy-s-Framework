using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体能力模块基础类，统一处理实体绑定与启用状态。
    /// </summary>
    public abstract class EntityModuleBase<TState> : MonoBehaviour, IEntityModule, 
        IEntityStateStore<TState> where TState : struct
    {
        private EntityCharacter _target;
        private bool _isEnabled = true;

        #region 属性
        public abstract ModuleType ModuleType { get; }
        public EntityCharacter Target => _target;
        public bool HasTarget => _target;
        public bool IsEnabled => _isEnabled;
        #endregion

        public virtual void Bind(EntityCharacter target)
        {
            _target = target;
            OnBound(target);
        }

        public virtual void SetEnabled(bool isEnabled)
        {
            if (_isEnabled == isEnabled) return;
            _isEnabled = isEnabled;
            OnEnabledChanged(isEnabled);
        }

        public virtual void Unbind()
        {
            EntityCharacter oldTarget = _target;
            _target = null;
            OnUnbound(oldTarget);
        }

        public abstract TState CaptureRollbackState();
        public abstract void RestoreRollbackState(in TState state);

        protected virtual void OnBound(EntityCharacter target) { }
        protected virtual void OnEnabledChanged(bool isEnabled) { }
        protected virtual void OnUnbound(EntityCharacter oldTarget) { }
    }
}
