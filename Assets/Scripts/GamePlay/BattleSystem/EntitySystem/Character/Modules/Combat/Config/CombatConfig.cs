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
        
        [Header("战斗命令优先级（从高到低）")]
        public InputCommandFlags[] commandPriorities =
        {
            InputCommandFlags.Run,
            InputCommandFlags.Roll,
            InputCommandFlags.SkillQ, 
            InputCommandFlags.SkillE,
            InputCommandFlags.SkillX, 
            InputCommandFlags.SpecialAction,
            InputCommandFlags.SpecialAttack,
            InputCommandFlags.PrimaryAttack
        };

        /// <summary>
        /// 创建运行时连招图
        /// </summary>
        public CombatGraph CreateRuntimeCombatGraph()
        {
            return combatGraph.CreateGraph();
        }
    }
}