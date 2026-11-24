using UnityEngine;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 实体播放动画请求
    /// </summary>
    public struct EntityAnimationRequest
    { 
        public AnimationClip clip;
        public float sampleOffset;
        public float duration;

        /// <summary> 切入动作的混合时长（秒） </summary>
        public float blendIn;

        /// <summary> 回到移动动画的混合时长（秒） </summary>
        public float blendOut;
    }
}
