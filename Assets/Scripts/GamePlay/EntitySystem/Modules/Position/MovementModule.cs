using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体移动能力模块，封装位移、朝向、重力、跳跃和冲刺等底层运动逻辑。
    /// 不持有状态机引用，不做状态切换决策，决策权由状态层负责。
    /// </summary>
    public class MovementModule : EntityModuleBase
    {
        private CharacterController _controller;
        private EntityConfig _config;
        private Transform _orientation;
        private Transform _mesh;

        private float _yaw;
        private float _pitch;

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
        public bool IsGrounded => _controller != null && _controller.isGrounded;
        public bool IsDashing => _isDashing;
        public int JumpCount => _jumpCount;
        #endregion

        #region 初始化
        /// <summary>
        /// 初始化移动模块运行时依赖。
        /// </summary>
        public void Init(EntityConfig config, Transform orientation, Transform mesh)
        {
            _config = config;
            _orientation = orientation;
            _mesh = mesh;
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
                CapsuleCollider collider = gameObject.AddComponent<CapsuleCollider>();
                collider.height = _config.height;
                collider.radius = _config.radius;
                collider.center = new Vector3(0, _config.height, 0);
            }
            else
            {
                _controller = gameObject.AddComponent<CharacterController>();
                _controller.height = _config.height;
                _controller.radius = _config.radius;
                _controller.center = new Vector3(0, _config.height, 0);
                
                Debug.LogWarning(_controller);
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
        
        #endregion

        #region 移动能力
        
        /// <summary>
        /// 位移：以模型朝向作为移动方向基准，并应用重力与冲刺计时器推进。
        /// </summary>
        public void Move(Vector2 inputDir, float speed, float dt, bool isFocus)
        {
            // 快照模式，不进行模拟
            if (!_controller) return;
            
            if (!_isDashing)
            {
                _locomotionSpeed = speed;
                UpdateLocomotionDir(inputDir, isFocus);
            }

            ApplyGravity(dt);
            ApplyMovement(dt);
            TickDash(dt);
        }

        /// <summary>
        /// 动画驱动位移：以 root motion 的水平速率作为移动速度，沿 mesh 当前朝向施加。
        /// </summary>
        public void ApplyRootMotion(Vector3 deltaPosition, float dt)
        {
            ApplyGravity(dt);

            Vector3 horizontal = Vector3.ProjectOnPlane(deltaPosition, Vector3.up);
            Vector3 meshForward = Vector3.ProjectOnPlane(_mesh.forward, Vector3.up);
            if (meshForward.sqrMagnitude > Mathf.Epsilon) meshForward.Normalize();

            Vector3 motion = meshForward * horizontal.magnitude;
            motion.y = _verticalVelocity * dt;
            _controller.Move(motion);
        }

        /// <summary>
        /// 视角即时转换：更新 orientation 的 pitch/yaw。
        /// </summary>
        public void Rotate(Vector2 aimInput, float dt)
        {
            if (aimInput.sqrMagnitude < Mathf.Epsilon) return;

            _yaw += aimInput.x * _config.aimHorizontalSpeed * dt;
            _pitch -= aimInput.y * _config.aimVerticalSpeed * dt;
            _pitch = Mathf.Clamp(_pitch, _config.minAimPitch, _config.maxAimPitch);

            _orientation.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        /// <summary>
        /// 更新模型朝向。
        /// </summary>
        public void UpdateMeshFacing(Vector2 moveInput, bool isFocus, float dt)
        {
            if (isFocus)
            {
                _mesh.rotation = Quaternion.Euler(0f, _yaw, 0f);
                return;
            }

            if (moveInput.sqrMagnitude < Mathf.Epsilon) return;

            Vector3 forward = _orientation.forward; forward.y = 0f; forward.Normalize();
            Vector3 right = _orientation.right; right.y = 0f; right.Normalize();
            Vector3 desiredDir = forward * moveInput.y + right * moveInput.x;
            if (desiredDir.sqrMagnitude < Mathf.Epsilon) return;

            Quaternion targetRot = Quaternion.LookRotation(desiredDir, Vector3.up);
            _mesh.rotation = Quaternion.RotateTowards(_mesh.rotation, targetRot, _config.meshTurnSpeed * dt);
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

        /// <summary>
        /// 开始冲刺。
        /// </summary>
        public void StartDash(Vector2 inputDir, float speed, float duration)
        {
            Vector3 forward = _orientation.forward; forward.y = 0f; forward.Normalize();
            Vector3 right = _orientation.right; right.y = 0f; right.Normalize();
            Vector3 dashDir = forward * inputDir.y + right * inputDir.x;
            if (dashDir.sqrMagnitude < Mathf.Epsilon) dashDir = forward;

            _dashDir = Vector3.ClampMagnitude(dashDir, 1f);
            _isDashing = true;
            _locomotionSpeed = speed;
            _dashAccumulator = duration;
        }

        private void UpdateLocomotionDir(Vector2 inputDir, bool isFocus)
        {
            Vector3 forward = _mesh.forward; forward.y = 0f; forward.Normalize();

            if (isFocus)
            {
                Vector3 right = _mesh.right; right.y = 0f; right.Normalize();
                _lastMoveDir = forward * inputDir.y + right * inputDir.x;
            }
            else
            {
                _lastMoveDir = forward * inputDir.magnitude;
            }

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
