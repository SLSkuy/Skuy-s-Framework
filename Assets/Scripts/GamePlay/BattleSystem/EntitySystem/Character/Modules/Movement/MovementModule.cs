using UnityEngine;
using Utils;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 实体根节点 Transform 能力：位置模拟、mesh 身体偏航与 Replica 碰撞切换
    /// 根节点旋转保持为单位四元数；身体朝向写在直接子节点 mesh 上
    /// </summary>
    public class MovementModule : EntitySnapshotModuleBase<MovementSnapshot>
    {
        private const string MESH_CHILD_NAME = "mesh";

        private CharacterController _controller;
        private Transform _mesh;

        private Vector3 _lastMoveDir;
        private float _verticalVelocity;
        private int _jumpCount;
        private bool _isGrounded;

        #region 属性
        public override ModuleType ModuleType => ModuleType.Movement;
        public Vector3 Position => transform.position;
        public Quaternion Rotation => _mesh ? _mesh.rotation : Quaternion.identity;
        // public Vector3 LinearVelocity { get; private set; }
        // public Vector3 AngularVelocity { get; private set; }
        public bool IsGrounded => _isGrounded;
        public int JumpCount => _jumpCount;
        #endregion

        /// <summary>
        /// 绑定后解析移动节点和角色控制器。
        /// </summary>
        protected override void OnBound(EntityCharacter target)
        {
            _mesh = transform.Find($"{EntityVisualPresentation.VISUAL_CHILD_NAME}/{MESH_CHILD_NAME}");
            _controller = GetComponent<CharacterController>();
        }
        
        /// <summary>
        /// 直接设置实体根位置
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
                // AngularVelocity = Vector3.zero;
                return;
            }

            Quaternion previousRotation = _mesh.rotation;
            Quaternion targetRotation = Quaternion.LookRotation(planarDirection.normalized, Vector3.up);
            _mesh.rotation = Quaternion.RotateTowards(previousRotation, targetRotation, 
                Config.meshTurnSpeed * deltaTime);

            // Quaternion deltaRotation = _mesh.rotation * Quaternion.Inverse(previousRotation);
            // deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
            // if (angle > 180f) angle -= 360f;
            // AngularVelocity = axis.sqrMagnitude > Mathf.Epsilon ? axis.normalized * (angle / deltaTime) : Vector3.zero;
        }

        /// <summary>
        /// 位移：平面速度沿 mesh 当前朝向，输入只提供相对 orientation 的目标转向与速度大小
        /// </summary>
        public void Move(Vector2 inputDir, float speed, float viewYaw, float dt)
        {
            if (!_controller) return;

            // 更新朝向
            UpdateLocomotionDir(inputDir, viewYaw);

            ApplyGravity(dt);
            ApplyMovement(speed, dt);
            _isGrounded = _controller.isGrounded;
        }

        /// <summary>
        /// 尝试跳跃，成功则设置垂直速度并累计跳跃次数
        /// </summary>
        public bool Jump(float jumpSpeed, int maxJumpCount)
        {
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
            if (_isGrounded)
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
                _verticalVelocity -= Config.gravity * dt;
            }

            if (_verticalVelocity < -Config.maxFallSpeed)
            {
                _verticalVelocity = -Config.maxFallSpeed;
            }
        }

        private void ApplyMovement(float speed, float dt)
        {
            Vector3 velocity = new Vector3(_lastMoveDir.x, 0f, _lastMoveDir.z) * speed;
            velocity.y = _verticalVelocity;
            // LinearVelocity = velocity;
            _controller.Move(velocity * dt);
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
                collider.height = Config.height;
                collider.radius = Config.radius;
                collider.center = new Vector3(0, Config.height / 2, 0);
            }
            else
            {
                _controller = gameObject.GetOrAddComponent<CharacterController>();
                _controller.height = Config.height;
                _controller.radius = Config.radius;
                _controller.skinWidth = 0.0001f;
                _controller.minMoveDistance = 0f;
                _controller.center = new Vector3(0, Config.height / 2, 0);
                _controller.enabled = true;
            }
        }

        /// <summary>
        /// 直接应用 mesh 世界空间旋转；根节点保持单位旋转
        /// </summary>
        public void Restore(Quaternion rotation)
        {
            _mesh.rotation = MathUtils.SafeNormalize(rotation);
            // AngularVelocity = angularVelocity;
        }

        /// <summary>
        /// 捕获移动模块完整回滚状态
        /// </summary>
        public override MovementSnapshot CaptureSnapshot()
        {
            return new MovementSnapshot
            {
                rootPosition = Position,
                meshRotation = Rotation,
                // rootLinearVelocity = LinearVelocity,
                // meshAngularVelocity = AngularVelocity,
                lastMoveDirection = _lastMoveDir,
                verticalVelocity = _verticalVelocity,
                jumpCount = _jumpCount,
                isGrounded = _isGrounded
            };
        }

        /// <summary>
        /// 恢复位置、姿态和全部移动内部状态
        /// </summary>
        public override void RestoreSnapshot(in MovementSnapshot state)
        {
            Teleport(state.rootPosition);
            // Restore(state.meshRotation, state.meshAngularVelocity);
            Restore(state.meshRotation);
            // LinearVelocity = state.rootLinearVelocity;
            // LinearVelocity = Vector3.zero;
            _lastMoveDir = state.lastMoveDirection;
            _verticalVelocity = state.verticalVelocity;
            _jumpCount = state.jumpCount;
            _isGrounded = state.isGrounded;
        }

        #endregion
        
        /// <summary>
        /// 清掉上一场留下的速度和跳跃计数。
        /// </summary>
        public void ResetMotion()
        {
            _lastMoveDir = Vector3.zero;
            _verticalVelocity = 0f;
            _jumpCount = 0;
            _isGrounded = false;
            // LinearVelocity = Vector3.zero;
            // AngularVelocity = Vector3.zero;
        }
    }
}
