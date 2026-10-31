using System;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 实体最基础状态快照
    /// </summary>
    [Serializable]
    public struct StateSnapshot
    {
        public EntityState entityState;    // 实体当前状态（待机、死亡等）
        
        public float locomotionSpeed;      // 目标期望移动速度
        public float rollRemainingTime;    // 翻滚剩余模拟时间（秒）
        
        public bool isRunning;
        public bool isSprinting;

        public bool isFocus;
    }
}