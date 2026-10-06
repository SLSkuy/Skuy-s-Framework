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
        private readonly Queue<StateTransition<TEnum>> _pendingTransitions = new();
        private StateTransition<TEnum> _currentTransition;

        private IState<TEnum> _current;
        private bool _isDraining;
        private bool _isRunningTask;

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
        /// 切换任务失败，没有进入 <see cref="StateTransition{TEnum}.NextState"/>
        /// 状态停留在当前状态中
        /// </summary>
        public event Action<StateTransition<TEnum>> OnTransitionFailed;
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
        /// 请求切换。带 <see cref="ILoadTask"/> 时，任务完成之后才退出当前状态并进入下一状态。
        /// </summary>
        public void RequestState(StateTransition<TEnum> transition)
        {
            if (!_states.ContainsKey(transition.NextState))
            {
                Debug.LogError($"[{GetType()}] state not exists : {transition.NextState}");
                return;
            }

            _pendingTransitions.Enqueue(transition);
            Drain();
        }

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// 更换状态
        /// </summary>
        /// <param name="state"></param>
        public void ChangeState(TEnum state)
        {
            RequestState(new StateTransition<TEnum>(state, null));
        }

        /// <summary>
        /// 停掉正在跑的切换任务并退出当前状态。程序退出时调用，不再进入队列里的下一状态。
        /// </summary>
        public void Stop()
        {
            _pendingTransitions.Clear();
            StopTask();

            _current?.Exit();
            _current = null;
        }

        private void Drain()
        {
            if (_isDraining || _isRunningTask) return;

            _isDraining = true;
            while (_pendingTransitions.Count > 0 && !_isRunningTask)
            {
                _currentTransition = _pendingTransitions.Dequeue();
                ILoadTask task = _currentTransition.Task;
                if (task == null)
                {
                    Transit(_currentTransition.NextState);
                    continue;
                }

                _isRunningTask = true;
                task.Finished += OnTaskFinished;
                task.Start();
            }

            _isDraining = false;
        }

        private void OnTaskFinished()
        {
            ILoadTask task = _currentTransition.Task;
            task.Finished -= OnTaskFinished;
            bool failed = task.IsFailed;
            task.Stop();
            _isRunningTask = false;

            if (failed)
            {
                OnTransitionFailed?.Invoke(_currentTransition);
            }
            else
            {
                Transit(_currentTransition.NextState);
            }

            if (_isDraining) return;
            Drain();
        }

        /// <summary>
        /// 执行状态切换
        /// </summary>
        private void Transit(TEnum state)
        {
            IState<TEnum> next = _states[state];
            if (_current == next) return;

            TEnum from = CurrentState;
            _current?.Exit();
            _current = next;

            OnStateChange?.Invoke(from, state);

            _current.Enter();
        }

        private void StopTask()
        {
            if (!_isRunningTask) return;

            ILoadTask task = _currentTransition.Task;
            task.Finished -= OnTaskFinished;
            task.Stop();
            _isRunningTask = false;
        }

        #endregion

        #region 生命周期

        public void Update(float deltaTime)
        {
            if (_isRunningTask)
            {
                _currentTransition.Task.Update(deltaTime);
            }

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
