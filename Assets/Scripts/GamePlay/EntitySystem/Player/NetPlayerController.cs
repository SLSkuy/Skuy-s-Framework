using Framework;
using UnityEngine;
using Utils;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 远程玩家控制器，捕获远程输入，对本地实体进行模拟
    /// </summary>
    public class NetPlayerController : AutoEventMonoBehaviour
    {
        public bool IsReady { get; private set; }

        private NetPlayerCharacter _playerCharacter;
        private IInputStateProvider _inputProvider;
        private Transform _cameraTransform;

        private Vector2 _currentRawMoveInput;
        private Vector2 _currentMappedMoveInput;

        #region 模拟

        /// <summary>
        /// 使用Tick间隔模拟，避免帧率不同导致模拟结果不一致
        /// </summary>
        /// <param name="deltaTime"></param>
        public void Simulate(float deltaTime)
        {
            if (!IsReady) return;

            // 通过Tick驱动获取输入变化，再触发内部事件
            // 若在设置输入状态时直接触发，会扰乱原本的Tick驱动顺序
            _inputProvider.CheckDiffFromLastState();
            _playerCharacter.Move(_currentMappedMoveInput, deltaTime);
        }

        #endregion
        
        /// <summary>
        /// 设置相机Transform
        /// </summary>
        /// <param name="cameraTrans">相机Transform</param>
        public void SetCameraTransform(Transform cameraTrans)
        {
            _cameraTransform = cameraTrans;
        }

        /// <summary>
        /// 配置网络控制器操控的玩家角色以及输入源
        /// </summary>
        public void Configure(NetPlayerCharacter playerCharacter, IInputStateProvider provider)
        {
            IsReady = true;
            _playerCharacter = playerCharacter;
            _inputProvider = provider;
        }
        
        /// <summary>
        /// 获取映射后的输入方向
        /// </summary>
        /// <returns>映射后的方向</returns>
        private Vector2 GetMappedDirection(Vector2 inputDir)
        {
            if (!_cameraTransform)
            {
                return inputDir;
            }
            
            return TransformUtils.MapInputToWorldDirection2D(inputDir, _cameraTransform);
        }

        #region 事件订阅
        
        [AutoEvent("OnMove", nameof(_inputProvider))]
        private void OnMoveEvent(Vector2 moveInput)
        {
            _currentRawMoveInput = moveInput;
            _currentMappedMoveInput = GetMappedDirection(moveInput);
        }

        [AutoEvent("OnPrimaryAttackPressed", nameof(_inputProvider))]
        private void OnPrimaryAttackPressed()
        {
            _playerCharacter.PrimaryAttack();
        }

        [AutoEvent("OnPrimaryAttackReleased", nameof(_inputProvider))]
        private void OnPrimaryAttackReleased()
        {
            _playerCharacter.StopPrimaryAttack();
        }

        [AutoEvent("OnSpecialAttackPressed", nameof(_inputProvider))]
        private void OnSpecialAttackPressed()
        {
            _playerCharacter.SpecialAttack();
        }
        
        [AutoEvent("OnSpecialAttackReleased", nameof(_inputProvider))]
        private void OnSpecialAttackReleased()
        {
            _playerCharacter.StopSpecialAttack();
        }

        [AutoEvent("OnSpecialActionPressed", nameof(_inputProvider))]
        private void OnSpecialActionPressed()
        {
            _playerCharacter.SpecialAction();
        }

        [AutoEvent("OnSpecialActionReleased", nameof(_inputProvider))]
        private void OnSpecialActionReleased()
        {
            _playerCharacter.StopSpecialAction();
        }

        [AutoEvent("OnInteractPressed", nameof(_inputProvider))]
        private void OnInteractPressed()
        {
            _playerCharacter.Interact();
        }

        [AutoEvent("OnSprintPressed", nameof(_inputProvider))]
        private void OnSprintPressed()
        {
            _playerCharacter.StartSprint();
        }

        [AutoEvent("OnSprintReleased", nameof(_inputProvider))]
        private void OnSprintReleased()
        {
            _playerCharacter.StopSprint();
        }

        [AutoEvent("OnDashPressed", nameof(_inputProvider))]
        private void OnDashPressed()
        {
            _playerCharacter.Dash();
        }

        [AutoEvent("OnAim", nameof(_inputProvider))]
        private void OnAim(Vector2 aimDirection)
        {
            if (aimDirection == Vector2.zero)
            {
                return;
            }
            
            Vector2 mappedDirection = GetMappedDirection(aimDirection);
            Vector3 targetPosition = new Vector3(mappedDirection.x, 0, mappedDirection.y);
            _playerCharacter.Rotate(targetPosition);
        }
        
        [AutoEvent("OnMouseAim", nameof(_inputProvider))]
        private void OnMouseAim(Vector2 aimDirection)
        {
            if (aimDirection == Vector2.zero)
            {
                return;
            }
            
            // 构成朝向方向向量
            Vector3 targetPosition = new Vector3(aimDirection.x, 0, aimDirection.y) - transform.position;
            _playerCharacter.Rotate(targetPosition);
        }

        #endregion
        
        #region 生命周期

        protected override void Start()
        {
            // 测试：直接从本地获取到输入源
            // TODO：多控制器输入时，设置输入源管理进行分配
            _inputProvider ??= GetComponent<IInputStateProvider>();
            _playerCharacter ??= GetComponent<NetPlayerCharacter>();
            
            // 如果没有设置相机，自动查找主相机
            if (_cameraTransform == null)
            {
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    _cameraTransform = mainCamera.transform;
                }
            }
            
            base.Start();
        }

        #endregion
    }
}
