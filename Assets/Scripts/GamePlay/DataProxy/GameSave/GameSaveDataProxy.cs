using System;
using System.Collections.Generic;
using Core;
using Framework;
using GamePlay.EntitySystem;
using GamePlay.GameData;

namespace GamePlay.DataProxy
{
    /// <summary>
    /// 本机存档数据代理，加入远端时不使用
    /// </summary>
    public class GameSaveDataProxy : BaseDataProxy
    {
        public override string DataPath => "SaveData";
        public override bool IsNeedSaveToLocal => true;

        private readonly Dictionary<Guid, PlayerData> _playerData = new();
        private GameSaveData _gameSave;

        #region 玩家信息管理

        /// <summary>
        /// 按 UUID 取这一局存档里的玩家记录
        /// </summary>
        public bool TryGetPlayerInfo(Guid uuid, out PlayerInfoData playerInfoData)
        {
            if (!_playerData.TryGetValue(uuid, out PlayerData player))
            {
                playerInfoData = null;
                return false;
            }

            playerInfoData = player.info;
            return true;
        }

        /// <summary>
        /// 按 UUID 更新已经记录过的玩家信息
        /// </summary>
        public void UpdatePlayerInfo(PlayerInfoData playerInfo)
        {
            PlayerData player = _playerData[playerInfo.uuid];
            // TODO: 更新记录的玩家信息
            player.info.playerName = playerInfo.playerName;
            // TODO: 更新记录的玩家信息

            // DEBUG模式下不保存随机生成的玩家数据
            if (AppCoreConfig.Instance.debugMode) return;
            MarkDataDirty();
        }
        
        /// <summary>
        /// 按 UUID 记录新的玩家，出场进度用默认值
        /// </summary>
        public void RecordPlayerInfo(PlayerInfoData playerInfo)
        {
            PlayerData player = new() { info = playerInfo };
            _gameSave.players.Add(player);
            _playerData.Add(playerInfo.uuid, player);
            
            // DEBUG模式下不保存随机生成的玩家数据
            if (AppCoreConfig.Instance.debugMode) return;
            MarkDataDirty();
        }

        #endregion

        #region 玩家存档数据管理
        
        /// <summary>
        /// 尝试获取玩家存档中的实体数据，存档中不存在实体数据时返回默认值
        /// </summary>
        public EntitySnapshot GetPlayerSnapshot(Guid uuid)
        {
            if (_playerData.TryGetValue(uuid, out PlayerData player))
            {
                return player.Snapshot;
            }

            return new EntitySnapshot();
        }

        /// <summary>
        /// 保存玩家实体数据
        /// </summary>
        public void RestorePlayerSnapshot(Guid uuid, EntitySnapshot snapshot)
        {
            if (_playerData.TryGetValue(uuid, out PlayerData player))
            {
                player.Snapshot = snapshot;
            }
        }

        #endregion
        
        #region 数据管理方法

        public override void Load()
        {
            if (!Global.LoadJson<GameSaveData>(DataPath, out var data))
            {
                data = new GameSaveData();
                MarkDataDirty();
            }

            _gameSave = data;
            _playerData.Clear();
            foreach (PlayerData player in _gameSave.players)
            {
                _playerData.Add(player.info.uuid, player);
            }
        }

        public override void Save()
        {
            Global.SaveJson(DataPath, _gameSave);
            MarkDataSaved();
        }

        public override void Destroy()
        {
            _playerData.Clear();
            _gameSave = null;
        }

        #endregion
    }
}
