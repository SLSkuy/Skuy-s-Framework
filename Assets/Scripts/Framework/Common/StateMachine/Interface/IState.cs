namespace Framework
{
    /// <summary>
    /// 状态接口
    /// </summary>
    public interface IState
    {
        /// <summary>
        /// 进入状态
        /// </summary>
        void Enter();

        /// <summary>
        /// 离开状态
        /// </summary>
        void Exit();

        /// <summary>
        /// 每帧更新
        /// </summary>
        void Update(float deltaTime);

        /// <summary>
        /// 固定补偿更新
        /// </summary>
        void FixedUpdate(float fixedDeltaTime);

        /// <summary>
        /// 延迟更新
        /// </summary>
        void LateUpdate();
    }
}