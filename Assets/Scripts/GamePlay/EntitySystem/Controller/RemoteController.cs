using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 远端玩家插值驱动器。只消费权威快照做渲染插值，不预测。
    /// </summary>
    [RequireComponent(typeof(NetEntityIdentity))]
    [RequireComponent(typeof(NetTransformSync))]
    [RequireComponent(typeof(EntityCharacter))]
    public class RemoteController : MonoBehaviour
    {
        private NetEntityIdentity _identity;
        private NetTransformSync _transformSync;
        private EntityCharacter _character;

        public void Init(NetEntityIdentity identity, NetTransformSync transformSync, EntityCharacter character)
        {
            _identity = identity;
            _character = character;
            _transformSync = transformSync;
        }

        /// <summary>
        /// 添加快照
        /// </summary>
        public void AddSnapshot(in NetTransformSnapshot snapshot)
        {
            if (_character == null || _identity == null || _identity.Role != NetEntityRole.Replica) return;
            if (_identity.IsInitialized && snapshot.EntityId != _identity.EntityId) return;

            _transformSync.OnAuthoritySnapshot(snapshot);
        }

        /// <summary>
        /// 每帧推进插值
        /// </summary>
        public void UpdateInterpolation(float deltaTime)
        {
            if (!_character || _transformSync == null) return;
            _transformSync.UpdateInterpolation(deltaTime);
        }

        private void Awake()
        {
            _identity = GetComponent<NetEntityIdentity>();
            _transformSync = GetComponent<NetTransformSync>();
            _character = GetComponent<EntityCharacter>();
        }
    }
}
