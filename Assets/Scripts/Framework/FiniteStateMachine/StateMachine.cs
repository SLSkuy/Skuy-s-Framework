using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.FiniteStateMachine
{
    /// <summary>
    /// 泛型状态机
    /// </summary>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    public class StateMachine<TEnum> where TEnum : Enum
    {
        private readonly Dictionary<TEnum, IState<TEnum>> _states = new();
        private IState<TEnum> _currentState;
        
        /// <summary>
        /// 当前状态
        /// </summary>
        public TEnum CurrentState => _currentState != null ? _currentState.StateKey : default;

        #region 事件
        public event Action<TEnum, TEnum> OnStateChange;
        #endregion

        #region 状态管理

        /// <summary>
        /// 注册状态
        /// </summary>
        public void RegisterState(IState<TEnum> state)
        {
            if (!_states.TryAdd(state.StateKey, state))
            {
                Debug.LogError($"State already exists : {state.StateKey}");
            }
        }

        /// <summary>
        /// 更换状态
        /// </summary>
        /// <param name="stateKey"></param>
        public void ChangeState(TEnum stateKey)
        {
            if (!_states.TryGetValue(stateKey, out var newState))
            {
                Debug.LogError($"State not found : {stateKey}");
                return;
            }

            if (_currentState == newState)
                return;
            
            // 初始状态变化事件
            OnStateChange?.Invoke(CurrentState, stateKey);

            _currentState?.Exit();
            _currentState = newState;
            _currentState.Enter();
        }

        #endregion

        #region 生命周期

        public void Update(float deltaTime)
        {
            _currentState?.Update(deltaTime);
        }

        public void FixedUpdate(float fixedDeltaTime)
        {
            _currentState?.FixedUpdate(fixedDeltaTime);
        }

        public void LateUpdate()
        {
            _currentState?.LateUpdate();
        }

        #endregion
    }
}