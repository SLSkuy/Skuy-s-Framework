using Framework;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 客户端预测同步入口。
    /// </summary>
    public interface INetPredictionModule<TSnapshot> where TSnapshot : struct, IEntitySnapshot
    {
        /// <summary>
        /// 记录预测快照。
        /// </summary>
        void RecordPrediction(uint inputTick, in InputState input, in TSnapshot snapshot);

        /// <summary>
        /// 接收权威快照并请求回放。
        /// </summary>
        void AcceptAuthoritySnapshot(in TSnapshot snapshot, float tickDeltaTime);
    }
}
