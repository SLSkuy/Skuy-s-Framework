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
        
        public ControlPolicy controlPolicy;
        
        /// <summary> 技能中断规则 </summary>
        public SkillInterruptRule[] interruptRules = Array.Empty<SkillInterruptRule>();
        
        /// <summary> 释放条件，必须全部满足；空数组表示无额外条件 </summary>
        [SerializeReference] public ICastCondition[] castConditions = Array.Empty<ICastCondition>();

        /// <summary> 技能效果 </summary>
        [SerializeReference] public SkillEffect[] effects = Array.Empty<SkillEffect>();
    }
}