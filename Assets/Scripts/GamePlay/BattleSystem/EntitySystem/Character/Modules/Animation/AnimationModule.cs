using UnityEngine;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 实体动画能力组件，控制实体的动画播放
    /// </summary>
    public class AnimationModule : EntityModuleBase
    {
        private MovementModule _movement;
        private ViewModule _view;
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
            _movement = Context.Get<MovementModule>();
            _view = Context.Get<ViewModule>();
            _animator = GetComponent<Animator>();

            _speedHash = Animator.StringToHash("Speed");
            _turnHash = Animator.StringToHash("Turn");
        }

        private void Update()
        {
            _animator.SetFloat(_speedHash, Context.LocomotionSpeed);
        }

        private void OnAnimatorMove()
        {
            return;
            
            EntityContext context = Context;
            _movement.Move(context.LastMoveInput, _animator.velocity.magnitude, _view.Yaw, Time.deltaTime);
        }
    }
}
