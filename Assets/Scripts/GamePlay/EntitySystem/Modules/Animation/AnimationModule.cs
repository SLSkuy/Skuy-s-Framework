using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体动画能力模块，通过实体上下文读取动画参数来源。
    /// </summary>
    public class AnimationModule : EntityModuleBase
    {
        private Animator _animator;
        private EntityContext _context;

        private float _currentSpeed;
        private float _speedVelocity;
        private int _speedHash;

        #region 动画能力
        /// <summary>
        /// 初始化动画模块上下文。
        /// </summary>
        public void Init(EntityContext context)
        {
            _context = context;
            _animator = context.Animator;
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

            float smoothTime = 0.1f;
            float locomotionSpeed = 0f;
            if (_context != null)
            {
                smoothTime = _context.Config ? _context.Config.animSpeedSmoothTime : smoothTime;
                locomotionSpeed = _context.locomotionSpeed;
            }
            else if (Target is BaseEntity entity && entity.TryGetConfig(out EntityConfig config) && config != null)
            {
                smoothTime = config.animSpeedSmoothTime;
                locomotionSpeed = Target.LocomotionSpeed;
            }

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
