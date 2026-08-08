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