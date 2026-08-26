using System;
using GamePlay.EntitySystem;
using GamePlay.Simulator;
using UnityEngine;
using Utils;

namespace GamePlay.MultiPlaySystem
{
    /// <summary>
    /// 远端角色快照缓冲、传送检测与插值采样。
    /// </summary>
    public sealed class CharacterSnapshotInterpolator
    {
        private readonly SnapshotBuffer _snapshots;
        private readonly double _tickInterval;
        private readonly int _interpolationDelayTicks;
        private EntityRollbackState _lastIncomingState;
        private EntityRollbackState _lastState;
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
            _snapshots = new SnapshotBuffer(Mathf.Max(2, bufferCapacity));
        }

        /// <summary>
        /// 添加远端快照；首帧或大距离传送返回 true，调用方应立即 Snap。
        /// </summary>
        public bool AddSnapshot(uint snapshotTick, in EntityRollbackState state)
        {
            SimulationConfig config = SimulationConfig.Instance;
            bool requiresSnap = !_hasIncomingState ||
                Vector3.Distance(_lastIncomingState.movementState.rootPosition, state.movementState.rootPosition) >=
                config.positionSnapThreshold ||
                Quaternion.Angle(_lastIncomingState.movementState.meshRotation, state.movementState.meshRotation) >=
                config.rotationSnapThresholdDegrees ||
                Quaternion.Angle(_lastIncomingState.viewState.viewRotation, state.viewState.viewRotation) >=
                config.rotationSnapThresholdDegrees;
            if (requiresSnap) Reset();

            _snapshots.Add(new EntityAuthorityState
            {
                snapshotTick = snapshotTick,
                state = state
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
        public bool TrySample(float deltaTime, out EntityRollbackState state)
        {
            state = _lastState;
            if (!_hasRenderTime || !_hasLastState) return false;
            if (_snapshots.Count < 2) return true;

            if (_snapshots.LatestTick - _snapshots.OldestTick < _interpolationDelayTicks)
                return true;

            double targetRenderTime = (_snapshots.LatestTick - _interpolationDelayTicks) * _tickInterval;
            _renderServerTime = Math.Min(_renderServerTime + Mathf.Max(0f, deltaTime), targetRenderTime);

            if (!_snapshots.TrySample(_renderServerTime, _tickInterval,
                    out EntityAuthorityState from, out EntityAuthorityState to, out float t))
                return true;

            _lastState = LerpVisibleState(from.state, to.state, t);
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

        private static EntityRollbackState LerpVisibleState(in EntityRollbackState from, in EntityRollbackState to,
            float t)
        {
            EntityRollbackState state = t >= 0.5f ? to : from;
            MovementRollbackState fromMove = from.movementState;
            MovementRollbackState toMove = to.movementState;
            MovementRollbackState movement = state.movementState;
            movement.rootPosition = Vector3.Lerp(fromMove.rootPosition, toMove.rootPosition, t);
            movement.meshRotation = Quaternion.Slerp(fromMove.meshRotation, toMove.meshRotation, t);
            movement.rootLinearVelocity = Vector3.Lerp(fromMove.rootLinearVelocity, toMove.rootLinearVelocity, t);
            movement.meshAngularVelocity = Vector3.Lerp(fromMove.meshAngularVelocity, toMove.meshAngularVelocity, t);
            state.movementState = movement;

            ViewRollbackState fromView = from.viewState;
            ViewRollbackState toView = to.viewState;
            ViewRollbackState view = state.viewState;
            Quaternion viewRotation = Quaternion.Slerp(fromView.viewRotation, toView.viewRotation, t);
            Vector3 euler = viewRotation.eulerAngles;
            view.viewRotation = viewRotation;
            view.viewAngularVelocity = Vector3.Lerp(fromView.viewAngularVelocity, toView.viewAngularVelocity, t);
            view.yaw = euler.y;
            view.pitch = MathUtils.NormalizePitch(euler.x);
            state.viewState = view;
            return state;
        }
    }
}
