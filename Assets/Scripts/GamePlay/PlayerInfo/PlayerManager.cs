using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Framework;

namespace GamePlay.PlayerInfo
{
    /// <summary>
    /// 玩家数据管理，接收连接抽象
    /// 接收到玩家的连接数据后，创建玩家数据实例，再进行业务逻辑处理
    /// 网络连接与业务的桥接层
    /// </summary>
    public class PlayerManager : SubSystemBase
    {
        private Dictionary<Guid, Player> _playersByUid = new();
        
        private PlayerManagerClientHandler _clientHandler;
        private PlayerManagerServerHandler _serverHandler;

        /// <summary>
        /// UID生成器
        /// </summary>
        private MD5 _uidGenerator;

        #region 属性

        public override int Priority => (int)GamePlaySystemPriority.PlayerManager;

        #endregion

        #region UID管理

        /// <summary>
        /// 生成玩家UID
        /// </summary>
        /// <param name="playerName">玩家名称</param>
        public Guid GenerateUID(string playerName)
        {
            byte[] hash = _uidGenerator.ComputeHash(Encoding.UTF8.GetBytes(playerName));
            return new Guid(hash);
        }

        #endregion

        #region 子系统生命周期

        public override void Init()
        {
            
        }

        public override void Destroy()
        {
            _playersByUid.Clear();
            _playersByUid = null;
            
            _clientHandler = null;
            _serverHandler = null;
        }

        #endregion
    }
}