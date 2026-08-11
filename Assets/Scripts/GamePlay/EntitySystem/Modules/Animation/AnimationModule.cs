using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 动画能力模块，通过实体状态视图读取动画参数来源。
    /// </summary>
    public class AnimationModule : EntityModuleBase
    {
        private Animator _animator;
        private int _speedHash;
        private float _currentSpeed;
        private float _speedVelocity;

        #region 动画能力
        /// <summary>
        /// 绑定 Animator。
        /// </summary>
        public void BindAnimator(Animator animator)
        {
            _animator = animator;
        }

        /// <summary>
        /// 更新动画参数。
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!IsEnabled || Target == null || !_animator) return;

            float smoothTime = 0.1f;
            if (Target is BaseEntity entity && entity.TryGetConfig(out EntityConfig config) && config != null)
            {
                smoothTime = config.animSpeedSmoothTime;
            }

            _currentSpeed = Mathf.SmoothDamp(_currentSpeed, Target.LocomotionSpeed,
                ref _speedVelocity, smoothTime, Mathf.Infinity, deltaTime);

            _animator.SetFloat(_speedHash, _currentSpeed);
        }
        #endregion

        #region 生命周期
        private void Awake()
        {
            _speedHash = Animator.StringToHash("Speed");
        }
        #endregion
    }
}
