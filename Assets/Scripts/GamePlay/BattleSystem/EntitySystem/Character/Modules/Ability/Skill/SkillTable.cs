using System;
using UnityEngine;

namespace GamePlay.BattleSystem
{
    [CreateAssetMenu(menuName = "GamePlay/SkillTable")]
    public class SkillTable : ScriptableObject
    {
        public SkillConfig[] skills = Array.Empty<SkillConfig>();
    }
}