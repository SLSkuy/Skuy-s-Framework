namespace GamePlay.Battle
{
    /// <summary>
    /// 房间内的玩家实体，标识独立于传输连接。
    /// </summary>
    public sealed class BattlePlayer
    {
        public BattlePlayer(uint playerId)
        {
            PlayerId = playerId;
            State = BattlePlayerState.Active;
        }

        #region 属性
        public uint PlayerId { get; }
        public BattlePlayerState State { get; internal set; }
        #endregion
    }
}
