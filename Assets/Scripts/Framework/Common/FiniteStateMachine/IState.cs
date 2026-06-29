using System;

namespace Framework.FiniteStateMachine
{
    /// <summary>
    /// 状态机状态接口，根据不同枚举创建不同的状态基类
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    public interface IState<TEnum> where TEnum : Enum
    {
        TEnum StateKey { get; }

        void Enter();

        void Exit();

        void Update(float deltaTime);

        void FixedUpdate(float fixedDeltaTime);

        void LateUpdate();
    }
}