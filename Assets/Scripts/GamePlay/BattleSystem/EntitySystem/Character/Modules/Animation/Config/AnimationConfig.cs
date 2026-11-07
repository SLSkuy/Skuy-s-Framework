using System;
using UnityEngine;

namespace GamePlay.BattleSystem
{
    [Serializable]
    public class AnimationConfig
    {
        public RuntimeAnimatorController animatorController;
        
        [Min(0f)] public float animSpeedSmoothTime = 0.1f;  // 仅动画 Speed 参数向期望速度过渡的平滑时间（秒），0 表示立即切换
        [Min(0f)] public float animTurnSmoothTime = 0.1f; // Turn 倾斜参数的指数平滑时间（秒），0 表示立即切换
    }
}