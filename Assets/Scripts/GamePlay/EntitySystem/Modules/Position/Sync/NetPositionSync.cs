using System;
using GamePlay.NetSync;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 位移同步网络组件，用于进行位移相关的网络同步
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetEntityIdentity))]
    [RequireComponent(typeof(EntityCharacter))]
    public class NetPositionSync : MonoBehaviour, INetSyncComponent,
        INetSyncSnapshotSource<NetPositionSnapshot>,
        INetSyncSnapshotReceiver<NetPositionSnapshot>,
        INetSyncUpdatable
    {
        public ModuleType ModuleType => ModuleType.Position;
        
        private NetEntityIdentity _identity;
        private EntityCharacter _character;
        private CharacterController _characterController;
        private SnapshotBuffer<NetPositionSnapshot> _snapshots;
        private CapsuleCollider _replicaMovementCollider;

        private Vector3 _lastCapturedPosition;
        private double _simulationTickInterval;
        private double _renderServerTime;
        private int _interpolationDelayTicks;
        private uint _lastCapturedTick;
        private bool _hasRenderTick;
        private bool _hasCapturedSnapshot;
        
        #region 属性
        public Vector3 CurrentRenderVelocity { get; private set; }
        public uint CurrentMovementState { get; private set; }
        #endregion

        public void ConfigureRole(NetEntityRole role)
        {
            if (_characterController != null)
            {
                _characterController.enabled = role != NetEntityRole.Replica;
            }

            ConfigureReplicaMovementCollider(role == NetEntityRole.Replica);
            if (role == NetEntityRole.Replica)
            {
                // 初始化快照缓冲区
                SyncConfig config = SyncConfig.Instance;               
                if (_snapshots != null) return;
                _simulationTickInterval = 1d / Mathf.Max(1, config.simulationTickRate);
                _interpolationDelayTicks = Mathf.Max(1, config.interpolationDelayTicks);

                int capacity = Mathf.Max(8, _interpolationDelayTicks * 4);
                _snapshots = new SnapshotBuffer<NetPositionSnapshot>(capacity);
            }
            else
            {
                _snapshots?.Clear();
                _hasRenderTick = false;
            }
        }

        /// <summary>
        /// 接收远程获取到的快照信息，仅Replica角色使用
        /// </summary>
        public void OnAuthoritySnapshot(in NetPositionSnapshot snapshot)
        {
            if (_identity != null && _identity.IsInitialized && snapshot.EntityId != _identity.EntityId) return;
            
            _snapshots.Add(snapshot);

            if (_hasRenderTick) return;

            _renderServerTime = snapshot.SnapshotTick * _simulationTickInterval;
            _hasRenderTick = true;
            ApplySnapshot(snapshot);
        }

        /// <summary>
        /// 插值过渡远程状态，仅Replica角色使用
        /// </summary>
        /// <param name="deltaTime"></param>
        public void UpdateInterpolation(float deltaTime)
        {
            if (!_hasRenderTick || _snapshots == null || _snapshots.Count < 2) return;

            float bufferedTickSpan = _snapshots.LatestTick - _snapshots.OldestTick;
            if (bufferedTickSpan < _interpolationDelayTicks) return;

            double targetRenderTime = (_snapshots.LatestTick - _interpolationDelayTicks) * _simulationTickInterval;
            _renderServerTime = Math.Min(_renderServerTime + deltaTime, targetRenderTime);

            if (_snapshots.TrySample(_renderServerTime, _simulationTickInterval,
                    out NetPositionSnapshot from, out NetPositionSnapshot to, out float t))
            {
                ApplyInterpolatedSnapshot(from, to, t);
            }
        }

        /// <summary>
        /// 由同步根组件驱动同步帧。
        /// </summary>
        public void SyncUpdate(float deltaTime)
        {
            UpdateInterpolation(deltaTime);
        }

        /// <summary>
        /// 如果为Replica角色，需要创建碰撞体，防止客户端预测穿模
        /// </summary>
        private void ConfigureReplicaMovementCollider(bool isReplica)
        {
            if (_characterController == null) return;

            if (isReplica)
            {
                if (_replicaMovementCollider == null)
                {
                    _replicaMovementCollider = gameObject.AddComponent<CapsuleCollider>();
                    _replicaMovementCollider.height = _characterController.height;
                    _replicaMovementCollider.radius = _characterController.radius;
                    _replicaMovementCollider.center = _characterController.center;
                }

                _replicaMovementCollider.enabled = true;
            }
            else if (_replicaMovementCollider != null)
            {
                _replicaMovementCollider.enabled = false;
            }
        }

        #region 同步接口

        /// <summary>
        /// 捕获当前快照
        /// </summary>
        public NetPositionSnapshot CaptureSnapshot(uint snapshotTick = 0, uint lastProcessedInputTick = 0)
        {
            Vector3 position = transform.position;
            Vector3 velocity = Vector3.zero;
            if (_hasCapturedSnapshot && snapshotTick > _lastCapturedTick)
            {
                float deltaTime = (snapshotTick - _lastCapturedTick) * (float)_simulationTickInterval;
                if (deltaTime > Mathf.Epsilon)
                {
                    velocity = (position - _lastCapturedPosition) / deltaTime;
                }
            }

            _lastCapturedPosition = position;
            _lastCapturedTick = snapshotTick;
            _hasCapturedSnapshot = true;

            return new NetPositionSnapshot
            {
                EntityId = _identity ? _identity.EntityId : 0,
                SnapshotTick = snapshotTick,
                LastProcessedInputTick = lastProcessedInputTick,
                Position = position,
                Velocity = velocity,
                MovementState = _character ? _character.CurrentState : EntityState.IDLE
            };
        }

        /// <summary>
        /// 应用快照
        /// </summary>
        public void ApplySnapshot(in NetPositionSnapshot snapshot)
        {
            // 去除CC影响
            bool wasEnabled = _characterController != null && _characterController.enabled;
            if (wasEnabled) _characterController.enabled = false;

            transform.position = snapshot.Position;
            CurrentRenderVelocity = snapshot.Velocity;
            CurrentMovementState = snapshot.MovementState;

            if (wasEnabled) _characterController.enabled = true;
        }
        
        /// <summary>
        /// 插值过渡快照
        /// </summary>
        public void ApplyInterpolatedSnapshot(in NetPositionSnapshot from, in NetPositionSnapshot to, float t)
        {
            Vector3 position = Vector3.Lerp(from.Position, to.Position, t);
            // 去除CC影响
            bool wasEnabled = _characterController != null && _characterController.enabled;
            if (wasEnabled) _characterController.enabled = false;
            
            transform.position = position;
            
            if (wasEnabled) _characterController.enabled = true;

            // 状态过渡
            CurrentRenderVelocity = Vector3.Lerp(from.Velocity, to.Velocity, t);
            CurrentMovementState = t < 0.5f ? from.MovementState : to.MovementState;
        }

        #endregion
        
        #region 声明周期

        private void Awake()
        {
            SyncConfig config = SyncConfig.Instance;
            if (_identity == null) _identity = GetComponent<NetEntityIdentity>();
            if (_character == null) _character = GetComponent<EntityCharacter>();
            if (_characterController == null) _characterController = GetComponent<CharacterController>();
            if (_simulationTickInterval <= 0d)
            {
                _simulationTickInterval = 1d / Mathf.Max(1, config.simulationTickRate);
                _interpolationDelayTicks = Mathf.Max(1, config.interpolationDelayTicks);
            }
        }

        #endregion
    }
}
