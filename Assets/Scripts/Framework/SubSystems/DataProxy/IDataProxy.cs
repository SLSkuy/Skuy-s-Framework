namespace Framework
{
    /// <summary>
    /// 数据代理类接口，便于数据代理管理器管理
    /// </summary>
    public interface IDataProxy
    {
        /// <summary>
        /// 数据代理名，由实现类定义，为网络协议同步考虑
        /// </summary>
        string DataName { get; }
        bool IsInitialized { get; }

        /// <summary>
        /// 初始化数据代理，获取存储的数据信息
        /// </summary>
        void _Init();
        
        /// <summary>
        /// 清理当前的所有数据
        /// </summary>
        void _Clear();
    }
}