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

        // 朝向控制
        private Vector2 _lastAimInput;
        private Quaternion _lastFaceDir;
        private float _yaw;
        private float _pitch;

        // 位移控制
        private Vector2 _lastMoveInput;
        private Vector2 _lastMoveDir;
        private float _locomotionMultiplier;
        private float _verticalVelocity;
        
        // 跳跃控制
        private int _jumpCount;

        // 冲刺控制
        private float _dashAccumulator;
        private bool _isSprinting;
        private bool _isDashing;

        #region 模拟入口

        /// <summary>
        /// 模拟运动入口，便于Tick驱动
        /// </summary>
        public void Simulate(float deltaTime)
        {
            ApplyGravity(deltaTime);
            Rotate(deltaTime);
            Locomotion(deltaTime);
        }

        private void Locomotion(float deltaTime)
        {
            if (_isDashing)
            {
                _dashAccumulator -= deltaTime;
                if (_dashAccumulator <= 0f)
                {
                    OnDashComplete();
                }
            }
            
            // 将移动方向调整为当前视角朝向
            Vector3 faceDir = _orientation.forward * _lastMoveInput.y + _orientation.right * _lastMoveInput.x;
            _lastMoveDir = new Vector2(faceDir.x, faceDir.z);

            // 限制最大掉落速度
            if(_verticalVelocity <= -config.maxFallSpeed) _verticalVelocity = -config.maxFallSpeed;
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
            if (_lastAimInput.sqrMagnitude < Mathf.Epsilon) return;
            
            // 计算俯仰角
            _yaw += _lastAimInput.x * config.aimHorizontalSpeed * deltaTime;
            _pitch -= _lastAimInput.y * config.aimVerticalSpeed * deltaTime;
            _pitch = Mathf.Clamp(_pitch, config.minAimPitch, config.maxAimPitch);

            _lastFaceDir = Quaternion.Euler(_pitch, _yaw, 0f);
            _orientation.rotation = _lastFaceDir;
            
            // 模型旋转，当前立即转向视角朝向
            _mesh.rotation = Quaternion.Euler(0, _yaw, 0f);
        }

        private void ApplyGravity(float deltaTime)
        {
            // 重力计算
            if (_characterController.isGrounded)
            {
                if (_verticalVelocity < 0f)
                {
                    _verticalVelocity = -1f; // 防止奇怪的抽动，添加一个默认的向下速度
                    _jumpCount = 0; // 滞空跳跃累计次数
                }
            }
            else
            {
                if(_jumpCount < 1) _jumpCount = 1;   // 从边缘坠落时只允许跳跃一次
                
                _verticalVelocity -= config.gravity * deltaTime;
            }
        }

        #endregion

        #region 实体控制

        public void Move(Vector2 dir)
        {
            if (_isDashing) return;
            
            _lastMoveInput = dir;
        }

        public void Aim(Vector2 aim)
        {
            _lastAimInput = aim;
        }
        
        public void StartSprint()
        {
            if (_isDashing) return;

            _locomotionMultiplier = config.sprintSpeed;
        }

        public void StopSprint()
        {
            if (_isDashing) return;

            _locomotionMultiplier = config.walkSpeed;
        }
        
        public void Jump()
        {
            if (_jumpCount >= config.jumpCount) return;
            
            _jumpCount++;
            
            _verticalVelocity = config.jumpSpeed;
        }

        public void Dash()
        {
            if (_isDashing) return;

            _isDashing = true;
            _locomotionMultiplier = config.dashSpeed;
            _dashAccumulator = config.dashDuration;
        }

        private void OnDashComplete()
        {
            _isDashing = false;
            _locomotionMultiplier = config.walkSpeed;
        }

        #endregion

        #region 生命周期

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            
            _orientation = transform.Find("orientation").transform;
            _mesh = transform.Find("mesh").transform;

            _locomotionMultiplier = config.walkSpeed;
        }

        private void Update()
        {
            Simulate(Time.deltaTime);
        }

        #endregion
    }
}
