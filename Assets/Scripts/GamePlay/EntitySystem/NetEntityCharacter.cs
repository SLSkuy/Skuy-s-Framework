using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 可进行固定步长模拟的网络角色。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public abstract class NetEntityCharacter<T> : NetEntity, IEntityCharacter<T> where T : struct, IEntitySnapshot
    {
        [Header("实体角色属性")]
        [SerializeField] protected EntityConfig config;

        private CharacterController _characterController;
        private MovementModule _movementModule;
        
        /// <summary>
        /// 更新模拟角色
        /// </summary>
        /// <param name="entityRole"></param>
        private void UpdateSimulationAvailability(NetEntityRole entityRole)
        {
            if (_characterController == null) return;
            _characterController.enabled = entityRole is NetEntityRole.Authority or NetEntityRole.Predict;
            if (entityRole is NetEntityRole.Replica)
            {
                var col = gameObject.AddComponent<CapsuleCollider>();
                col.height = _characterController.height;
                col.radius = _characterController.radius;
            }
        }
        
        #region 快照管理
        
        public virtual void ApplySnapshot(in T snapshot)
        {
            bool controllerWasEnabled = _characterController != null && _characterController.enabled;
            if (controllerWasEnabled) _characterController.enabled = false;

            transform.SetPositionAndRotation(snapshot.Position, Quaternion.Euler(snapshot.Rotation));

            if (controllerWasEnabled) _characterController.enabled = true;
        }
        
        public virtual void ApplyInterpolatedSnapshot(in T from, in T to, float t)
        {
            Vector3 position = Vector3.Lerp(from.Position, to.Position, t);
            Quaternion rotation = Quaternion.Slerp(Quaternion.Euler(from.Rotation), Quaternion.Euler(to.Rotation), t);
            transform.SetPositionAndRotation(position, rotation);
        }
        
        public virtual T CaptureSnapshot(uint snapshotTick, uint lastProcessedInputTick = 0)
        {
            return new T
            {
                EntityId = EntityId,
                SnapshotTick = snapshotTick,
                Position = transform.position,
                Rotation = transform.eulerAngles
            };
        }
        
        #endregion
        
        #region 实体操控方法
        
        public void Move(Vector2 direction, float tickDeltaTime)
        {
            _movementModule.Move(direction, tickDeltaTime);
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

        public void Rotate(Vector3 direction)
        {
            _movementModule.Rotate(direction);
        }
        
        #endregion

        #region 事件回调

        protected override void OnRoleChanged(NetEntityRole previousRole, NetEntityRole newRole)
        {
            base.OnRoleChanged(previousRole, newRole);
            UpdateSimulationAvailability(newRole);
        }

        #endregion
        
        #region 生命周期

        private void Awake()
        {
            if (config == null) config = EntityConfig.Instance;

            _characterController = GetComponent<CharacterController>();
            _movementModule = new MovementModule();
            _movementModule.Init(_characterController, config, transform);
            UpdateSimulationAvailability(Role);
        }

        #endregion
    }
}
