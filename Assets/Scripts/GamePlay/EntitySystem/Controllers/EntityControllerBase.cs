using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体控制器基础类，统一维护控制源与实体目标的绑定关系。
    /// </summary>
    public abstract class EntityControllerBase : MonoBehaviour
    {
        private EntityCharacter _target;

        #region 属性
        public EntityCharacter Target => _target;
        public bool HasTarget => _target != null;
        #endregion

        public virtual void Bind(EntityCharacter target)
        {
            _target = target;
            OnBound(target);
        }

        public virtual void Unbind()
        {
            EntityCharacter oldTarget = _target;
            _target = null;
            OnUnbound(oldTarget);
        }

        protected virtual void OnBound(EntityCharacter target) { }
        protected virtual void OnUnbound(EntityCharacter oldTarget) { }
    }
}
