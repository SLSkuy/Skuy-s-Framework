using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体运动状态，提供实体最基础的运动状态更新
    /// </summary>
    public abstract class EntityLocomotionState : EntityBaseState
    {
        protected EntityLocomotionState(EntityContext context) : base(context) { }
        
        // 朝向控制
        private float _yaw;
        private float _pitch;

        // 位移控制
        private Vector3 _lastMoveDir;
        protected float _locomotionSpeed;
        protected float _verticalVelocity;

        // 跳跃控制
        protected int _jumpCount;

        // 冲刺控制
        private Vector3 _dashDir;
        private float _dashAccumulator;

        /// <summary>
        /// 视角即刻转换，模型朝向则根据当前 yaw 即刻转向
        /// </summary>
        public void Rotate(Vector2 aimInput, float dt)
        {
            if (aimInput.sqrMagnitude < Mathf.Epsilon) return;

            _yaw += aimInput.x * Config.aimHorizontalSpeed * dt;
            _pitch -= aimInput.y * Config.aimVerticalSpeed * dt;
            _pitch = Mathf.Clamp(_pitch, Config.minAimPitch, Config.maxAimPitch);

            Orientation.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Mesh.rotation = Quaternion.Euler(0f, _yaw, 0f);
        }

        private void UpdateLocomotionDir(Vector2 inputDir)
        {
            Vector3 forward = Orientation.forward; forward.y = 0f; forward.Normalize();
            Vector3 right = Orientation.right; right.y = 0f; right.Normalize();
            _lastMoveDir = forward * inputDir.y + right * inputDir.x;
            // 防止斜向移动速度加快
            _lastMoveDir = Vector3.ClampMagnitude(_lastMoveDir, 1f);
        }
        
        private void ApplyGravity(float dt)
        {
            if (Controller.isGrounded)
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
                _verticalVelocity -= Config.gravity * dt;
            }

            // 限制最大掉落速度
            if (_verticalVelocity < -Config.maxFallSpeed)
            {
                _verticalVelocity = -Config.maxFallSpeed;
            }
        }

        private void ApplyMovement(float dt)
        {
            Vector3 moveDir = Context.IsDashing ? _dashDir : _lastMoveDir;
            Vector3 velocity = new Vector3(moveDir.x, 0f, moveDir.z) * _locomotionSpeed;
            velocity.y = _verticalVelocity;
            Controller.Move(velocity * dt);
        }

        #region 状态控制

        protected override void Tick(float dt)
        {
            ApplyGravity(dt);
            Rotate(Context.LastAimInput, dt);
            UpdateLocomotionDir(Context.LastMoveInput);
            ApplyMovement(dt);
        }

        #endregion
    }
}