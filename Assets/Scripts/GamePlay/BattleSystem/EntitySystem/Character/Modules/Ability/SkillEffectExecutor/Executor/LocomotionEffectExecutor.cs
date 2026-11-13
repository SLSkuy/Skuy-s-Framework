using UnityEngine;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 移动技能效果处理器，处理技能释放期间的角色位移效果
    /// </summary>
    public class LocomotionEffectExecutor : BaseSkillEffectExecutor
    {
        private readonly MovementModule _movement;

        /// <summary> 程序控制移动状态 </summary>
        private float _speed;
        private bool _isMoving;

        public LocomotionEffectExecutor(EntityContext context) : base(context)
        {
            _movement = context.Get<MovementModule>();
        }

        public override void ExecuteSkillEffect(SkillEffect effect)
        {
            if (effect is MoveSkillEffect moveEffect)
            {
                _speed = moveEffect.speed;
                _isMoving = true;
            }

            if (effect is RootMotionSkillEffect rootMotionEffect)
            {
                
            }
        }

        public override void StopExecution()
        {
            if (_isMoving)
            {
                _isMoving = false;
                _speed = 0;
            }
        }

        public override void Tick(float deltaTime)
        {
            if (_isMoving)
            {
                _movement.Move(Vector2.up, _speed, deltaTime);
            }
        }
    }
}
