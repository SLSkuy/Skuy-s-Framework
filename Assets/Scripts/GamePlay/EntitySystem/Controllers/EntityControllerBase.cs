using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体控制器基础类，统一维护控制源与实体目标的绑定关系。
    /// </summary>
    public abstract class EntityControllerBase : MonoBehaviour, IEntityController
    {
        private EntitySimulationObject _target;

        #region 属性
        public EntitySimulationObject Target => _target;
        public bool HasTarget => _target != null;
        public abstract EntityDriveMode DriveMode { get; }
        #endregion

        #region 控制器绑定
        /// <summary>
        /// 绑定实体目标。
        /// </summary>
        public virtual void Bind(EntitySimulationObject target)
        {
            _target = target;
            OnBound(target);
        }

        /// <summary>
        /// 解绑实体目标。
        /// </summary>
        public virtual void Unbind()
        {
            EntitySimulationObject oldTarget = _target;
            _target = null;
            OnUnbound(oldTarget);
        }

        /// <summary>
        /// 绑定完成回调。
        /// </summary>
        protected virtual void OnBound(EntitySimulationObject target) { }

        /// <summary>
        /// 解绑完成回调。
        /// </summary>
        protected virtual void OnUnbound(EntitySimulationObject oldTarget) { }
        #endregion
    }
}

