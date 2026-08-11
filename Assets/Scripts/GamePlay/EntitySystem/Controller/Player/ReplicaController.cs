using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 远端玩家插值驱动器。只消费权威快照做渲染插值，不预测。
    /// </summary>
    [RequireComponent(typeof(NetEntityIdentity))]
    [RequireComponent(typeof(NetPositionSync))]
    [RequireComponent(typeof(EntityCharacter))]
    public class ReplicaController : EntityControllerBase
    {
        private NetEntityIdentity _identity;
        private NetPositionSync _positionSync;
        private EntityCharacter _character;

        #region 属性
        public override EntityDriveMode DriveMode => EntityDriveMode.Replica;
        #endregion

        public void Init(NetEntityIdentity identity, NetPositionSync positionSync, EntityCharacter character)
        {
            _identity = identity;
            _character = character;
            Bind(character);
            _positionSync = positionSync;
        }

        /// <summary>
        /// 添加快照
        /// </summary>
        public void AddSnapshot(in NetPositionSnapshot snapshot)
        {
            if (Target == null || _identity == null || _identity.Role != NetEntityRole.Replica) return;
            if (_identity.IsInitialized && snapshot.EntityId != _identity.EntityId) return;

            _positionSync.OnAuthoritySnapshot(snapshot);
        }

        /// <summary>
        /// 每帧推进插值
        /// </summary>
        public void UpdateInterpolation(float deltaTime)
        {
            if (Target == null || _positionSync == null) return;
            _positionSync.UpdateInterpolation(deltaTime);
        }

        private void Awake()
        {
            _identity = GetComponent<NetEntityIdentity>();
            _positionSync = GetComponent<NetPositionSync>();
            _character = GetComponent<EntityCharacter>();
            Bind(_character);
        }
    }
}
