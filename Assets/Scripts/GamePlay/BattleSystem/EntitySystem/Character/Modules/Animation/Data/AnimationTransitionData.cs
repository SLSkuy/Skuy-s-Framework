using Framework;
using UnityEngine.Animations;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 单个一次性动画过渡数据记录
    /// </summary>
    internal class AnimationTransitionData : IPoolable
    {
        public AnimationClipPlayable clip;
        public float startWeight;
        public float currentWeight;
        
        public void Reset()
        {
            startWeight = 0;
            currentWeight = 0;
        }
    }
}