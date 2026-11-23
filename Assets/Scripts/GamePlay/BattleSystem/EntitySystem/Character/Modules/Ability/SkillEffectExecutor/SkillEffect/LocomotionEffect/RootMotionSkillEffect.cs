using System;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 根运动移动技能效果
    /// </summary>
    [Serializable]
    public class RootMotionSkillEffect : SkillEffect
    {
        /// <summary>根运动烘焙数据</summary>
        public BakedAnimMotionData motionData;
        
        /// <summary>技能碰撞策略</summary>
        public SkillLocomotionCollisionPolicy collisionPolicy;
    }
}