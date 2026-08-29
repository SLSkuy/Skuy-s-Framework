using System;

namespace Network
{
    /// <summary>
    /// 连接层事件。Removed 表示连接对象已被彻底移除（含宽限期结束）。
    /// </summary>
    public interface IConnectionEvents
    {
        event Action<uint> ClientRemoved;
    }
}
