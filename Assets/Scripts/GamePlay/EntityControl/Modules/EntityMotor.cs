using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体运动执行器，封装位移/旋转/重力/冲刺等底层运动逻辑。
    /// 不持有状态机引用，不做任何状态切换决策——决策权完全在状态。
    /// </summary>
    public class EntityMotor
    {
        private readonly CharacterController _controller;
        private readonly EntityConfig _config;
        private readonly Transform _orientation; // 视角变换
        private readonly Transform _mesh;        // 模型变换

        // 朝向控制
        private float _yaw;
        private float _pitch;

        // 位移控制
        private Vector3 _lastMoveDir;
        private float _locomotionSpeed;
        private float _verticalVelocity;

        // 跳跃控制
        private int _jumpCount;

        // 冲刺控制
        private Vector3 _dashDir;
        private float _dashAccumulator;
        private bool _isDashing;
        
        #region 状态属性
        public bool IsDashing => _isDashing;
        public int JumpCount => _jumpCount;
        #endregion

        public EntityMotor(CharacterController controller, EntityConfig config, Transform orientation, Transform mesh)
        {
            _controller = controller;
            _orientation = orientation;
            _mesh = mesh;
            _config = config;
            _locomotionSpeed = _config.walkSpeed;
        }

        /// <summary>
        /// 位移：以模型朝向作为移动方向基准，按速度档位移动，并应用重力与冲刺计时器推进。
        /// 冲刺期间方向锁定，speed 参数不生效。
        /// </summary>
        public void Move(Vector2 inputDir, float speed, float dt, bool isFocus)
        {
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
        /// 动画驱动位移：以 root motion 的水平速率作为移动速度，沿 mesh 当前朝向施加，
        /// 使位移跟随模型转向（root 不随转向旋转，直接用 deltaPosition 方向只会朝固定方向移动）。
        /// 垂直位移由重力/跳跃物理驱动。由 EntityCharacter.OnAnimatorMove 调用，仅当 config.rootMotion 开启时使用。
        /// </summary>
        public void ApplyRootMotion(Vector3 deltaPosition, float dt)
        {
            ApplyGravity(dt);

            // 取 root motion 的水平速率，沿 mesh 当前朝向施加，位移即可随模型转向
            Vector3 horizontal = Vector3.ProjectOnPlane(deltaPosition, Vector3.up);
            Vector3 meshForward = Vector3.ProjectOnPlane(_mesh.forward, Vector3.up);
            if (meshForward.sqrMagnitude > Mathf.Epsilon) meshForward.Normalize();

            Vector3 motion = meshForward * horizontal.magnitude;
            motion.y = _verticalVelocity * dt;
            _controller.Move(motion);
        }

        /// <summary>
        /// 视角即刻转换：更新 orientation 的 pitch/yaw。
        /// 模型朝向不再在此处理，统一交由 UpdateMeshFacing 按 IsFocus 决策。
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
        /// 更新模型朝向：
        /// 锁定状态(IsFocus)：模型时刻与 orientation 的 yaw 一致。
        /// 非锁定状态：模型以固定角速度缓慢转向当前移动方向（而非 orientation 方向）。
        /// </summary>
        public void UpdateMeshFacing(Vector2 moveInput, bool isFocus, float dt)
        {
            if (isFocus)
            {
                _mesh.rotation = Quaternion.Euler(0f, _yaw, 0f);
                return;
            }

            // 非锁定：无移动输入时保持当前朝向
            if (moveInput.sqrMagnitude < Mathf.Epsilon) return;

            Vector3 forward = _orientation.forward; forward.y = 0f; forward.Normalize();
            Vector3 right = _orientation.right; right.y = 0f; right.Normalize();
            Vector3 desiredDir = forward * moveInput.y + right * moveInput.x;
            if (desiredDir.sqrMagnitude < Mathf.Epsilon) return;

            Quaternion targetRot = Quaternion.LookRotation(desiredDir, Vector3.up);
            _mesh.rotation = Quaternion.RotateTowards(_mesh.rotation, targetRot, _config.meshTurnSpeed * dt);
        }

        /// <summary>
        /// 尝试跳跃，成功则设置垂直速度并累加跳跃次数。
        /// 冲刺中或达到跳跃次数上限时失败。
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
        /// 开始冲刺：根据输入方向计算冲刺方向（无输入时按当前朝向）。
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
            // 以模型朝向作为移动方向基准，配合模型转动效果更自然
            Vector3 forward = _mesh.forward; forward.y = 0f; forward.Normalize();

            if (isFocus)
            {
                // 锁定：以 mesh 朝向为基准做完整输入映射，允许侧移/后退
                Vector3 right = _mesh.right; right.y = 0f; right.Normalize();
                _lastMoveDir = forward * inputDir.y + right * inputDir.x;
            }
            else
            {
                // 非锁定：以 mesh 朝向为移动方向，幅度由输入决定（无侧移）
                _lastMoveDir = forward * inputDir.magnitude;
            }

            // 防止斜向移动速度加快
            _lastMoveDir = Vector3.ClampMagnitude(_lastMoveDir, 1f);
        }

        private void ApplyGravity(float dt)
        {
            if (_controller.isGrounded)
            {
                if (_verticalVelocity < 0f)
                {
                    _verticalVelocity = -1f; // 防止奇怪的抽动，添加一个默认的向下速度
                    _jumpCount = 0; // 滞空跳跃累计次数
                }
            }
            else
            {
                if (_jumpCount < 1) _jumpCount = 1; // 从边缘坠落时只允许跳跃一次
                _verticalVelocity -= _config.gravity * dt;
            }

            // 限制最大掉落速度
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
    }
}
