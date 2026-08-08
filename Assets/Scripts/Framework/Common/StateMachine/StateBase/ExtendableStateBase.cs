using System;

namespace Framework.StateMachine
{
    /// <summary>
    /// 可拓展状态机基类
    /// </summary>
    public abstract class ExtendableStateBase<TKey> : IExtendableState<TKey> where TKey : IEquatable<TKey>
    {
        public abstract TKey StateKey { get; }
        
        protected ExtendableStateMachine<TKey> _stateMachine;
        
        protected ExtendableStateBase(ExtendableStateMachine<TKey> stateMachine)
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