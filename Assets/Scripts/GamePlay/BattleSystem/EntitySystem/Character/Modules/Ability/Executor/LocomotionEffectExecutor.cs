using UnityEngine;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 移动技能效果处理器，处理技能释放期间的角色位移效果
    /// </summary>
    public class LocomotionEffectExecutor : BaseSkillEffectExecutor
    {
        private readonly MovementModule _movement;
        private readonly ViewModule _view;

        #region 程序控制移动
        private float _speed;
        private bool _isMoving;
        private SkillLocomotionDirectionPolicy _directionPolicy;
        #endregion

        public LocomotionEffectExecutor(EntityContext context) : base(context)
        {
            _movement = context.Get<MovementModule>();
            _view = context.Get<ViewModule>();
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
                switch (_directionPolicy)
                {
                    case SkillLocomotionDirectionPolicy.None:
                        _movement.Move(_context.LastMoveInput, _speed, deltaTime);
                        break;
                    case SkillLocomotionDirectionPolicy.MeshForward:
                        break;
                    case SkillLocomotionDirectionPolicy.CameraForward:
                        break;
                }
            }
        }
    }
}