using System;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 单个预测 Tick 的输入命令与模拟后完整状态。
    /// </summary>
    [Serializable]
    public struct EntityPredictionFrame
    {
        public uint Tick;
        public EntityInputCommand Command;
        public EntityRollbackState State;
    }
}
