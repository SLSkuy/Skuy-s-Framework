using System;
using UnityEngine;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 方形伤害范围技能效果
    /// </summary>
    [Serializable]
    public class HitBoxSkillEffect : SkillEffect
    {
        /// <summary>相对角色位置和朝向的盒中心偏移</summary>
        public Vector3 hitBoxPivot;
        
        /// <summary>相对角色朝向的盒旋转</summary>
        public Quaternion hitBoxRotation = Quaternion.identity;

        /// <summary>盒的完整尺寸</summary>
        public Vector3 hitBoxSize;
    }
}
