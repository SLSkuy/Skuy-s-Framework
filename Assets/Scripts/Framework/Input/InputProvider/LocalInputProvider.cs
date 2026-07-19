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
        private bool _isInitialized;

        /// <summary>
        /// 鼠标面准事件
        /// </summary>
        public event Action<Vector2> OnMouseAim;

        private void Awake()
        {
            Init();
        }

        private void OnEnable()
        {
            if(!_isInitialized) Init();
            
            _actionMap[currentInputMap].Enable();
        }

        private void OnDisable()
        {
            _actionMap[currentInputMap].Disable();
        }

        private void Update()
        {
            UpdateOriginPlayerActionInput();
            CheckDiffFromLastState();

            // 鼠标设备单独处理输入
            if (deviceType == InputDeviceType.KeyboardAndMouse)
            {
                Vector2 mousePos = Vector2.zero;
                if (ScreenUtils.TryGetMouseWorldPosition(_mainCamera, out Vector3 mousePosition))
                {
                    mousePos = new Vector2(mousePosition.x, mousePosition.z);
                }

                OnMouseAim?.Invoke(mousePos);
            }
        }

        private void Init()
        {
            _actionMap = new Dictionary<LocalInputType, InputActionMap>();
            InputActions = new PlayerInputActions();
            PlayerActions = InputActions.Player;
            UIActions = InputActions.UI;
            
            RegisterInputAction(LocalInputType.Player, PlayerActions);
            RegisterInputAction(LocalInputType.UI, UIActions);

            // 如果是键盘输入，禁用Aim输出，采用鼠标射线检测
            if (deviceType == InputDeviceType.KeyboardAndMouse)
            {
                PlayerActions.Aim.Disable();
            }
            
            // 如果没有设置相机，自动查找主相机
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

            _isInitialized = true;
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
            _currentInputState.AimInput = PlayerActions.Aim.ReadValue<Vector2>();
            _currentInputState.IsPrimaryAttackPressed = PlayerActions.PrimaryAttack.IsPressed();
            _currentInputState.IsSpecialAttackPressed = PlayerActions.SpecialAttack.IsPressed();
            _currentInputState.IsSpecialActionPressed = PlayerActions.SpecialAction.IsPressed();
            _currentInputState.IsInteractPressed = PlayerActions.Interact.IsPressed();
            _currentInputState.IsSprintPressed = PlayerActions.Sprint.IsPressed();
            _currentInputState.IsDashPressed = PlayerActions.Dash.IsPressed();
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
