using System;
using UnityEngine;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 通用技能配置，静态存在，加载后只读
    /// </summary>
    [Serializable]
    public class SkillConfig
    {
        /// <summary> 技能ID </summary>
        public uint skillId;
        
        /// <summary> 技能名称 </summary>
        public string skillName;
        
        /// <summary> 技能冷却时间 </summary>
        public float cooldown;
        
        //// <summary> 技能花费 </summary>
        // public float cost;
        
        /// <summary> 持续时间 </summary>
        public uint durationTicks;
        
        /// <summary> 技能中断规则 </summary>
        public SkillInterruptRule[] interruptRules = Array.Empty<SkillInterruptRule>();
        
        /// <summary> 技能效果 </summary>
        [SerializeReference] public SkillEffect[] effects = Array.Empty<SkillEffect>();
    }
}