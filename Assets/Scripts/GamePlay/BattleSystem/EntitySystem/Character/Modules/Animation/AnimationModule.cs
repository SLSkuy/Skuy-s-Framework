using UnityEngine;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 实体动画能力组件，控制实体的动画播放
    /// </summary>
    public class AnimationModule : EntityModuleBase
    {
        private Animator _animator;
        private MovementModule _movement;
        private ViewModule _view;

        private int _speedHash;
        private int _turnHash;
        
        #region 属性
        public override ModuleType ModuleType => ModuleType.Animation;
        #endregion

        /// <summary>
        /// 绑定后解析动画组件并缓存参数标识。
        /// </summary>
        protected override void OnBound(EntityCharacter target)
        {
            _animator = GetComponent<Animator>();
            _movement = Context.Get<MovementModule>();
            _view = Context.Get<ViewModule>();

            _speedHash = Animator.StringToHash("Speed");
            _turnHash = Animator.StringToHash("Turn");
        }

        private void Update()
        {
            SetAnimParam();
        }

        private void SetAnimParam()
        {
            _animator.SetFloat(_speedHash, Context.ActualPlanarSpeed);
            
            // 根运动时才启用倾斜跑动带动身体旋转
            if(Config.rootMotion) _animator.SetFloat(_turnHash, CalculateTurn());
        }

        /// <summary>
        /// 将 mesh 朝向到视角相对输入朝向的水平夹角映射到 [-1, 1]，左负右正。
        /// </summary>
        private float CalculateTurn()
        {
            Vector2 moveInput = Context.LastMoveInput;
            if (moveInput.sqrMagnitude <= Mathf.Epsilon) return 0f;

            Vector3 inputDirection = Quaternion.Euler(0f, _view.Yaw, 0f) * new Vector3(moveInput.x, 0f, moveInput.y);
            Vector3 meshForward = Vector3.ProjectOnPlane(_movement.Rotation * Vector3.forward, Vector3.up);
            float angle = Vector3.SignedAngle(meshForward, inputDirection, Vector3.up);
            return Mathf.Clamp(angle / 180f, -1f, 1f);
        }

        private void OnAnimatorMove()
        {

        }
    }
}
