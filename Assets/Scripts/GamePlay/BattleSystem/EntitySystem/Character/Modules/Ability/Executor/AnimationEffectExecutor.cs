namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 技能动画效果处理器，处理技能释放期间的所有角色动画播放
    /// </summary>
    public class AnimationEffectExecutor : BaseSkillEffectExecutor
    {
        private readonly AnimationModule _animation;
        private readonly int _tickRate;

        public AnimationEffectExecutor(EntityContext context, int tickRate) : base(context)
        {
            _animation = context.Get<AnimationModule>();
            _tickRate = tickRate;
        }

        public override void ExecuteSkillEffect(SkillEffect effect)
        {
            if (effect is AnimationSkillEffect animationEffect)
            {
                // 动画时间转换，动画的播放不采用Tick
                float duration = AbilityUtils.TicksToSeconds(effect.durationTicks, _tickRate);
                
                _animation.PlayActionAnimation(new EntityAnimationRequest
                {
                    clip = animationEffect.clip,
                    sampleOffset = animationEffect.sampleOffset,
                    duration = duration
                });
            }
        }

        public override void StopExecution()
        {
            _animation.InterruptActionAnimation();
        }
    }
}

