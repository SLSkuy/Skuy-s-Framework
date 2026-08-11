namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 可插值同步组件接口，用于 Replica 角色消费远端快照并推进渲染状态。
    /// </summary>
    public interface INetInterpolatedSync<TSnapshot> where TSnapshot : struct, IEntitySnapshot
    {
        /// <summary>
        /// 接收权威快照。
        /// </summary>
        void OnAuthoritySnapshot(in TSnapshot snapshot);

        /// <summary>
        /// 按渲染帧推进插值。
        /// </summary>
        void UpdateInterpolation(float deltaTime);
    }
}
