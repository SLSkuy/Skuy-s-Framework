using System;

namespace GamePlay.PlayerInfo
{
    /// <summary>
    /// 玩家数据抽象，将连接与玩家信息进行绑定
    /// 玩家信息再与游戏数据绑定
    /// </summary>
    public sealed class Player
    {
        #region 属性
        public Guid Uid { get; }
        public string PlayerName { get; }
        public PlayerState State { get; private set; }
        #endregion
    }
}
