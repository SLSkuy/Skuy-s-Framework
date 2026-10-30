using UnityEngine;

namespace GamePlay.BattleSystem
{
    public struct AnimationSnapshot
    {
        
    }
    
    public class AnimationModule : EntityModuleBase<AnimationSnapshot>
    {
        private CharacterConfig _config;
        private Animator _animator;

        private int _speedHash;
        private int _turnHash;
        
        #region 属性
        public override ModuleType ModuleType => ModuleType.Animation;
        #endregion

        public void Init(CharacterConfig config)
        {
            _config = config;
            _animator = GetComponent<Animator>();

            _speedHash = Animator.StringToHash("Speed");
            _turnHash = Animator.StringToHash("Turn");
        }

        private void Update()
        {
            _animator.SetFloat(_speedHash, Target.Context.LocomotionSpeed);
        }

        private void OnAnimatorMove()
        {
            EntityContext context = Target.Context;
            context.Movement.Move(context.LastMoveInput, _animator.velocity.magnitude, context.View.Yaw, Time.deltaTime);
        }

        public override AnimationSnapshot CaptureSnapshot()
        {
            throw new System.NotImplementedException();
        }

        public override void RestoreSnapshot(in AnimationSnapshot state)
        {
            throw new System.NotImplementedException();
        }
    }
}
