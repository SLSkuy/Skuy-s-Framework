using UnityEngine;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 实体能力模块基础类，统一处理实体绑定与启用状态。
    /// </summary>
    public abstract class EntityModuleBase : MonoBehaviour
    {
        #region 属性
        public EntityCharacter Target { get; private set; }
        public abstract ModuleType ModuleType { get; }
        public bool HasTarget => Target;
        public bool IsEnabled { get; private set; } = true;

        /// <summary>
        /// 实体控制数据上下文
        /// </summary>
        protected EntityContext Context => Target.Context;
        
        /// <summary>
        /// 实体配置
        /// </summary>
        protected CharacterConfig Config => Context.Config;
        #endregion

        /// <summary>
        /// 绑定已创建上下文的实体，并初始化模块自身依赖
        /// </summary>
        public void Bind(EntityCharacter target)
        {
            Target = target;
            OnBound(target);
        }

        public virtual void SetEnabled(bool isEnabled)
        {
            if (IsEnabled == isEnabled) return;
            IsEnabled = isEnabled;
            OnEnabledChanged(isEnabled);
        }

        public virtual void Unbind()
        {
            EntityCharacter oldTarget = Target;
            Target = null;
            OnUnbound(oldTarget);
        }

        /// <summary>
        /// 从上下文读取配置并初始化自身；此时其他模块可能尚未初始化，不启动模拟或状态切换
        /// </summary>
        protected virtual void OnBound(EntityCharacter target) { }
        protected virtual void OnEnabledChanged(bool isEnabled) { }
        protected virtual void OnUnbound(EntityCharacter oldTarget) { }
    }
}
