using System;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 玩家预测帧状态
    /// </summary>
    [Serializable]
    public struct PlayerPredictionSnapshot
    {
        public uint tick;
        public EntityControlCommand command;
        public EntitySnapshot snapshot;
    }
}
