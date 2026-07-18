using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 基础网络实体角色，提供基础的移动能力和网络同步能力
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public abstract class NetEntityCharacter<T> : NetEntity<T>, IEntityCharacter where T : struct, IEntitySnapshot
    {
        [Header("实体属性")]
        [SerializeField] protected EntityConfig config;

        private MovementModule _movementModule;
        private CharacterController _characterController;

        public void Move(Vector2 dir)
        {
            _movementModule.Move(dir, Time.deltaTime);
        }

        /// <summary>
        /// 使用Tick时间进行模拟，避免帧率不一致导致模拟偏差过大
        /// </summary>
        public void Move(Vector2 dir, float tickTime)
        {
            _movementModule.Move(dir, tickTime);
        }

        public void StartSprint()
        {
            _movementModule.StartSprint();
        }

        public void StopSprint()
        {
            _movementModule.StopSprint();
        }

        public void Dash()
        {
            _movementModule.Dash();
        }

        public void Rotate(Vector3 dir)
        {
            _movementModule.Rotate(dir);
        }

        public override void SetRole(NetEntityRole role)
        {
            base.SetRole(role);
            _characterController.enabled = role == NetEntityRole.Authority;
        }

        #region 生命周期

        protected override void Awake()
        {
            base.Awake();

            if (config == null) config = EntityConfig.Instance;
            
            _characterController = GetComponent<CharacterController>();
            _movementModule = new MovementModule();
            _movementModule.Init(_characterController, config, transform);
        }

        #endregion
    }
}
