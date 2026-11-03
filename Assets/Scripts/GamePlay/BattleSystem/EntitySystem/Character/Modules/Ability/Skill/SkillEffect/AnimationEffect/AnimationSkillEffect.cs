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
        /// <summary> 动画播放层级 </summary>
        public int layer;
        
        /// <summary> 采样偏移 </summary>
        public float sampleOffset;
        
        /// <summary> 动画过渡时间 </summary>
        public float transitionTime = 0.15f;
        
        /// <summary> 动画片段源文件 </summary>
        public AnimationClip clip;
    }
}