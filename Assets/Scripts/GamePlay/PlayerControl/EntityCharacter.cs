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
        private Vector3 _lastMoveDir;
        private float _locomotionSpeed;
        private float _verticalVelocity;
        
        // 跳跃控制
        private int _jumpCount;

        // 冲刺控制
        private Vector3 _dashDir;
        private float _dashAccumulator;
        private bool _isSprinting;
        private bool _isDashing;

        #region 模拟入口

        /// <summary>
        /// 模拟运动入口，便于Tick驱动
        /// </summary>
        public void Simulate(float deltaTime)
        {
            Rotate(deltaTime);
            ApplyGravity(deltaTime);
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
            else
            {
                // 将移动方向调整为当前视角朝向
                Vector3 forward = _orientation.forward; forward.y = 0; forward.Normalize();
                Vector3 right = _orientation.right; right.y = 0; right.Normalize();
                _lastMoveDir = forward * _lastMoveInput.y + right * _lastMoveInput.x;
                
                // 防止斜向移动速度加快（什么起源引擎）
                _lastMoveDir = Vector3.ClampMagnitude(_lastMoveDir,1);
            }
            
            Vector3 moveDir = _isDashing ? _dashDir : _lastMoveDir;
            
            // 限制最大掉落速度
            if(_verticalVelocity < -config.maxFallSpeed) _verticalVelocity = -config.maxFallSpeed;
            Vector3 velocity = new Vector3(moveDir.x, 0f, moveDir.z) * _locomotionSpeed;
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
            if(!_characterController.isGrounded) return;
            
            _isSprinting = true;
            
            if (_isDashing) return;
            
            _locomotionSpeed = config.sprintSpeed;
        }

        public void StopSprint()
        {
            _isSprinting = false;
            
            if (_isDashing) return;
            
            _locomotionSpeed = config.walkSpeed;
        }
        
        public void Jump()
        {
            if (_isDashing) return;
            
            if (_jumpCount >= config.jumpCount) return;
            
            _jumpCount++;
            
            _verticalVelocity = config.jumpSpeed;
        }

        public void Dash()
        {
            if (_isDashing) return;
            
            if (!_characterController.isGrounded) return;
            
            // 如果没有输入，则按当前朝向进行冲刺
            Vector3 forward = _orientation.forward; forward.y=0; forward.Normalize();
            Vector3 right = _orientation.right; right.y=0; right.Normalize();
            Vector3 dashDir = forward * _lastMoveInput.y + right * _lastMoveInput.x;
            if(dashDir.sqrMagnitude < Mathf.Epsilon) dashDir = forward;

            _dashDir = Vector3.ClampMagnitude(dashDir, 1f);

            _isDashing = true;
            _locomotionSpeed = config.dashSpeed;
            _dashAccumulator = config.dashDuration;
        }

        private void OnDashComplete()
        {
            _isDashing = false;
            _locomotionSpeed = _isSprinting ? config.sprintSpeed : config.walkSpeed;
        }

        #endregion

        #region 生命周期

        private void Awake()
        {
            _orientation = transform.Find("orientation").transform;
            _mesh = transform.Find("mesh").transform;
            
            _characterController = GetComponent<CharacterController>();
            
            _locomotionSpeed = config.walkSpeed;
        }

        private void Update()
        {
            Simulate(Time.deltaTime);
        }

        #endregion
    }
}
