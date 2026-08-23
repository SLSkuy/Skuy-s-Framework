using UnityEngine;
using Utils;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体位置移动能力模块，只负责位置模拟与位置同步落点。
    /// </summary>
    public class MovementModule : EntityModuleBase
    {
        private CharacterController _controller;
        private EntityConfig _config;

        private Vector3 _lastMoveDir;
        private float _locomotionSpeed;
        private float _verticalVelocity;
        private int _jumpCount;

        private Vector3 _dashDir;
        private float _dashAccumulator;
        private bool _isDashing;

        #region 状态属性
        public override ModuleType ModuleType => ModuleType.Position;
        
        public Vector3 Position => transform.position;
        public Vector3 LinearVelocity { get; private set; }
        public bool IsGrounded => _controller != null && _controller.isGrounded;
        public bool IsDashing => _isDashing;
        public int JumpCount => _jumpCount;
        #endregion

        #region 初始化
        /// <summary>
        /// 初始化移动模块运行时配置。
        /// </summary>
        public void Init(EntityConfig config)
        {
            _config = config;
            _locomotionSpeed = _config.walkSpeed;
        }
        #endregion

        #region 网络同步
        
        /// <summary>
        /// 添加碰撞体或驱动器
        /// </summary>
        public void SetReplicaMode(bool isReplica)
        {
            if (isReplica)
            {
                // 保险措施
                _controller = GetComponent<CharacterController>();
                if (_controller != null) _controller.enabled = false;
                
                CapsuleCollider collider = gameObject.GetOrAddComponent<CapsuleCollider>();
                collider.height = _config.height;
                collider.radius = _config.radius;
                collider.center = new Vector3(0, _config.height / 2, 0);
            }
            else
            {
                _controller = gameObject.GetOrAddComponent<CharacterController>();
                _controller.height = _config.height;
                _controller.radius = _config.radius;
                _controller.skinWidth = 0.0001f;
                _controller.minMoveDistance = 0f;
                _controller.center = new Vector3(0, _config.height / 2, 0);
            }
        }

        /// <summary>
        /// 直接设置实体位置，用于权威快照或插值快照应用。
        /// </summary>
        public void Teleport(Vector3 position)
        {
            bool wasEnabled = _controller && _controller.enabled;
            if (wasEnabled) _controller.enabled = false;

            transform.position = position;

            if (wasEnabled) _controller.enabled = true;
        }

        /// <summary>
        /// 捕获移动模块完整回滚状态。
        /// </summary>
        public MovementRollbackState CaptureRollbackState()
        {
            return new MovementRollbackState
            {
                LastMoveDirection = _lastMoveDir,
                LinearVelocity = LinearVelocity,
                DashDirection = _dashDir,
                LocomotionSpeed = _locomotionSpeed,
                VerticalVelocity = _verticalVelocity,
                DashRemainingTime = _dashAccumulator,
                JumpCount = _jumpCount,
                IsDashing = _isDashing
            };
        }

        /// <summary>
        /// 恢复位置和全部移动内部状态。
        /// </summary>
        public void RestoreRollbackState(Vector3 position, in MovementRollbackState state)
        {
            Teleport(position);
            _lastMoveDir = state.LastMoveDirection;
            LinearVelocity = state.LinearVelocity;
            _dashDir = state.DashDirection;
            _locomotionSpeed = state.LocomotionSpeed;
            _verticalVelocity = state.VerticalVelocity;
            _dashAccumulator = state.DashRemainingTime;
            _jumpCount = state.JumpCount;
            _isDashing = state.IsDashing;
        }
        
        #endregion

        #region 移动能力
        
        /// <summary>
        /// 位移：以模型朝向作为移动方向基准，并应用重力与冲刺计时器推进。
        /// </summary>
        public void Move(Vector2 inputDir, float speed, float dt)
        {
            // 快照模式，不进行模拟
            if (!_controller) return;
            
            if (!_isDashing)
            {
                _locomotionSpeed = speed;
                UpdateLocomotionDir(inputDir);
            }

            ApplyGravity(dt);
            ApplyMovement(dt);
            TickDash(dt);
        }

        /// <summary>
        /// 尝试跳跃，成功则设置垂直速度并累计跳跃次数。
        /// </summary>
        public bool Jump(float jumpSpeed, int maxJumpCount)
        {
            if (_isDashing) return false;
            if (_jumpCount >= maxJumpCount) return false;

            _jumpCount++;
            _verticalVelocity = jumpSpeed;
            return true;
        }

        private void UpdateLocomotionDir(Vector2 inputDir)
        {
            _lastMoveDir = new Vector3(inputDir.x, 0f, inputDir.y);
            _lastMoveDir = Vector3.ClampMagnitude(_lastMoveDir, 1f);
        }

        private void ApplyGravity(float dt)
        {
            if (_controller.isGrounded)
            {
                if (_verticalVelocity < 0f)
                {
                    _verticalVelocity = -1f;
                    _jumpCount = 0;
                }
            }
            else
            {
                if (_jumpCount < 1) _jumpCount = 1;
                _verticalVelocity -= _config.gravity * dt;
            }

            if (_verticalVelocity < -_config.maxFallSpeed)
            {
                _verticalVelocity = -_config.maxFallSpeed;
            }
        }

        private void ApplyMovement(float dt)
        {
            Vector3 moveDir = _isDashing ? _dashDir : _lastMoveDir;
            Vector3 velocity = new Vector3(moveDir.x, 0f, moveDir.z) * _locomotionSpeed;
            velocity.y = _verticalVelocity;
            LinearVelocity = velocity;
            _controller.Move(velocity * dt);
        }

        private void TickDash(float dt)
        {
            if (!_isDashing) return;

            _dashAccumulator -= dt;
            if (_dashAccumulator <= 0f)
            {
                _isDashing = false;
            }
        }
        #endregion
    }
}
