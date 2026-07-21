using System;
using Framework;
using GamePlay.NetSync;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 客户端远端玩家驱动器，只消费权威快照并进行渲染插值。
    /// </summary>
    [RequireComponent(typeof(NetPlayerCharacter))]
    public class RemotePlayerController : AutoEventMonoBehaviour
    {
        private NetPlayerCharacter _character;
        private SnapshotBuffer<NetPlayerSnapshot> _snapshots;
        
        private int _interpolationDelayTicks;
        private double _simulationTickInterval;
        
        private double _renderServerTime;
        private bool _hasRenderTick;

        /// <summary>
        /// 配置本地远端玩家控制器
        /// </summary>
        /// <param name="character"></param>
        public void Configure(NetPlayerCharacter character)
        {
            _character = character;
        }

        /// <summary>
        /// 添加快照
        /// </summary>
        /// <param name="snapshot"></param>
        public void AddSnapshot(in NetPlayerSnapshot snapshot)
        {
            if (_character == null || _character.Role != NetEntityRole.Replica) return;
            if (_character.IsInitialized && snapshot.EntityId != _character.EntityId) return;

            _snapshots.Add(snapshot);
            
            // 初始快照直接应用，后续快照走插值处理
            if (_hasRenderTick) return;
            _renderServerTime = snapshot.SnapshotTick * _simulationTickInterval;
            _hasRenderTick = true;
            _character.ApplySnapshot(snapshot);
        }
        

        private void TryInterpolation()
        {
            if (!_hasRenderTick || _snapshots.Count < 2 || !_character) return;

            // 确保留有足够的插值余量
            float bufferedTickSpan = _snapshots.LatestTick - _snapshots.OldestTick;
            if (bufferedTickSpan < _interpolationDelayTicks) return;

            double targetRenderTime = (_snapshots.LatestTick - _interpolationDelayTicks) * _simulationTickInterval;
            _renderServerTime = Math.Min(_renderServerTime + Time.deltaTime, targetRenderTime);

            if (_snapshots.TrySample(_renderServerTime, _simulationTickInterval, out NetPlayerSnapshot from, out NetPlayerSnapshot to, out float t))
            {
                _character.ApplyInterpolatedSnapshot(from, to, t);
            }
        }

        #region 生命周期

        private void Awake()
        {
            SyncConfig config = SyncConfig.Instance;
            
            _simulationTickInterval = 1d / Mathf.Max(1, config.simulationTickRate);
            _interpolationDelayTicks = Mathf.Max(1, config.interpolationDelayTicks);
            
            int capacity = Mathf.Max(8, _interpolationDelayTicks * 4);
            _snapshots = new SnapshotBuffer<NetPlayerSnapshot>(capacity);
        }

        private void Update()
        {
            TryInterpolation();
        }

        #endregion
    }
}
