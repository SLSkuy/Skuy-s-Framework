namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体状态只读视图，提供最基础的状态获取
    /// 用于同步、动画和调试读取实体状态
    /// </summary>
    public interface IEntityStateView
    {
        /// <summary>
        /// 当前状态编号。
        /// </summary>
        uint CurrentState { get; }
    }
}

