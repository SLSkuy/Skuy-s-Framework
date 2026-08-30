namespace GamePlay.Battle
{
    /// <summary>
    /// 战局内的玩家实体，标识独立于传输连接。
    /// </summary>
    public sealed class BattlePlayer
    {
        public BattlePlayer(uint playerId, uint clientId)
        {
            PlayerId = playerId;
            ClientId = clientId;
            State = BattlePlayerState.Active;
        }

        #region 属性
        public uint PlayerId { get; }
        public uint ClientId { get; }
        public BattlePlayerState State { get; internal set; }
        #endregion
    }
}
