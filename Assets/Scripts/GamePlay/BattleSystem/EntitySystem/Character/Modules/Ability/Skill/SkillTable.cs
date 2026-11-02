using UnityEngine;

namespace GamePlay.BattleSystem
{
    [CreateAssetMenu(menuName = "GamePlay/SkillTable")]
    public class SkillTable : ScriptableObject
    {
        public BaseSkill[] skills;
    }
}