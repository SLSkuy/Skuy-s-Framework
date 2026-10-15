using System;
using Core;
using Framework;
using GamePlay.GameSave;

namespace GamePlay.DataProxy
{
    /// <summary>
    /// 本机玩家数据代理，记录处理本机玩家的数据信息
    /// </summary>
    public class PlayerDataProxy : BaseDataProxy
    {
        public override string DataPath => "PlayerData";
        public override bool IsNeedSaveToLocal => true;

        private PlayerData _playerData;
        
        #region 属性
        public PlayerData LocalPlayerData => _playerData;
        public Guid UUID => _playerData.uuid;
        public string PlayerName => _playerData.playerName;
        #endregion

        /// <summary>
        /// 本机玩家重命名角色名称
        /// </summary>
        public void Rename(string playerName)
        {
            _playerData.playerName = playerName;
            
            MarkDataDirty();
        }

        #region 数据管理方法

        public override void Load()
        {
            if (AppCoreConfig.Instance.debugMode)
            {
                // DEBUG模式下随机生成UUID，防止因UUID相同导致的冲突问题
                _playerData = new PlayerData { uuid = Guid.NewGuid() };
                return;
            }
            
            if (!Global.LoadJson<PlayerData>(DataPath, out var data))
            {
                // 不存在数据文件时，创建新的数据配置
                data = new PlayerData();
            }

            _playerData = data;
            if (_playerData.uuid != Guid.Empty) return;

            // 生成本机玩家UUID
            _playerData.uuid = Guid.NewGuid();
            MarkDataDirty();
        }

        public override void Save()
        {
            if (!AppCoreConfig.Instance.debugMode)
            {
                // DEBUG模式下不保存随机生成的玩家数据
                Global.SaveJson(DataPath, _playerData);    
            }
        }

        public override void Destroy()
        {
            _playerData = null;
        }

        #endregion
    }
}