using System;

namespace Framework
{
    /// <summary>
    /// 状态机状态基类
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    public abstract class StateBase<TEnum> : IState<TEnum> where TEnum : Enum
    {
        public abstract TEnum StateKey { get; }

        protected readonly StateMachine<TEnum> _stateMachine;

        protected StateBase(StateMachine<TEnum> stateMachine)
        {
            _stateMachine = stateMachine;
        }

        /// <summary>
        /// 进入状态，每次进入状态时调用一次
        /// </summary>
        public virtual void Enter()
        {

        }

        /// <summary>
        /// 退出状态，每次退出状态时调用一次
        /// </summary>
        public virtual void Exit()
        {

        }

        /// <summary>
        /// 状态更新
        /// </summary>
        public void Update(float deltaTime)
        {
            Tick(deltaTime);
            CheckStateChange();
        }

        /// <summary>
        /// 状态机Tick逻辑处理
        /// </summary>
        protected virtual void Tick(float deltaTime)
        {
            
        }

        /// <summary>
        /// 检测状态变换
        /// </summary>
        protected virtual void CheckStateChange()
        {
            
        }

        public virtual void FixedUpdate(float fixedDeltaTime)
        {

        }

        public virtual void LateUpdate()
        {

        }
    }
}