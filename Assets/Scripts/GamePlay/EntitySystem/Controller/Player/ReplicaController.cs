using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 远端玩家插值驱动器。只消费权威快照做渲染插值，不预测。
    /// </summary>
    [RequireComponent(typeof(NetEntityIdentity))]
    [RequireComponent(typeof(NetTransformSync))]
    [RequireComponent(typeof(EntityCharacter))]
    public class ReplicaController : EntityControllerBase
    {
        private NetEntityIdentity _identity;
        private NetTransformSync _transformSync;
        private EntityCharacter _character;

        #region 属性
        public override EntityDriveMode DriveMode => EntityDriveMode.Replica;
        #endregion

        public void Init(NetEntityIdentity identity, NetTransformSync transformSync, EntityCharacter character)
        {
            _identity = identity;
            _character = character;
            Bind(character);
            _transformSync = transformSync;
        }

        /// <summary>
        /// 添加快照
        /// </summary>
        public void AddSnapshot(in NetTransformSnapshot snapshot)
        {
            if (Target == null || _identity == null || _identity.Role != NetEntityRole.Replica) return;
            if (_identity.IsInitialized && snapshot.EntityId != _identity.EntityId) return;

            _transformSync.OnAuthoritySnapshot(snapshot);
        }

        /// <summary>
        /// 每帧推进插值
        /// </summary>
        public void UpdateInterpolation(float deltaTime)
        {
            if (Target == null || _transformSync == null) return;
            _transformSync.UpdateInterpolation(deltaTime);
        }

        private void Awake()
        {
            _identity = GetComponent<NetEntityIdentity>();
            _transformSync = GetComponent<NetTransformSync>();
            _character = GetComponent<EntityCharacter>();
            Bind(_character);
        }
    }
}
