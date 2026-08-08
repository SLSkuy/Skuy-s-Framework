using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体角色基类，提供一系列移动控制基础方法
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class EntityCharacter : MonoBehaviour
    {
        [SerializeField] protected EntityConfig config;

        private CharacterController _characterController;

        // 根节点只负责位置控制，不负责旋转
        private Transform _orientation; // 视角变换
        private Transform _mesh;   // 模型变换
        private bool _rotateToLookDir;    // 是否将模型旋转到当前视角朝向

        // 朝向控制
        private Quaternion _lastFaceDir;
        private bool _instantRotateToLookDir;

        // 位移控制
        private Vector2 _lastMoveDir;
        private float _locomotionMultiplier;
        private float _verticalVelocity;

        // 冲刺控制
        private float _dashAccumulator;
        private bool _isLockInput;

        #region 模拟入口

        /// <summary>
        /// 模拟运动入口，便于Tick驱动
        /// </summary>
        public void Simulate(float deltaTime)
        {
            Rotate(deltaTime);
            Locomotion(deltaTime);
        }
        
        /// <summary>
        /// 设置模型是否跟随视角，以及跟随时是否跳过旋转插值。
        /// </summary>
        public void SetRotateToLookDirection(bool enabled, bool instant = false)
        {
            _rotateToLookDir = enabled;
            _instantRotateToLookDir = instant;
        }

        private void Locomotion(float deltaTime)
        {
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
            else _verticalVelocity -= config.gravity * deltaTime;

            // 限制最大掉落速度
            if(_verticalVelocity < -config.maxFallSpeed) _verticalVelocity = -config.maxFallSpeed;

            Vector3 velocity = new Vector3(_lastMoveDir.x, 0f, _lastMoveDir.y) * _locomotionMultiplier;
            velocity.y = _verticalVelocity;

            _characterController.Move(velocity * deltaTime);
        }

        /// <summary>
        /// 视角即刻转换，模型朝向则根据选择进行插值或即刻旋转
        /// </summary>
        /// <param name="deltaTime"></param>
        private void Rotate(float deltaTime)
        {
            if (!_rotateToLookDir) return;

            if (_instantRotateToLookDir)
            {
                _mesh.rotation = _lastFaceDir;
                return;
            }

            float maxDegreesDelta = config.rotationSpeed * Mathf.Max(0f, deltaTime);
            _mesh.rotation = Quaternion.RotateTowards(
                _mesh.rotation,
                _lastFaceDir,
                maxDegreesDelta);
        }

        #endregion

        #region 实体控制

        public void Move(Vector2 dir)
        {
            if (!_isLockInput)
            {
                _lastMoveDir = dir;
            }
        }

        public void Aim(Vector2 aim)
        {
            if (aim.sqrMagnitude <= Mathf.Epsilon) return;

            Vector3 direction = new Vector3(aim.x, 0f, aim.y).normalized;
            _lastFaceDir = Quaternion.LookRotation(direction, Vector3.up);

            // 视角始终立即响应；模型是否跟随由 Rotate 在模拟 Tick 中决定。
            _orientation.rotation = _lastFaceDir;
        }

        public void StartSprint()
        {
            if (_isLockInput) return;

            _locomotionMultiplier = config.sprintSpeed;
        }

        public void StopSprint()
        {
            if (_isLockInput) return;

            _locomotionMultiplier = config.walkSpeed;
        }

        public void Dash()
        {
            if (_isLockInput) return;

            _isLockInput = true;
            _locomotionMultiplier = config.dashSpeed;
            _dashAccumulator = config.dashDuration;
        }

        private void OnDashComplete()
        {
            _isLockInput = false;
            _locomotionMultiplier = config.walkSpeed;
        }

        #endregion

        #region 生命周期

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();

            _rotateToLookDir = true;
            _orientation = transform.Find("orientation").transform;
            _mesh = transform.Find("mesh").transform;
            _lastFaceDir = _mesh.rotation;

            _locomotionMultiplier = config.walkSpeed;
        }

        private void Update()
        {
            Simulate(Time.deltaTime);
        }

        #endregion
    }
}
