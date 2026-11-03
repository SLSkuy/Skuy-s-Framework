using System;
using UnityEngine;

namespace GamePlay.BattleSystem
{
    /// <summary>单个技能的静态配置；加载后只读，施法状态由实例持有。</summary>
    [Serializable]
    public class EntitySkill
    {
        /// <summary> 技能ID </summary>
        public uint skillId;
        
        /// <summary> 技能名称 </summary>
        public string skillName;
        
        /// <summary> 技能冷却 </summary>
        public float cooldown;
        
        /// <summary> 技能花费 </summary>
        public float cost;
        
        /// <summary> 持续时间 </summary>
        public int durationTicks;
        
        /// <summary> 技能释放期间控制命令管理 </summary>
        public SkillControlPolicy skillControlPolicy;
        
        /// <summary> 技能中断规则 </summary>
        public SkillInterruptRule[] interruptRules = Array.Empty<SkillInterruptRule>();
        
        /// <summary> 技能效果 </summary>
        [SerializeReference] public SkillEffect[] effects = Array.Empty<SkillEffect>();

        // Legacy：M13 资产迁移前保留原序列化字段，当前 AbilityModule 仍使用它们。
        [HideInInspector] public float duration;
        [HideInInspector] public AnimationClip clip;
    }
}