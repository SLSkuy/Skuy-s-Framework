using System;
using GamePlay.EntitySystem;
using UnityEngine;

namespace GamePlay.NetSync
{
    /// <summary>
    /// 远端实体服务端时间插值器，只负责 Position/Rotation 表现采样。
    /// </summary>
    public sealed class SnapshotInterpolator
    {
        private readonly SnapshotBuffer<TransformSnapshot> _snapshots;
        private readonly double _tickInterval;
        private readonly int _interpolationDelayTicks;
        private double _renderServerTime;
        private bool _hasRenderTime;
        private EntitySimulationState _lastState;
        private bool _hasLastState;

        #region 属性
        public int BufferedSnapshotCount => _snapshots.Count;
        public uint OldestTick => _snapshots.OldestTick;
        public uint LatestTick => _snapshots.LatestTick;
        #endregion

        public SnapshotInterpolator(int simulationTickRate, int interpolationDelayTicks, int bufferCapacity)
        {
            _tickInterval = 1d / Mathf.Max(1, simulationTickRate);
            _interpolationDelayTicks = Mathf.Max(1, interpolationDelayTicks);
            _snapshots = new SnapshotBuffer<TransformSnapshot>(Mathf.Max(2, bufferCapacity));
        }

        /// <summary>
        /// 添加服务端快照。乱序和重复快照由 SnapshotBuffer 处理。
        /// </summary>
        public void Add(uint snapshotTick, in EntitySimulationState state)
        {
            if (!state.IsFinite()) return;

            TransformSnapshot snapshot = new()
            {
                SnapshotTick = snapshotTick,
                State = state
            };
            _snapshots.Add(snapshot);

            if (_hasRenderTime) return;

            _renderServerTime = snapshotTick * _tickInterval;
            _lastState = state;
            _hasLastState = true;
            _hasRenderTime = true;
        }

        /// <summary>
        /// 推进渲染服务端时间并采样当前 Transform。
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
                    out TransformSnapshot from, out TransformSnapshot to, out float t))
                return true;

            EntitySimulationState fromState = from.State;
            EntitySimulationState toState = to.State;
            _lastState = new EntitySimulationState
            {
                Position = Vector3.Lerp(fromState.Position, toState.Position, t),
                Rotation = Quaternion.Slerp(fromState.Rotation, toState.Rotation, t),
                LinearVelocity = Vector3.Lerp(fromState.LinearVelocity, toState.LinearVelocity, t),
                AngularVelocity = Vector3.Lerp(fromState.AngularVelocity, toState.AngularVelocity, t)
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
            _lastState = default;
            _hasRenderTime = false;
            _hasLastState = false;
        }
    }
}
