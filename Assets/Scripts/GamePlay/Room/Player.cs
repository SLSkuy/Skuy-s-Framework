namespace GamePlay.Room
{
    /// <summary>
    /// 对局名册中的玩家，标识独立于传输连接。
    /// </summary>
    public sealed class Player
    {
        public Player(uint playerId)
        {
            PlayerId = playerId;
            State = PlayerState.Active;
        }

        #region 属性
        public uint PlayerId { get; }
        public PlayerState State { get; internal set; }
        #endregion
    }
}
