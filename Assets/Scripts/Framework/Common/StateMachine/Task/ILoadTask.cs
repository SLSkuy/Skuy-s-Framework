using System;

namespace Framework
{
    /// <summary>
    /// 状态机切换工作，可在状态切换之间插入流程，流程完成之后才会进行状态切换
    /// </summary>
    public interface ILoadTask
    {
        #region 事件
        /// <summary>
        /// 这段工作结束。成功与失败都发，用 <see cref="IsFailed"/> 区分。
        /// </summary>
        event Action Finished;
        #endregion

        bool IsFailed { get; }

        /// <summary>
        /// 开始这段工作。当场就能完成时在这里直接发 <see cref="Finished"/>。
        /// </summary>
        void Start();

        /// <summary>
        /// 推进这段工作。靠回调判完成的工作不需要在这里做事。
        /// </summary>
        void Update(float deltaTime);

        /// <summary>
        /// 拆掉这段工作留下的订阅。
        /// </summary>
        void Stop();
    }
}
