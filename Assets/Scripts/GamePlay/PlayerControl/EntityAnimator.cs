using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体动画控制器
    /// </summary>
    public class EntityAnimator : MonoBehaviour
    {
        private Animator _animator;
        
        private float _currentSpeed;

        private int _speedHash;

        private void SetAnimationParam()
        {
            _animator.SetFloat(_speedHash, _currentSpeed);
        }

        #region 生命周期

        private void Awake()
        {
            _speedHash = Animator.StringToHash("Speed");
        }

        private void Start()
        {
            _animator = GetComponent<Animator>();
        }

        private void Update()
        {
            SetAnimationParam();
        }

        #endregion
    }
}