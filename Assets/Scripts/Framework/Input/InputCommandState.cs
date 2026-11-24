namespace Framework
{
    /// <summary>
    /// 输入命令状态
    /// </summary>
    public enum InputCommandState
    {
        /// <summary>
        /// 没有命令事件
        /// </summary>
        None,
        
        /// <summary>
        /// 按下
        /// </summary>
        Pressed,
        
        /// <summary>
        /// 按住
        /// </summary>
        Held,
        
        /// <summary>
        /// 释放
        /// </summary>
        Released,
    }
}