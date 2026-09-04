using System;

namespace Framework
{
    /// <summary>
    /// 可拓展状态机状态接口，可通过拓展TKey实现状态的拓展
    /// </summary>
    public interface IExtendableState<TKey> : IState where TKey : IEquatable<TKey>
    {
        TKey StateKey { get; }
    }
}