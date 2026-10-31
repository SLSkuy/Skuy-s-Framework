namespace Framework
{
    /// <summary>
    /// 由所属业务模块管理绑定周期的网络消息处理器。
    /// </summary>
    public interface IHandler
    {
        void Bind();
        void Unbind();
    }
}
