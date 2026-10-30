using UnityEngine;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 实体动画能力组件，控制实体的动画播放
    /// </summary>
    public class AnimationModule : EntityModuleBase
    {
        private Animator _animator;

        private int _speedHash;
        private int _turnHash;
        
        #region 属性
        public override ModuleType ModuleType => ModuleType.Animation;
        #endregion

        /// <summary>
        /// 绑定后解析动画组件并缓存参数标识。
        /// </summary>
        protected override void OnBound(EntityCharacter target)
        {
            _animator = GetComponent<Animator>();

            _speedHash = Animator.StringToHash("Speed");
            _turnHash = Animator.StringToHash("Turn");
        }

        private void Update()
        {
            _animator.SetFloat(_speedHash, Context.ActualPlanarSpeed);
        }

        private void OnAnimatorMove()
        {

        }
    }
}
