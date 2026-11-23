using UnityEngine;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 移动技能效果处理器，处理技能释放期间的角色位移效果
    /// </summary>
    public class MoveEffectExecutor : BaseSkillEffectExecutor
    {
        private readonly MovementModule _movement;

        /// <summary> 程序控制移动状态 </summary>
        private float _speed;
        private bool _isMoving;

        public MoveEffectExecutor(EntityContext context) : base(context)
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
        }
        
        public override void Tick(float deltaTime)
        {
            if (_isMoving)
            {
                _movement.Move(Vector2.up, _speed, deltaTime);
            }
        }

        public override void StopExecution()
        {
            _isMoving = false;
            _speed = 0;
        }
    }
}
