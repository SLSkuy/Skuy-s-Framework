using System;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 玩家输入提供器基类，提供属性获取与各类事件订阅
    /// </summary>
    public class BaseInputProvider : MonoBehaviour, IInputStateProvider
    {
        #region 属性
        public Vector2 MoveInput => _currentInputState.MoveInput;
        public Vector2 AimInput => _currentInputState.AimInput;
        public bool IsPrimaryAttackPressed => _currentInputState.IsPrimaryAttackPressed;
        public bool IsSpecialAttackPressed => _currentInputState.IsSpecialAttackPressed;
        public bool IsSpecialActionPressed => _currentInputState.IsSpecialActionPressed;
        public bool IsInteractPressed => _currentInputState.IsInteractPressed;
        public bool IsSprintPressed => _currentInputState.IsSprintPressed;
        public bool IsJumpPressed => _currentInputState.IsJumpPressed;
        public bool IsSwitchModePressed => _currentInputState.IsSwitchModePressed;
        #endregion
        
        /// <summary>
        /// 缓存本地输入状态，暂时只针对PlayerActions
        /// </summary>
        protected InputState _currentInputState = new();
        protected InputState _previousInputState = new();
        
        #region 事件
        public event Action<Vector2> OnMove;
        public event Action<Vector2> OnAim;
        public event Action OnPrimaryAttackPressed;
        public event Action OnPrimaryAttackReleased;
        public event Action OnSpecialAttackPressed;
        public event Action OnSpecialAttackReleased;
        public event Action OnSpecialActionPressed;
        public event Action OnSpecialActionReleased;
        public event Action OnInteractPressed;
        public event Action OnInteractReleased;
        public event Action OnSprintPressed;
        public event Action OnSprintReleased;
        public event Action OnJump;
        public event Action OnSwitchModePressed;
        #endregion
        
        /// <summary>
        /// 检测输入差异并触发相应事件
        /// 网络控制器则由Tick驱动调用
        /// </summary>
        public void CheckDiffFromLastState()
        {
            if (_currentInputState.MoveInput != _previousInputState.MoveInput)
                OnMove?.Invoke(_currentInputState.MoveInput);
            
            if (_currentInputState.AimInput != _previousInputState.AimInput)
                OnAim?.Invoke(_currentInputState.AimInput);
            
            if (_currentInputState.IsPrimaryAttackPressed && !_previousInputState.IsPrimaryAttackPressed)
                OnPrimaryAttackPressed?.Invoke();
            else if (!_currentInputState.IsPrimaryAttackPressed && _previousInputState.IsPrimaryAttackPressed)
                OnPrimaryAttackReleased?.Invoke();
            
            if (_currentInputState.IsSpecialAttackPressed && !_previousInputState.IsSpecialAttackPressed)
                OnSpecialAttackPressed?.Invoke();
            else if (!_currentInputState.IsSpecialAttackPressed && _previousInputState.IsSpecialAttackPressed)
                OnSpecialAttackReleased?.Invoke();
            
            if (_currentInputState.IsSpecialActionPressed && !_previousInputState.IsSpecialActionPressed)
                OnSpecialActionPressed?.Invoke();
            else if (!_currentInputState.IsSpecialActionPressed && _previousInputState.IsSpecialActionPressed)
                OnSpecialActionReleased?.Invoke();
            
            if (_currentInputState.IsInteractPressed && !_previousInputState.IsInteractPressed)
                OnInteractPressed?.Invoke();
            else if (!_currentInputState.IsInteractPressed && _previousInputState.IsInteractPressed)
                OnInteractReleased?.Invoke();
            
            if (_currentInputState.IsSprintPressed && !_previousInputState.IsSprintPressed)
                OnSprintPressed?.Invoke();
            else if (!_currentInputState.IsSprintPressed && _previousInputState.IsSprintPressed)
                OnSprintReleased?.Invoke();
            
            if(_currentInputState.IsJumpPressed && !_previousInputState.IsJumpPressed)
                OnJump?.Invoke();
            
            if(_currentInputState.IsSwitchModePressed && !_previousInputState.IsSwitchModePressed)
                OnSwitchModePressed?.Invoke();
        }
        
        /// <summary>
        /// 获取当前输入状态
        /// </summary>
        /// <returns>当前输入状态</returns>
        public virtual InputState GetInputState()
        {
            return _currentInputState;
        }

        /// <summary>
        /// 设置当前输入状态
        /// </summary>
        /// <param name="state">输入状态</param>
        public virtual void SetInputState(InputState state)
        {
            // 什么也不做，本地输入不能覆盖，只能从InputSystem读取数据
            // 远程输入重写该方法以应用远程输入
        }
    }
}