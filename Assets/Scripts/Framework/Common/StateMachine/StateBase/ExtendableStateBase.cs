using System;

namespace Framework
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