using Framework;

namespace Network
{
    /// <summary>
    /// 客户端网路连接交互封装
    /// </summary>
    public class NetClient : SubSystemBase
    {
        public override SubSystemPriority Priority => SubSystemPriority.NetWorkManager;
    }
}
