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
    }
}