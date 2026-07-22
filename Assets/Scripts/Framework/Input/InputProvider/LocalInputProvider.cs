using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Utils;

namespace Framework
{
    /// <summary>
    /// 本地原生输入封装
    /// </summary>
    public class LocalInputProvider : BaseInputProvider
    {
        [Header("输入设备配置")]
        public InputDeviceType deviceType = InputDeviceType.KeyboardAndMouse;
        
        public PlayerInputActions InputActions { get; private set; }
        public PlayerInputActions.PlayerActions PlayerActions { get; private set; }
        public PlayerInputActions.UIActions UIActions { get; private set; }

        /// <summary>
        /// 当前启用的ActionMap
        /// </summary>
        public LocalInputType currentInputMap = LocalInputType.Player;
        private Dictionary<LocalInputType, InputActionMap> _actionMap;
        private Camera _mainCamera;

        /// <summary>
        /// 鼠标瞄准事件
        /// </summary>
        public event Action<Vector2> OnMouseAim;

        private void Awake()
        {
            Init();
        }

        private void OnEnable()
        {
            _actionMap[currentInputMap].Enable();
        }

        private void OnDisable()
        {
            _actionMap[currentInputMap].Disable();
        }

        private void Update()
        {
            UpdateOriginPlayerActionInput();
            UpdateMouseAimInput();
            CheckDiffFromLastState();
        }

        private void Init()
        {
            _actionMap = new Dictionary<LocalInputType, InputActionMap>();
            InputActions = new PlayerInputActions();
            PlayerActions = InputActions.Player;
            UIActions = InputActions.UI;
            
            RegisterInputAction(LocalInputType.Player, PlayerActions);
            RegisterInputAction(LocalInputType.UI, UIActions);

            // 如果没有设置相机，自动查找主相机
            if (!_mainCamera)
            {
                _mainCamera = Camera.main;
            }
        }

        public void RegisterInputAction(LocalInputType type, InputActionMap action)
        {
            if(action != null)
                _actionMap[type] = action;
        }

        public void UnregisterInputAction(LocalInputType type)
        {
            _actionMap.Remove(type);
        }

        /// <summary>
        /// 更新本地玩家控制输入
        /// </summary>
        private void UpdateOriginPlayerActionInput()
        {
            // 缓存上一帧输入，用于检测需要触发哪些事件
            _previousInputState = _currentInputState;
            
            // 获取当前帧中输入
            _currentInputState.MoveInput = PlayerActions.Move.ReadValue<Vector2>();
            Vector2 aimInput = PlayerActions.Aim.ReadValue<Vector2>();
            if (deviceType == InputDeviceType.GamePad) aimInput = TransformUtils.MapInputToWorldDirection2D(aimInput, _mainCamera.transform);
            _currentInputState.AimInput = aimInput;
            _currentInputState.IsPrimaryAttackPressed = PlayerActions.PrimaryAttack.IsPressed();
            _currentInputState.IsSpecialAttackPressed = PlayerActions.SpecialAttack.IsPressed();
            _currentInputState.IsSpecialActionPressed = PlayerActions.SpecialAction.IsPressed();
            _currentInputState.IsInteractPressed = PlayerActions.Interact.IsPressed();
            _currentInputState.IsSprintPressed = PlayerActions.Sprint.IsPressed();
            _currentInputState.IsDashPressed = PlayerActions.Dash.IsPressed();
        }

        /// <summary>
        /// 将鼠标位置转换为世界空间瞄准方向，并写入可捕获的输入状态。
        /// </summary>
        private void UpdateMouseAimInput()
        {
            if (deviceType != InputDeviceType.KeyboardAndMouse) return;
            Vector2 aimDirection = Vector2.zero;
            if (ScreenUtils.TryGetMouseWorldPosition(_mainCamera, out Vector3 mousePosition))
            {
                Vector3 direction = mousePosition - transform.position;
                aimDirection = new Vector2(direction.x, direction.z).normalized;
            }

            _currentInputState.AimInput = aimDirection;
            OnMouseAim?.Invoke(aimDirection);
        }

        /// <summary>
        /// 切换ActionMap
        /// </summary>
        /// <param name="type">ActionMap类型</param>
        public void SwitchInputMap(LocalInputType type)
        {
            if (_actionMap.ContainsKey(type))
            {
                _actionMap[currentInputMap].Disable();
                _actionMap[type].Enable();
                currentInputMap = type;
            }
        }

        /// <summary>
        /// 禁用某一ActionMap一段时间
        /// </summary>
        /// <param name="type">需要禁用的ActionMap类型</param>
        /// <param name="sec">时间</param>
        public void DisableActionForSec(LocalInputType type, float sec)
        {
            DisableActionForSec(_actionMap[type], sec);
        }

        /// <summary>
        /// 禁用某一ActionMap一段时间
        /// </summary>
        /// <param name="actionMap">需要禁用的行为</param>
        /// <param name="sec">时间</param>
        public void DisableActionForSec(InputActionMap actionMap, float sec)
        {
            StartCoroutine(DisableAction(actionMap, sec));
        }

        private IEnumerator DisableAction(InputActionMap actionMap, float sec)
        {
            actionMap.Disable();
            yield return new WaitForSeconds(sec);
            actionMap.Enable();
        }
    }
}
