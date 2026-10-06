using System;
using GamePlay.EntitySystem;

namespace GamePlay.Simulation
{
    /// <summary>
    /// 实体预测状态数据缓存
    /// </summary>
    [Serializable]
    public struct EntityPredictionFrame
    {
        public uint tick;
        public EntityCommand command;
        public EntitySnapshot state;
    }
}
