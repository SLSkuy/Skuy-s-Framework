using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 网络同步标识组件，挂载于需要网络同步的实体上
    /// </summary>
    [RequireComponent(typeof(EntityCharacter))]
    public class NetEntityCharacter : MonoBehaviour
    {
        [SerializeField] private uint entityId;
        [SerializeField] public NetEntityRole role;
        
        private EntityCharacter _character;
        private CharacterController _characterController;
        private bool _capsuleAdded;

        #region 对外属性
        public uint EntityId => entityId;
        public NetEntityRole Role => role;
        public bool IsInitialized => entityId != 0;
        #endregion

        /// <summary>
        /// 初始化网络同步标识
        /// </summary>
        public void Init(uint id, NetEntityRole newRole)
        {
            entityId = id;
            SetRole(newRole);
        }

        /// <summary>
        /// 设置网络同步角色
        /// 若为远程玩家，则禁用CC并添加碰撞体
        /// </summary>
        public void SetRole(NetEntityRole newRole)
        {
            if (role == newRole) return;
            
            if (_characterController == null) return;
            _characterController.enabled = newRole is not NetEntityRole.Replica;
            if (newRole is NetEntityRole.Replica && !_capsuleAdded)
            {
                CapsuleCollider col = gameObject.AddComponent<CapsuleCollider>();
                col.height = _characterController.height;
                col.radius = _characterController.radius;
                _capsuleAdded = true;
            }
        }

        /// <summary>
        /// 创建控制角色实例
        /// </summary>
        private void CreateEntity()
        {
            if (!TryGetComponent<EntityCharacter>(out var character))
            {
                character = gameObject.AddComponent<EntityCharacter>();
            }

            character.tickDrive = true;
        }

        #region 快照接口

        /// <summary>
        /// 直接应用权威快照
        /// </summary>
        public void ApplySnapshot(in NetPlayerSnapshot snapshot)
        {
            bool wasEnabled = _characterController != null && _characterController.enabled;
            if (wasEnabled) _characterController.enabled = false;

            // 临时禁用 CharacterController 防冲突
            transform.SetPositionAndRotation(snapshot.Position, Quaternion.Euler(snapshot.Rotation));

            if (wasEnabled) _characterController.enabled = true;
        }
        
        /// <summary>
        /// 应用插值快照（远端渲染用）
        /// </summary>
        public void ApplyInterpolatedSnapshot(in NetPlayerSnapshot from, in NetPlayerSnapshot to, float t)
        {
            Vector3 position = Vector3.Lerp(from.Position, to.Position, t);
            Quaternion rotation = Quaternion.Slerp(Quaternion.Euler(from.Rotation), Quaternion.Euler(to.Rotation), t);
            transform.SetPositionAndRotation(position, rotation);
        }
        
        /// <summary>
        /// 抓取当前状态快照
        /// </summary>
        public NetPlayerSnapshot CaptureSnapshot(uint snapshotTick = 0, uint lastProcessedInputTick = 0)
        {
            return new NetPlayerSnapshot
            {
                EntityId = entityId,
                SnapshotTick = snapshotTick,
                LastProcessedInputTick = lastProcessedInputTick,
                Position = transform.position,
                Rotation = transform.eulerAngles
            };
        }

        #endregion

        #region 生命周期

        private void Awake()
        {
            // TODO: 感觉这里获取组件也不太稳，改成注入？
            _characterController = GetComponent<CharacterController>();
            
            CreateEntity();
        }

        #endregion
    }
}