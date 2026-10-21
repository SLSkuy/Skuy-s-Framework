using System;
using GamePlay.EntitySystem;

namespace GamePlay.Simulation
{
    /// <summary>
    /// 玩家预测帧状态
    /// </summary>
    [Serializable]
    public struct PlayerPredictionFrame
    {
        public uint tick;
        public EntityCommand command;
        public EntitySnapshot snapshot;
    }
}
