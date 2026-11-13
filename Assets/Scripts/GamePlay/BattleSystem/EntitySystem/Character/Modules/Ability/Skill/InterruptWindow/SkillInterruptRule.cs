using System;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 技能中断窗口配置
    /// </summary>
    [Serializable]
    public sealed class SkillInterruptRule
    {
        /// <summary> 中断窗口开始时间 </summary>
        public int startTick;
        
        /// <summary> 中断窗口结束时间 </summary>
        public int endTick;
        
        /// <summary> 中断条件 </summary>
        public SkillInterruptReason reason;
    }
}