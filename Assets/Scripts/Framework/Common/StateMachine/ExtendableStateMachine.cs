using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.StateMachine
{
    /// <summary>
    /// 可拓展状态机，需要创建静态类并声明静态状态变量标识
    /// </summary>
    public class ExtendableStateMachine<TKey> where TKey : IEquatable<TKey>
    {
        private readonly Dictionary<TKey, IExtendableState<TKey>> _states = new();
        private IExtendableState<TKey> _currentEnumState;
        
        /// <summary>
        /// 当前状态
        /// </summary>
        public TKey CurrentState => _currentEnumState != null ? _currentEnumState.StateKey : default;

        #region 事件
        public event Action<TKey, TKey> OnStateChange;
        #endregion

        #region 状态管理

        /// <summary>
        /// 注册状态
        /// </summary>
        public void RegisterState(IExtendableState<TKey> state)
        {
            if (!_states.TryAdd(state.StateKey, state))
            {
                Debug.LogError($"State already exists : {state.StateKey}");
            }
        }

        /// <summary>
        /// 更换状态
        /// </summary>
        /// <param name="state"></param>
        public void ChangeState(TKey state)
        {
            if (!_states.TryGetValue(state, out var newState))
            {
                Debug.LogError($"State not found : {state}");
                return;
            }

            if (_currentEnumState == newState)
                return;
            
            // 初始状态变化事件
            OnStateChange?.Invoke(CurrentState, state);

            _currentEnumState?.Exit();
            _currentEnumState = newState;
            _currentEnumState.Enter();
        }

        #endregion

        #region 生命周期

        public void Update(float deltaTime)
        {
            _currentEnumState?.Update(deltaTime);
        }

        public void FixedUpdate(float fixedDeltaTime)
        {
            _currentEnumState?.FixedUpdate(fixedDeltaTime);
        }

        public void LateUpdate()
        {
            _currentEnumState?.LateUpdate();
        }

        #endregion
    }
}