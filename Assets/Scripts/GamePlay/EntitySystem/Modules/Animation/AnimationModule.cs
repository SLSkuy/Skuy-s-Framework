using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体动画能力模块，通过实体上下文读取动画参数来源。
    /// </summary>
    public class AnimationModule : EntityModuleBase
    {
        private EntityConfig _config;
        private Animator _animator;

        private float _currentSpeed;
        private float _speedVelocity;
        private int _speedHash;

        #region 属性
        public Vector3 DeltaPosition => _animator.deltaPosition;
        #endregion

        #region 动画能力
        /// <summary>
        /// 初始化动画模块上下文。
        /// </summary>
        public void Init(EntityConfig config, Animator animator)
        {
            _config = config;
            _animator = animator;
        }

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
            if (!IsEnabled || !_animator) return;

            float smoothTime = _config.animSpeedSmoothTime;
            float locomotionSpeed = Target.LocomotionSpeed;

            _currentSpeed = Mathf.SmoothDamp(_currentSpeed, locomotionSpeed,
                ref _speedVelocity, smoothTime, Mathf.Infinity, deltaTime);

            _animator.SetFloat(_speedHash, _currentSpeed);
        }
        #endregion

        #region 生命周期
        private void Awake()
        {
            _speedHash = Animator.StringToHash("Speed");
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }
        #endregion
    }
}
