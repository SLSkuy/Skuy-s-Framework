using UnityEngine;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 实体动画能力组件，控制实体的动画播放
    /// </summary>
    public class AnimationModule : EntityModuleBase
    {
        private Animator _animator;
        private EntityStateMachineModule _stateMachine;
        private MovementModule _movement;
        private ViewModule _view;
        
        private float _targetSpeed;
        private float _targetTurn;
        private bool _wasRolling;
        
        private int _speedHash;
        private int _turnHash;
        private int _rollHash;
        
        #region 属性
        public override ModuleType ModuleType => ModuleType.Animation;
        #endregion

        /// <summary>
        /// 绑定后解析动画组件并缓存参数标识。
        /// </summary>
        protected override void OnBound(EntityCharacter target)
        {
            _animator = GetComponent<Animator>();
            _stateMachine = Context.Get<EntityStateMachineModule>();
            _movement = Context.Get<MovementModule>();
            _view = Context.Get<ViewModule>();
            
            _speedHash = Animator.StringToHash("Speed");
            _turnHash = Animator.StringToHash("Turn");
            _rollHash = Animator.StringToHash("Roll");
            
            _animator.ResetTrigger(_rollHash);
            _wasRolling = false;
            _targetSpeed = 0f;
            _targetTurn = 0f;
        }

        private void UpdateSpeed()
        {
            // 动画速度只在表现层平滑，不参与移动模拟或快照恢复
            float targetSpeed = Context.DesiredSpeed * Context.LastMoveInput.magnitude; // 神秘手柄输入
            float smoothTime = Config.animSpeedSmoothTime;
            float blend = smoothTime > 0f ? 1f - Mathf.Exp(-Time.deltaTime / smoothTime) : 1f;
            _targetSpeed = Mathf.Lerp(_targetSpeed, targetSpeed, blend);
            if (Mathf.Abs(_targetSpeed - targetSpeed) < 0.001f)
            {
                _targetSpeed = targetSpeed;
            }
            _animator.SetFloat(_speedHash, _targetSpeed);
        }
        
        private void UpdateTurn()
        {
            // 计算转向数值
            float targetTurn = CalculateTurn();
            float smoothTime = Config.animTurnSmoothTime;
            float blend = smoothTime > 0f ? 1f - Mathf.Exp(-Time.deltaTime / smoothTime) : 1f;
            _targetTurn = Mathf.Lerp(_targetTurn, targetTurn, blend);
            if (Mathf.Abs(_targetTurn - targetTurn) < 0.001f)
            {
                _targetTurn = targetTurn;
            }
            _animator.SetFloat(_turnHash, _targetTurn);
        }

        /// <summary>
        /// 根据当前表现帧的逻辑状态触发翻滚，避免固定 Tick 重演重复发送动画事件
        /// </summary>
        private void UpdateRoll()
        {
            bool isRolling = _stateMachine.CurrentState == EntityState.ROLL;
            if (isRolling && !_wasRolling)
            {
                _animator.SetTrigger(_rollHash);
            }
            else if (!isRolling && _wasRolling)
            {
                _animator.ResetTrigger(_rollHash);
            }

            _wasRolling = isRolling;
        }

        /// <summary>
        /// 将 mesh 朝向到视角相对输入朝向的水平夹角映射到 [-1, 1]，左负右正。
        /// </summary>
        private float CalculateTurn()
        {
            Vector2 moveInput = Context.LastMoveInput;
            if (moveInput.sqrMagnitude <= Mathf.Epsilon) return 0f;

            Vector3 inputDirection = Quaternion.Euler(0f, _view.Yaw, 0f) * new Vector3(moveInput.x, 0f, moveInput.y);
            Vector3 meshForward = Vector3.ProjectOnPlane(_movement.Rotation * Vector3.forward, Vector3.up);
            float angle = Vector3.SignedAngle(meshForward, inputDirection, Vector3.up);
            return Mathf.Clamp(angle / 180f, -1f, 1f);
        }

        #region 生命周期

        private void Update()
        {
            UpdateSpeed();
            UpdateTurn();
            UpdateRoll();
        }
        
        private void OnDisable()
        {
            // 角色还池或停用后清除上一轮的动画插值状态和未消费的翻滚触发器。
            _animator?.ResetTrigger(_rollHash);
            _targetSpeed = 0f;
            _targetTurn = 0f;
            _wasRolling = false;
        }

        #endregion
    }
}