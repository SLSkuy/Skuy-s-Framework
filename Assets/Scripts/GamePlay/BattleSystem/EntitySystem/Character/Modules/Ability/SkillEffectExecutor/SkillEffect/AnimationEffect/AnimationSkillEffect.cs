using System;
using UnityEngine;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 技能动画效果
    /// </summary>
    [Serializable]
    public class AnimationSkillEffect : SkillEffect
    {
        //// <summary> 动画播放层级 </summary>
        // public int layer;    // TODO: 暂不考虑动画播放层级
        
        /// <summary> 采样偏移 </summary>
        public float sampleOffset;
        
        /// <summary> 切入此动作的混合时长（秒） </summary>
        [Min(0f)]
        public float blendIn = 0.15f;

        /// <summary> 从此动作回到移动动画的混合时长（秒） </summary>
        [Min(0f)]
        public float blendOut = 0.15f;
        
        /// <summary> 动画片段源文件 </summary>
        public AnimationClip clip;
    }
}
