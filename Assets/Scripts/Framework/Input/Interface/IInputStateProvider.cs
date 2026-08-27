namespace Framework
{
    /// <summary>
    /// 输入状态提供器接口，定义玩家的输入数据结构获取方法
    /// </summary>
    public interface IInputStateProvider
    {
        /// <summary>
        /// 获取当前的输入状态
        /// </summary>
        /// <returns>输入状态</returns>
        InputState GetInputState();
    }
}