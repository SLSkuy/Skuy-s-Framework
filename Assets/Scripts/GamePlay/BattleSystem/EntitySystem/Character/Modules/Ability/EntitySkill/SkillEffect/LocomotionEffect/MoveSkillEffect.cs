using System;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 固定速度位移技能效果
    /// </summary>
    [Serializable]
    public sealed class MoveSkillEffect : SkillEffect
    {
        /// <summary> 位移速度 </summary>
        public float speed;
        
        /// <summary> 技能移动方向策略 </summary>
        public SkillLocomotionDirectionPolicy directionPolicy;
        
        /// <summary> 技能碰撞策略 </summary>
        public SkillLocomotionCollisionPolicy collisionPolicy;
    }
}