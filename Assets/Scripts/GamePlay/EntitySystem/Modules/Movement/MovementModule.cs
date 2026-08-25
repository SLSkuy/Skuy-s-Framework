using UnityEngine;
using Utils;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体根节点 Transform 能力：位置模拟、mesh 身体偏航与 Replica 碰撞切换
    /// 根节点旋转保持为单位四元数；身体朝向写在直接子节点 mesh 上
    /// </summary>
    public class MovementModule : EntityModuleBase<MovementRollbackState>
    {
        private const string MESH_CHILD_NAME = "mesh";

        private CharacterController _controller;
        private EntityConfig _config;
        private Transform _mesh;

        private Vector3 _lastMoveDir;
        private Vector3 _angularVelocity;
        private Vector3 _dashDir;
        private float _locomotionSpeed;
        private float _verticalVelocity;
        private float _dashAccumulator;
        private int _jumpCount;
        private bool _isDashing;

        #region 属性
        public override ModuleType ModuleType => ModuleType.Transform;
        public Vector3 Position => transform.position;
        public Quaternion Rotation => _mesh != null ? _mesh.rotation : Quaternion.identity;
        public Vector3 LinearVelocity { get; private set; }
        public Vector3 AngularVelocity => _angularVelocity;
        public bool IsGrounded => _controller != null && _controller.isGrounded;
        public bool IsDashing => _isDashing;
        public int JumpCount => _jumpCount;
        #endregion

        /// <summary>
        /// 初始化移动与转向运行时配置
        /// </summary>
        public void Init(EntityConfig config)
        {
            _config = config;
            _locomotionSpeed = _config.walkSpeed;
            _mesh = transform.Find(MESH_CHILD_NAME);
        }

        /// <summary>
        /// 直接设置实体位置，用于权威快照或插值快照应用
        /// </summary>
        public void Teleport(Vector3 position)
        {
            bool wasEnabled = _controller && _controller.enabled;
            if (wasEnabled) _controller.enabled = false;

            transform.position = position;

            if (wasEnabled) _controller.enabled = true;
        }

        /// <summary>
        /// 按视角相对移动输入，将 mesh 转向该目标朝向；不旋转根节点
        /// </summary>
        public void Rotate(Vector2 move, float viewYaw, float deltaTime)
        {
            if (deltaTime <= 0f) return;

            Vector3 planarDirection = Quaternion.Euler(0f, viewYaw, 0f) * new Vector3(move.x, 0f, move.y);
            if (planarDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                _angularVelocity = Vector3.zero;
                return;
            }

            Quaternion previousRotation = _mesh.rotation;
            Quaternion targetRotation = Quaternion.LookRotation(planarDirection.normalized, Vector3.up);
            _mesh.rotation = Quaternion.RotateTowards(previousRotation, targetRotation, 
                _config.meshTurnSpeed * deltaTime);

            Quaternion deltaRotation = _mesh.rotation * Quaternion.Inverse(previousRotation);
            deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;
            _angularVelocity = axis.sqrMagnitude > Mathf.Epsilon ? axis.normalized * (angle / deltaTime) : Vector3.zero;
        }

        /// <summary>
        /// 位移：平面速度沿 mesh 当前朝向，输入只提供相对 orientation 的目标转向与速度大小
        /// </summary>
        public void Move(Vector2 inputDir, float speed, float viewYaw, float dt)
        {
            if (!_controller) return;

            if (!_isDashing)
            {
                _locomotionSpeed = speed;
                UpdateLocomotionDir(inputDir, viewYaw);
            }

            ApplyGravity(dt);
            ApplyMovement(dt);
            TickDash(dt);
        }

        /// <summary>
        /// 尝试跳跃，成功则设置垂直速度并累计跳跃次数
        /// </summary>
        public bool Jump(float jumpSpeed, int maxJumpCount)
        {
            if (_isDashing) return false;
            if (_jumpCount >= maxJumpCount) return false;

            _jumpCount++;
            _verticalVelocity = jumpSpeed;
            return true;
        }

        private void UpdateLocomotionDir(Vector2 inputDir, float viewYaw)
        {
            float magnitude = Mathf.Clamp01(inputDir.magnitude);
            if (magnitude <= Mathf.Epsilon)
            {
                _lastMoveDir = Vector3.zero;
                return;
            }

            Vector3 meshForward = _mesh.forward;
            meshForward.y = 0f;
            if (meshForward.sqrMagnitude <= Mathf.Epsilon)
            {
                meshForward = Vector3.forward;
            }

            _lastMoveDir = meshForward.normalized * magnitude;
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

        #region 快照逻辑
        
        /// <summary>
        /// 按 Replica 切换驱动 CharacterController 或静态胶囊。
        /// </summary>
        public void SetReplicaMode(bool isReplica)
        {
            if (isReplica)
            {
                _controller = GetComponent<CharacterController>();
                if (_controller) _controller.enabled = false;

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
                _controller.enabled = true;
            }
        }

        /// <summary>
        /// 直接应用 mesh 世界空间旋转与角速度；根节点保持单位旋转
        /// </summary>
        public void Restore(Quaternion rotation, Vector3 angularVelocity)
        {
            _mesh.rotation = MathUtils.SafeNormalize(rotation);
            _angularVelocity = angularVelocity;
        }

        /// <summary>
        /// 捕获移动模块完整回滚状态
        /// </summary>
        public override MovementRollbackState CaptureRollbackState()
        {
            return new MovementRollbackState
            {
                rootPosition = Position,
                meshRotation = Rotation,
                rootLinearVelocity = LinearVelocity,
                meshAngularVelocity = _angularVelocity,
                lastMoveDirection = _lastMoveDir,
                dashDirection = _dashDir,
                appliedLocomotionSpeed = _locomotionSpeed,
                verticalVelocity = _verticalVelocity,
                dashRemainingTime = _dashAccumulator,
                jumpCount = _jumpCount,
                isDashing = _isDashing
            };
        }

        /// <summary>
        /// 恢复位置、姿态和全部移动内部状态
        /// </summary>
        public override void RestoreRollbackState(in MovementRollbackState state)
        {
            Teleport(state.rootPosition);
            Restore(state.meshRotation, state.meshAngularVelocity);
            LinearVelocity = state.rootLinearVelocity;
            _lastMoveDir = state.lastMoveDirection;
            _dashDir = state.dashDirection;
            _locomotionSpeed = state.appliedLocomotionSpeed;
            _verticalVelocity = state.verticalVelocity;
            _dashAccumulator = state.dashRemainingTime;
            _jumpCount = state.jumpCount;
            _isDashing = state.isDashing;
        }

        #endregion
    }
}
