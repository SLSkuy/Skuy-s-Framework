namespace Framework
{
    /// <summary>
    /// 进入对象池的对象。还池前由对象池调用 <see cref="Reset"/>，清掉本次借出留下的状态。
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// 清掉本次使用留下的状态，使对象可以再次借出。
        /// </summary>
        void Reset();
    }
}
