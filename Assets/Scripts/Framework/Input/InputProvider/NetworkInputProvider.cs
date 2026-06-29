namespace Framework
{
    /// <summary>
    /// 网络输入提供器
    /// 用于接收和同步网络玩家的输入状态
    /// </summary>
    public class NetworkInputProvider : BaseInputProvider
    {
        /// <summary>
        /// 设置输入状态（从网络接收时调用）
        /// </summary>
        public override void SetInputState(InputState state)
        {
            _previousInputState = _currentInputState;
            _currentInputState = state;
            
            // 检查远程输入变换
            CheckInputDifferences();
        }
    }
}
