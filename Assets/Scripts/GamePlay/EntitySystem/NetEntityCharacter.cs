using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 基础实体角色，提供基础的移动能力
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [RequireComponent(typeof(CharacterController))]
    public abstract class NetEntityCharacter<T> : NetEntity<T>, IEntityCharacter where T : struct, IEntitySnapshot
    {
        [Header("实体属性")]
        [SerializeField]protected EntityConfig config;

        private MovementModule _movementModule;
        private CharacterController _characterController;
        
        public void Move(Vector2 dir)
        {
            _movementModule.Move(dir);   
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

        #region 生命周期

        protected override void Awake()
        {
            base.Awake();

            if (config == null) config = EntityConfig.Instance;
            
            _characterController = GetComponent<CharacterController>();
            _movementModule = new MovementModule();
            _movementModule.Init(_characterController, config, _transform);
        }

        #endregion
    }
}