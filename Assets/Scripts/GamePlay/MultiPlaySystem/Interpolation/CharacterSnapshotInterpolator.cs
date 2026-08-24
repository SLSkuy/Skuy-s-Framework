using System;
using GamePlay.EntitySystem;
using UnityEngine;

namespace GamePlay.MultiPlaySystem
{
    /// <summary>
    /// 远端角色快照缓冲、传送检测与插值采样。
    /// </summary>
    public sealed class CharacterSnapshotInterpolator
    {
        private readonly SnapshotBuffer<CharacterSnapshot> _snapshots;
        private readonly double _tickInterval;
        private readonly int _interpolationDelayTicks;
        private EntitySimulationState _lastIncomingState;
        private EntitySimulationState _lastState;
        private double _renderServerTime;
        private bool _hasIncomingState;
        private bool _hasRenderTime;
        private bool _hasLastState;

        #region Properties
        public int BufferedSnapshotCount => _snapshots.Count;
        #endregion

        public CharacterSnapshotInterpolator(int simulationTickRate, int interpolationDelayTicks, int bufferCapacity)
        {
            _tickInterval = 1d / Mathf.Max(1, simulationTickRate);
            _interpolationDelayTicks = Mathf.Max(1, interpolationDelayTicks);
            _snapshots = new SnapshotBuffer<CharacterSnapshot>(Mathf.Max(2, bufferCapacity));
        }

        /// <summary>
        /// 添加远端快照；首帧或大距离传送返回 true，调用方应立即 Snap。
        /// </summary>
        public bool AddSnapshot(uint snapshotTick, in EntitySimulationState state)
        {
            SyncConfig config = SyncConfig.Instance;
            bool requiresSnap = !_hasIncomingState ||
                Vector3.Distance(_lastIncomingState.position, state.position) >= config.positionSnapThreshold ||
                Quaternion.Angle(_lastIncomingState.rotation, state.rotation) >=
                config.rotationSnapThresholdDegrees;
            if (requiresSnap) Reset();

            _snapshots.Add(new CharacterSnapshot
            {
                SnapshotTick = snapshotTick,
                State = state
            });
            _lastIncomingState = state;
            _hasIncomingState = true;

            if (!_hasRenderTime)
            {
                _renderServerTime = snapshotTick * _tickInterval;
                _lastState = state;
                _hasLastState = true;
                _hasRenderTime = true;
            }

            return requiresSnap;
        }

        /// <summary>
        /// 推进远端渲染时间并采样角色姿态。
        /// </summary>
        public bool TrySample(float deltaTime, out EntitySimulationState state)
        {
            state = _lastState;
            if (!_hasRenderTime || !_hasLastState) return false;
            if (_snapshots.Count < 2) return true;

            if (_snapshots.LatestTick - _snapshots.OldestTick < _interpolationDelayTicks)
                return true;

            double targetRenderTime = (_snapshots.LatestTick - _interpolationDelayTicks) * _tickInterval;
            _renderServerTime = Math.Min(_renderServerTime + Mathf.Max(0f, deltaTime), targetRenderTime);

            if (!_snapshots.TrySample(_renderServerTime, _tickInterval,
                    out CharacterSnapshot from, out CharacterSnapshot to, out float t))
                return true;

            EntitySimulationState fromState = from.State;
            EntitySimulationState toState = to.State;
            _lastState = new EntitySimulationState
            {
                position = Vector3.Lerp(fromState.position, toState.position, t),
                rotation = Quaternion.Slerp(fromState.rotation, toState.rotation, t),
                linearVelocity = Vector3.Lerp(fromState.linearVelocity, toState.linearVelocity, t),
                angularVelocity = Vector3.Lerp(fromState.angularVelocity, toState.angularVelocity, t),
                locomotionState = t >= 0.5f ? toState.locomotionState : fromState.locomotionState,
                isGrounded = t >= 0.5f ? toState.isGrounded : fromState.isGrounded
            };
            state = _lastState;
            return true;
        }

        /// <summary>
        /// 清空缓冲并回到未初始化状态，用于实体传送或重新生成。
        /// </summary>
        public void Reset()
        {
            _snapshots.Clear();
            _renderServerTime = 0d;
            _lastIncomingState = default;
            _lastState = default;
            _hasIncomingState = false;
            _hasRenderTime = false;
            _hasLastState = false;
        }
    }
}
