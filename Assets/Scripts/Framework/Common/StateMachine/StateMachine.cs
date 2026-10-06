using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 泛型状态机
    /// </summary>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    public class StateMachine<TEnum> where TEnum : Enum
    {
        private readonly Dictionary<TEnum, IState<TEnum>> _states = new();

        /// <summary>
        /// 缓存的切换请求
        /// </summary>
        private readonly Queue<IState<TEnum>> _pendingStates = new();
        private IState<TEnum> _current;
        private bool _isTransition;

        #region 属性

        /// <summary>
        /// 当前状态
        /// </summary>
        public TEnum CurrentState => _current != null ? _current.StateKey : default;
 
        /// <summary>
        /// 当前状态对象。第一次转换之前为空。
        /// </summary>
        public IState<TEnum> Current => _current;

        #endregion

        #region 事件
        public event Action<TEnum, TEnum> OnStateChange;

        /// <summary>
        /// 切换任务失败，没有进入 <see cref="ProcedureTransition{TEnum}.NextState"/>
        /// 状态停留在当前状态中
        /// </summary>
        public event Action<ProcedureTransition<TEnum>> OnTransitionFailed;
        #endregion

        #region 状态管理

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// 注册状态
        /// </summary>
        public void RegisterState(IState<TEnum> state)
        {
            if (!_states.TryAdd(state.StateKey, state))
            {
                Debug.LogError($"[{GetType()}] state already exists : {state.StateKey}");
            }
        }

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// 更换状态
        /// </summary>
        /// <param name="state"></param>
        public void ChangeState(TEnum state)
        {
            if (!_states.TryGetValue(state, out IState<TEnum> stateState))
            {
                Debug.LogError($"[{GetType()}] state not exists : {state}");
                return;
            }
            
            _pendingStates.Enqueue(stateState);
            if (_isTransition) return;

            _isTransition = true;
            while (_pendingStates.Count > 0)
            {
                Transit(_pendingStates.Dequeue());
            }
            _isTransition = false;
        }
        
        /// <summary>
        /// 执行状态切换
        /// </summary>
        private void Transit(IState<TEnum> state)
        {
            if (_current == state) return;

            TEnum from = CurrentState;
            _current?.Exit();
            _current = state;

            OnStateChange?.Invoke(from, state.StateKey);

            _current.Enter();
        }
        
        /// <summary>
        /// 停掉正在跑的切换任务并退出当前状态。程序退出时调用，不再进入队列里的下一状态。
        /// </summary>
        public void Stop()
        {
            _pendingStates.Clear();
            _current?.Exit();
            _current = null;
            _isTransition = false;
        }
        
        /// <summary>
        /// 切换任务失败，没有进入目标状态。
        /// </summary>
        public void NotifyTransitionFailed(ProcedureTransition<TEnum> transition)
        {
            OnTransitionFailed?.Invoke(transition);
        }

        #endregion

        #region 生命周期

        public void Update(float deltaTime)
        {
            _current?.Update(deltaTime);
        }

        public void FixedUpdate(float fixedDeltaTime)
        {
            _current?.FixedUpdate(fixedDeltaTime);
        }

        public void LateUpdate()
        {
            _current?.LateUpdate();
        }

        #endregion
    }
}
