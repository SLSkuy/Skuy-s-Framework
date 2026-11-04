namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 技能轨道运行状态
    /// </summary>
    public enum SkillTrackState
    {
        /// <summary>
        /// 初始化，刚更新即将播放的技能配置
        /// </summary>
        Init,
        
        /// <summary>
        /// 播放中
        /// </summary>
        Running,
        
        /// <summary>
        /// 暂停
        /// </summary>
        Paused,
        
        /// <summary>
        /// 技能被打断
        /// </summary>
        Interrupt,
        
        /// <summary>
        /// 技能播放完成
        /// </summary>
        Complete,
    }
}