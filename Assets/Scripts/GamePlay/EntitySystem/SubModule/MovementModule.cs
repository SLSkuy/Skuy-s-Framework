using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 移动控制子模块，用于处理实体的移动逻辑
    /// </summary>
    public class MovementModule
    {
        private CharacterController _characterController;
        private EntityConfig _config;
        private Transform _transform;

        private float _locomotionMultiplier;
        private float _dashAccumulator;
        private Vector2 _lastMoveDir;
        private bool _isLockInput;
        
        /// <summary>
        /// 垂直速度
        /// </summary>
        private float _verticalVelocity;

        public void Init(CharacterController controller, EntityConfig config, Transform entityTransform)
        {
            _characterController = controller;
            _config = config;
            _transform = entityTransform;
            _locomotionMultiplier = _config.walkSpeed;
        }

        public void Move(Vector2 dir, float deltaTime)
        {
            if (!_isLockInput)
            {
                _lastMoveDir = dir;
            }

            if (_isLockInput)
            {
                _dashAccumulator -= deltaTime;
                if (_dashAccumulator <= 0f)
                {
                    OnDashComplete();
                }
            }

            // 重力计算
            if (_characterController.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -1f; // 防止奇怪的抽动，添加一个默认的向下速度
            else _verticalVelocity -= _config.gravity * deltaTime;
            
            // 限制最大掉落速度
            if(_verticalVelocity < -_config.maxFallSpeed) _verticalVelocity = -_config.maxFallSpeed;

            Vector3 velocity = new Vector3(_lastMoveDir.x, 0f, _lastMoveDir.y) * _locomotionMultiplier;
            velocity.y = _verticalVelocity;
            _characterController.Move(velocity * deltaTime);
        }

        public void StartSprint()
        {
            if (_isLockInput) return;
            
            _locomotionMultiplier = _config.sprintSpeed;
        }

        public void StopSprint()
        {
            if (_isLockInput) return;
            
            _locomotionMultiplier = _config.walkSpeed;
        }

        public void Dash()
        {
            if (_isLockInput) return;

            _isLockInput = true;
            _locomotionMultiplier = _config.dashSpeed;
            _dashAccumulator = _config.dashDuration;
        }

        public void Rotate(Vector3 direction)
        {
            direction.y = 0f;
            if (direction != Vector3.zero)
            {
                _transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        private void OnDashComplete()
        {
            _isLockInput = false;
            _locomotionMultiplier = _config.walkSpeed;
        }
    }
}
