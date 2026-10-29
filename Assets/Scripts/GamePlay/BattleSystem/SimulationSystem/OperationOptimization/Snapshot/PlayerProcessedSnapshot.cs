namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 玩家在某一Tick上模拟处理完成后的Tick帧状态
    /// </summary>
    public struct PlayerProcessedSnapshot
    {
        public uint entityId;
        public uint lastProcessedInputTick;
        public EntitySnapshot snapshot;
    }
}
