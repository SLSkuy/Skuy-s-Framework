namespace Framework.StateMachine
{
    /// <summary>
    /// 枚举状态机状态接口，根据不同枚举创建不同的状态基类
    /// </summary>
    public interface IEnumState : IExtendableState<int>
    {
        
    }
}