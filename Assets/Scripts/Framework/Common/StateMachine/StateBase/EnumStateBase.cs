using System;

namespace Framework.StateMachine
{
    /// <summary>
    /// 状态机状态基类
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    public abstract class EnumStateBase<TEnum> : IEnumState where TEnum : Enum
    {
        public abstract int StateKey { get; }

        protected EnumStateMachine<TEnum> _stateMachine;

        protected EnumStateBase(EnumStateMachine<TEnum> stateMachine)
        {
            _stateMachine = stateMachine;
        }

        public virtual void Enter()
        {

        }

        public virtual void Exit()
        {

        }

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