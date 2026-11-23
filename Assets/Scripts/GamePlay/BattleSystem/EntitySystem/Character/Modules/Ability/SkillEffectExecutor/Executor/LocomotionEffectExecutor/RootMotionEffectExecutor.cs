using UnityEngine;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 根运动技能效果执行器
    /// </summary>
    public class RootMotionEffectExecutor : BaseSkillEffectExecutor
    {
        private readonly MovementModule _movement;
        private BakedAnimMotionData _motionData;

        private uint _elapsedTick;
        private bool _isMoving;
        
        public RootMotionEffectExecutor(EntityContext context) : base(context)
        {
            _movement = context.Get<MovementModule>();
        }

        public override void ExecuteSkillEffect(SkillEffect effect)
        {
            if (effect is RootMotionSkillEffect motionEffect)
            {
                _motionData = motionEffect.motionData;
                _isMoving = true;
            }
        }

        public override void Tick(float deltaTime)
        {
            if (_isMoving)
            {
                Vector2 deltaPosXZ = _motionData.EvaluateTickDeltaPosXZ(_elapsedTick);
                _movement.Move(Vector2.up, deltaPosXZ, deltaTime);
            }
        }

        public override void StopExecution()
        {
            _isMoving = false;
            _elapsedTick = 0;
        }
    }
}