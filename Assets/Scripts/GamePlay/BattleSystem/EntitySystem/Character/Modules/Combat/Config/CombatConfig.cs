using System;
using Framework;
using UnityEngine;

namespace GamePlay.BattleSystem
{
    [Serializable]
    public class CombatConfig
    {
        [Header("连招图")]
        public CombatGraphConfig combatGraph;
        
        [HideInInspector][Header("战斗命令优先级（从高到低）")]
        public EntityCommandFlags[] commandPriorities =
        {
            EntityCommandFlags.Roll, 
            EntityCommandFlags.SkillQ, 
            EntityCommandFlags.SkillE,
            EntityCommandFlags.SkillX, 
            EntityCommandFlags.SpecialAction,
            EntityCommandFlags.SpecialAttack,
            EntityCommandFlags.PrimaryAttack
        };
    }
}