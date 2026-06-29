using System;

namespace Framework.FiniteStateMachine
{
    /// <summary>
    /// 状态机状态基类
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    public abstract class StateBase<TEnum> : IState<TEnum> where TEnum : Enum
    {
        public abstract TEnum StateKey { get; }

        protected StateMachine<TEnum> StateMachine;

        protected StateBase(StateMachine<TEnum> stateMachine)
        {
            StateMachine = stateMachine;
        }

        public virtual void Enter()
        {

        }

        public virtual void Exit()
        {

        }

        public virtual void Update(float deltaTime)
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