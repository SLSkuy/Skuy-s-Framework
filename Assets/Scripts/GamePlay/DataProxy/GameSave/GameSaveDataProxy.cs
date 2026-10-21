using System;
using System.Linq;
using Core;
using Framework;
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

        private GameSaveData _gameSave;

        #region 玩家信息处理

        /// <summary>
        /// 按 UUID 取这一局存档里的玩家记录
        /// </summary>
        public bool TryGetPlayerInfo(Guid uuid, out PlayerInfoData playerInfoData)
        {
            foreach (var player in _gameSave.players.Where(player => player.info.uuid == uuid))
            {
                playerInfoData = player.info;
                return true;
            }

            playerInfoData = null;
            return false;
        }

        /// <summary>
        /// 按 UUID 更新已经记录过的玩家信息
        /// </summary>
        public void UpdatePlayerInfo(PlayerInfoData playerInfo)
        {
            foreach (var player in _gameSave.players.Where(player => player.info.uuid == playerInfo.uuid))
            {
                // TODO: 更新记录的玩家信息
                player.info.playerName = playerInfo.playerName;
                // TODO: 更新记录的玩家信息
                
                MarkDataDirty();
                return;
            }
        }
        
        /// <summary>
        /// 按 UUID 记录新的玩家，出场进度用默认值
        /// </summary>
        public void RecordPlayerInfo(PlayerInfoData playerInfo)
        {
            // DEBUG模式下不保存随机生成的玩家数据
            if (AppCoreConfig.Instance.debugMode) return;
            
            _gameSave.players.Add(new PlayerData { info = playerInfo });
            MarkDataDirty();
        }

        #endregion
        
        #region 数据管理方法

        public override void Load()
        {
            if (Global.LoadJson<GameSaveData>(DataPath, out var data))
            {
                _gameSave = data;
                return;
            }

            _gameSave = new GameSaveData();
            MarkDataDirty();
        }

        public override void Save()
        {
            Global.SaveJson(DataPath, _gameSave);
        }

        public override void Destroy()
        {
            _gameSave = null;
        }

        #endregion
    }
}
