using Framework;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 移动控制子模块，用于处理实体的移动逻辑
    /// </summary>
    public class MovementModule
    {
        // 组件引用
        private CharacterController _characterController;
        private EntityConfig _config;
        private Transform _transform;
        
        private float _locomotionMultiplier;
        private bool _isLockInput;
        private bool _isKnockback;
        private Vector2 _lastMoveDir;

        public void Init(CharacterController controller, EntityConfig config, Transform entityTransform)
        {
            _characterController = controller;
            _config = config;
            _transform = entityTransform;

            _locomotionMultiplier = _config.walkSpeed;
            _isLockInput = false;
        }

        public virtual void Move(Vector2 dir)
        {
            if (!_isLockInput) _lastMoveDir = dir;

            Vector3 moveDir = new Vector3(_lastMoveDir.x, 0f, _lastMoveDir.y) * _locomotionMultiplier;
            _characterController.SimpleMove(moveDir);
        }

        public virtual void StartSprint()
        {
            if (_isLockInput) return;
            _locomotionMultiplier = _config.sprintSpeed;
        }

        public virtual void StopSprint()
        {
            if (_isLockInput) return;
            _locomotionMultiplier = _config.walkSpeed;
        }

        public virtual void Dash()
        {
            if (_isLockInput) return;

            _isLockInput = true;
            _locomotionMultiplier = _config.dashSpeed;

            // 启动计时操作
            Global.Get<TimerManager>().CreateAndStart(_config.dashDuration, OnDashComplete);
        }

        protected virtual void OnDashComplete()
        {
            _isLockInput = false;
            _locomotionMultiplier = _config.walkSpeed;
        }

        public virtual void Rotate(Vector3 direction)
        {
            direction.y = 0f;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                _transform.rotation = targetRotation;
            }
        }
    }
}