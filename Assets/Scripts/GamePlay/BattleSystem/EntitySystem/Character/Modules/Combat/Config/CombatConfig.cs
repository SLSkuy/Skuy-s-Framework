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
    }
}