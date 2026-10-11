using Framework;

namespace GamePlay.GameSave
{
    /// <summary>
    /// 玩家数据代理，集中获取玩家相关数据
    /// </summary>
    public class PlayerDataProxy : BaseDataProxy
    {
        public override string DataPath => "PlayerData";
        public override bool IsNeedSaveToLocal => true;

        private PlayerInfo _playerInfo;
        
        #region 属性
        public string PlayerName => _playerInfo.playerName;
        #endregion

        public void Rename(string playerName)
        {
            MarkDataDirty();
            
            _playerInfo.playerName = playerName;
        }

        #region 数据管理方法

        public override void Load()
        {
            if (Global.LoadJson<PlayerInfo>(DataPath, out var data))
            {
                _playerInfo = data;
                return;
            }
            
            _playerInfo = new PlayerInfo();
        }

        public override void Save()
        {
            Global.SaveJson(DataPath, _playerInfo);
        }

        #endregion
    }
}