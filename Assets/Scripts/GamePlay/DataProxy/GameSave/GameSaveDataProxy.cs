using System;
using System.Linq;
using Framework;
using GamePlay.GameSave;

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

        /// <summary>
        /// 尝试获取玩家信息
        /// </summary>
        public bool TryGetPlayerInfo(Guid uuid, out PlayerData playerData)
        {
            foreach (var player in _gameSave.players.Where(player => player.uuid == uuid))
            {
                playerData = player;
                return true;
            }

            playerData = null;
            return false;
        }

        /// <summary>
        /// 按 UUID 更新已经记录过的玩家数据
        /// </summary>
        public void UpdatePlayer(PlayerData player)
        {
            foreach (var t in _gameSave.players.Where(t => t.uuid == player.uuid))
            {
                t.UpdateInfo(player);
                MarkDataDirty();
                return;
            }
        }
        
        /// <summary>
        /// 按 UUID 记录新的玩家
        /// </summary>
        public void RecordPlayer(PlayerData player)
        {
            _gameSave.players.Add(player);
            MarkDataDirty();
        }

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
