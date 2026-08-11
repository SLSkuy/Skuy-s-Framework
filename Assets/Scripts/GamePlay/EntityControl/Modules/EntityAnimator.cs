using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体动画控制器
    /// Speed 参数由外部注入：来源为 EntityContext.locomotionSpeed。
    /// 为避免速度档位切换时动画跳变，当前速度字段对目标速度做插值过渡，使动画显示效果更均匀。
    /// </summary>
    public class EntityAnimator : MonoBehaviour
    {
        private Animator _animator;
        private EntityContext _context;

        private float _currentSpeed;
        private float _speedVelocity;   // SmoothDamp 内部速度
        private int _speedHash;

        /// <summary>
        /// 由 EntityCharacter 在装配上下文后注入，提供 locomotionSpeed 等数据来源。
        /// </summary>
        public void Init(EntityContext context)
        {
            _context = context;
            _animator = context.Animator;
        }

        private void SetAnimationParam(float deltaTime)
        {
            if (_context == null || !_animator) return;

            // 对目标速度做插值过渡，避免档位切换时的跳变
            float smoothTime = _context.Config ? _context.Config.animSpeedSmoothTime : 0.1f;
            _currentSpeed = Mathf.SmoothDamp(_currentSpeed, _context.locomotionSpeed,
                ref _speedVelocity, smoothTime, Mathf.Infinity, deltaTime);

            _animator.SetFloat(_speedHash, _currentSpeed);
        }

        #region 生命周期

        private void Awake()
        {
            _speedHash = Animator.StringToHash("Speed");
        }

        private void Update()
        {
            SetAnimationParam(Time.deltaTime);
        }

        #endregion
    }
}
