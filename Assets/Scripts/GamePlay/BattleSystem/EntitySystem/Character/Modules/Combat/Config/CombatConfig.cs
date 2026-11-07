using System;
using UnityEngine;

namespace GamePlay.BattleSystem
{
    [Serializable]
    public class CombatConfig
    {
        [Header("闪避控制")] 
        public uint rollSkillId;
        
        [Header("普通攻击配置")] 
        public uint primaryAttackSkillId;
        public uint specialAttackSkillId;
        
        // TODO: 优化，释放时才创建对应的技能实例，并定时销毁，纳入对象池进行复用
        [Header("技能ID表")] 
        public uint[] skillIds = {};
    }
}