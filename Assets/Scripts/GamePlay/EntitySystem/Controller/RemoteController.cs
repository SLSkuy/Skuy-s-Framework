using System;
using GamePlay.NetSync;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 远端玩家插值驱动器（普通 C# 类，由 ClientSimulator 持有）。
    /// 只消费权威快照做渲染插值，不预测。
    /// </summary>
    public class RemoteController : MonoBehaviour
    {
        private NetEntityCharacter _netCharacter;
        private EntityCharacter _character;
        private SnapshotBuffer<NetPlayerSnapshot> _snapshots;

        private int _interpolationDelayTicks;
        private double _simulationTickInterval;
        private double _renderServerTime;
        private bool _hasRenderTick;

        private void Init(NetEntityCharacter netCharacter, EntityCharacter character)
        {
            _character = character;
            _netCharacter = netCharacter;
            
            SyncConfig config = SyncConfig.Instance;
            _simulationTickInterval = 1d / Mathf.Max(1, config.simulationTickRate);
            _interpolationDelayTicks = Mathf.Max(1, config.interpolationDelayTicks);

            int capacity = Mathf.Max(8, _interpolationDelayTicks * 4);
            _snapshots = new SnapshotBuffer<NetPlayerSnapshot>(capacity);
        }
        
        /// <summary>
        /// 添加快照
        /// </summary>
        public void AddSnapshot(in NetPlayerSnapshot snapshot)
        {
            if (_character == null || _netCharacter.Role != NetEntityRole.Replica) return;
            if (_netCharacter.IsInitialized && snapshot.EntityId != _netCharacter.EntityId) return;

            _snapshots.Add(snapshot);

            // 初始快照直接应用，后续快照走插值处理
            if (_hasRenderTick) return;
            _renderServerTime = snapshot.SnapshotTick * _simulationTickInterval;
            _hasRenderTick = true;
            _netCharacter.ApplySnapshot(snapshot);
        }
        
        /// <summary>
        /// 每帧推进插值（由 ClientSimulator.Update 调用，替代旧版 RemotePlayerController.Update）
        /// </summary>
        public void UpdateInterpolation(float deltaTime)
        {
            if (!_hasRenderTick || _snapshots.Count < 2 || !_character) return;

            // 确保留有足够的插值余量
            float bufferedTickSpan = _snapshots.LatestTick - _snapshots.OldestTick;
            if (bufferedTickSpan < _interpolationDelayTicks) return;

            double targetRenderTime = (_snapshots.LatestTick - _interpolationDelayTicks) * _simulationTickInterval;
            _renderServerTime = Math.Min(_renderServerTime + deltaTime, targetRenderTime);

            if (_snapshots.TrySample(_renderServerTime, _simulationTickInterval,
                    out NetPlayerSnapshot from, out NetPlayerSnapshot to, out float t))
            {
                _netCharacter.ApplyInterpolatedSnapshot(from, to, t);
            }
        }
    }
}