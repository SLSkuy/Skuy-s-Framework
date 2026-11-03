using System;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 技能效果数据配置
    /// </summary>
    [Serializable]
    public abstract class SkillEffect
    {
        /// <summary> 技能效果ID </summary>
        public uint effectId;
        
        /// <summary> 技能效果释放Tick </summary>
        public int startTick;
        
        /// <summary> 技能效果持续Tick </summary>
        public int durationTicks;
        
        /// <summary> 技能效果排序 </summary>
        public int order;
        
        /// <summary> 技能效果结束策略 </summary>
        public SkillEffectEndPolicy endPolicy;
    }
}