using UnityEngine;

namespace GamePlay.BattleSystem
{
    [System.Serializable]
    public class WeaponCapsuleSkillEffect : SkillEffect
    {
        /// <summary>沿武器根节点局部 +Z 方向的胶囊总长，包含两端半球，至少为两倍半径。</summary>
        [Min(0f)] public float weaponHeight;

        /// <summary>胶囊体判定半径</summary>
        [Min(0f)] public float weaponRadius;

        /// <summary>攻击窗口内的检测次数，次数最多为攻击窗口Tick数</summary>
        [Min(1)] public int hitRate = 1;
    }
}