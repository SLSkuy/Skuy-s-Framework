namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 技能效果事件
    /// </summary>
    public struct SkillEffectEvent
    {
        public uint Tick;
        
        /// <summary>
        /// 技能效果索引，对应技能上的效果列表
        /// </summary>
        public int EffectIndex;
        
        public SkillEffectEventType Type;
    }
}