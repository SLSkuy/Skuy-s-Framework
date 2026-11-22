namespace GamePlay.BattleSystem
{
    [System.Serializable]
    public class WeaponCapsuleSkillEffect : SkillEffect
    {
        /// <summary>沿武器根节点局部 +Z 方向的胶囊总长，包含两端半球，至少为两倍半径。</summary>
        public float weaponHeight;

        /// <summary>胶囊体判定半径</summary>
        public float weaponRadius;

        /// <summary>攻击窗口内的检测次数，上限为窗口 Tick 数的一半，单 Tick 窗口检测一次。</summary>
        public int hitRate = 1;
    }
}