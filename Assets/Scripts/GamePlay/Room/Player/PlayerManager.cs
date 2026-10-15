using System;
using System.Collections.Generic;
using Framework;
using GamePlay.DataProxy;
using GamePlay.GameSave;

namespace GamePlay.Room
{
    /// <summary>
    /// 这一局在场的玩家，只持有本局进入过的玩家信息
    /// 完成ConnectionID -> UUID的转换，对业务逻辑屏蔽连接ID
    /// </summary>
    public class PlayerManager : IDisposable
    {
        private readonly GameSaveDataProxy _gameSave = Global.GetDataProxy<GameSaveDataProxy>();
        
        private readonly Dictionary<Guid, Player> _playersByUUID = new();
        private readonly Dictionary<uint, Player> _playersByConnectionId = new();
        
        #region 属性
        public int PlayerCount => _playersByUUID.Count;
        #endregion

        /// <summary>
        /// 通过UUID获取当前局内的玩家信息
        /// </summary>
        public bool TryGetPlayer(Guid uuid, out Player player)
        {
            return _playersByUUID.TryGetValue(uuid, out player);
        }

        public bool TryGetPlayer(uint connectionId, out Player player)
        {
            return _playersByConnectionId.TryGetValue(connectionId, out player);
        }

        public PlayerData[] GetActivePlayerInfos()
        {
            List<PlayerData> infos = new List<PlayerData>();
            foreach (var p in _playersByUUID.Values)
            {
                if (p.State == PlayerState.Active)
                {
                    infos.Add(new PlayerData(p));
                }
            }
            return infos.ToArray();
        }

        /// <summary>
        /// 客户端添加远端玩家数据
        /// 客户端不需要使用connectionID
        /// </summary>
        public void AddPlayerClient(PlayerData player)
        {
            Player p = new Player(player);
            p.Active();
            
            _playersByUUID.Add(p.UUID, p);
        }

        /// <summary>
        /// 添加玩家局内实例记录，服务端使用
        /// 同时更新存档数据
        /// </summary>
        public void AddPlayerServer(uint connectionId, PlayerData player)
        {
            // 判断是否加入过对局，获取数据缓存
            if (_gameSave.TryGetPlayerInfo(player.uuid, out PlayerData info))
            {
                // 更新缓存的玩家数据
                _gameSave.UpdatePlayer(player);
            }
            else
            {
                // 未加入过对局，更新玩家记录
                _gameSave.RecordPlayer(player);
            }
            
            Player p = new Player(player);
            p.Active();
            
            _playersByUUID.Add(player.uuid, p);
            _playersByConnectionId.Add(connectionId, p);
        }

        public void RemovePlayerClient(Guid uuid)
        {
            if (!TryGetPlayer(uuid, out Player p)) return;
            p.Inactive();
            
            _playersByUUID.Remove(uuid);
        }

        /// <summary>
        /// 删除玩家局内实例
        /// </summary>
        public Guid RemovePlayerServer(uint connectionId)
        {
            if (!TryGetPlayer(connectionId, out Player p)) return Guid.Empty;
            
            Guid uuid = _playersByConnectionId[connectionId].UUID;
            p.Inactive();
            
            _playersByUUID.Remove(uuid);
            _playersByConnectionId.Remove(connectionId);
            return uuid;
        }
        
        public void Dispose()
        {
            _playersByUUID.Clear();
            _playersByConnectionId.Clear();
        }
    }
}
