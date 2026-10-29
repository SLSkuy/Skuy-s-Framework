using System;
using System.Collections.Generic;
using Framework;
using GamePlay.DataProxy;
using UnityEngine;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 缓冲权威帧，插值 Replica 实体快照
    /// </summary>
    public class SnapshotInterpolation
    {
        private readonly SimulationDataProxy _simulationData = Global.GetDataProxy<SimulationDataProxy>();
        
        private class EntityTrack
        {
            public SnapshotBuffer Buffer;
            public uint AppliedSnapshotTick;
            public bool HasAppliedSnapshot;
        }

        private readonly List<EntityAuthoritySnapshot> _crossedFrames = new();
        private readonly Dictionary<uint, EntityTrack> _tracks = new();
        private readonly List<uint> _expiredEntityIds = new();
        private readonly Simulator _simulator;
        
        private readonly float _snapshotTickRate;
        private readonly int _targetBufferedTicks;
        private readonly int _snapshotBufferCapacity;

        /// <summary>
        /// 当前播放的服务端Tick，取出包围当前Tick的两个快照，进行插值显示
        /// </summary>
        private double _currentPlaybackTick;
        private bool _hasStartedPlayback;   // 是否存在播放窗口，用于辨别首次开始插值或等待插值时间过久
        private bool _isWaitingForSnapshots;    // 快照消耗完毕，是否等待快照
        private float _smoothedRemainingSnapshotTicks;  // 剩余可消耗的快照数量

        public SnapshotInterpolation(Simulator simulator, int bufferCapacity = 32)
        {
            _simulator = simulator;
            _targetBufferedTicks = _simulationData.InterpolateDelayTick;  // 延迟插值帧，避免出现没有下一帧作为插值样本
            _snapshotTickRate = _simulationData.SnapshotSyncRate;
            _snapshotBufferCapacity = bufferCapacity;
        }

        /// <summary>
        /// 写入一帧权威快照，不改实体。
        /// </summary>
        public void AddFrame(uint entityId, in EntityAuthoritySnapshot snapshot)
        {
            if (!_tracks.TryGetValue(entityId, out EntityTrack track))
            {
                track = new EntityTrack
                {
                    Buffer = new SnapshotBuffer(_snapshotBufferCapacity)
                };

                _tracks.Add(entityId, track);
            }

            track.Buffer.AddFrame(snapshot);
        }

        /// <summary>
        /// 沿快照 tick 推进播放时刻，并写出 Replica 画面。
        /// </summary>
        public void Present(float deltaTime)
        {
            if (_tracks.Count == 0) return;

            if (!TryGetLatestTick(out uint latestReceivedSnapshotTick, out uint oldestBufferedSnapshotTick)) return;

            // 延迟是目标缓冲深度，实际收到的最新快照才是播放边界
            double targetPlaybackTick = Math.Max(oldestBufferedSnapshotTick, (double)latestReceivedSnapshotTick - _targetBufferedTicks);
            if (!_hasStartedPlayback)
            {
                // 首次接收快照时，等待形成缓冲窗口，再从目标延迟位置播放。
                if (!CanStartPlayback()) return;

                _currentPlaybackTick = targetPlaybackTick;
                _smoothedRemainingSnapshotTicks = _targetBufferedTicks;
                _hasStartedPlayback = true;
            }
            else if (_currentPlaybackTick < oldestBufferedSnapshotTick)
            {
                // 长时间停顿导致播放位置已被缓冲淘汰时，重新建立播放窗口
                _currentPlaybackTick = targetPlaybackTick;
                _smoothedRemainingSnapshotTicks = _targetBufferedTicks;
                _isWaitingForSnapshots = false;
            }
            else
            {
                AdvancePlayback(deltaTime, latestReceivedSnapshotTick);
            }

            _expiredEntityIds.Clear();
            foreach (KeyValuePair<uint, EntityTrack> pair in _tracks)
            {
                // 清除失活实体记录
                if (!_simulator.TryGet(pair.Key, out _, out EntityCharacter character))
                {
                    _expiredEntityIds.Add(pair.Key);
                    continue;
                }

                if (!character.IsInitialized) continue;

                PresentEntity(pair.Value, character, _currentPlaybackTick);
            }

            for (int i = 0; i < _expiredEntityIds.Count; i++)
            {
                _tracks.Remove(_expiredEntityIds[i]);
            }

            if (_tracks.Count == 0)
            {
                _hasStartedPlayback = false;
                _isWaitingForSnapshots = false;
            }
        }

        /// <summary>
        /// 连续推进播放头，用缓冲深度温和调速，耗尽后等待余量恢复。
        /// </summary>
        private void AdvancePlayback(float deltaTime, uint latestReceivedSnapshotTick)
        {
            float remainingSnapshotTicks = (float)(latestReceivedSnapshotTick - _currentPlaybackTick);
            if (_isWaitingForSnapshots)
            {
                // 保持当前位置，积累余量到达目标缓存后向前恢复，避免反复短暂停顿或倒播
                if (remainingSnapshotTicks < Mathf.Max(1, _targetBufferedTicks)) return;

                _isWaitingForSnapshots = false;
                _smoothedRemainingSnapshotTicks = _targetBufferedTicks;
            }

            // 滤除逐包到达产生的锯齿，容差内保持原速，最大调速幅度为 5%。
            float bufferSmoothingFactor = 1f - Mathf.Exp(-2f * deltaTime);
            _smoothedRemainingSnapshotTicks = Mathf.Lerp(_smoothedRemainingSnapshotTicks, remainingSnapshotTicks, bufferSmoothingFactor);
            float bufferedTickError = _smoothedRemainingSnapshotTicks - _targetBufferedTicks;
            float playbackSpeedMultiplier = 1f;
            if (Mathf.Abs(bufferedTickError) > 0.5f)
            {
                playbackSpeedMultiplier += Mathf.Clamp((bufferedTickError - Mathf.Sign(bufferedTickError) * 0.5f) * 0.02f, -0.05f, 0.05f);
            }

            _currentPlaybackTick += deltaTime * _snapshotTickRate * playbackSpeedMultiplier;
            if (_currentPlaybackTick >= latestReceivedSnapshotTick)
            {
                _currentPlaybackTick = latestReceivedSnapshotTick;
                _isWaitingForSnapshots = true;
            }
        }

        /// <summary>
        /// 判断是否已经形成可播放窗口。
        /// </summary>
        private bool CanStartPlayback()
        {
            foreach (EntityTrack track in _tracks.Values)
            {
                SnapshotBuffer buffer = track.Buffer;
                if (buffer.Count < 2) continue;

                if (buffer.LatestTick - buffer.OldestTick >= _targetBufferedTicks)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryGetLatestTick(out uint latestReceivedSnapshotTick, out uint oldestBufferedSnapshotTick)
        {
            latestReceivedSnapshotTick = 0;
            oldestBufferedSnapshotTick = uint.MaxValue;
            bool hasFrame = false;

            foreach (EntityTrack track in _tracks.Values)
            {
                SnapshotBuffer buffer = track.Buffer;
                if (buffer.Count == 0) continue;

                if (buffer.OldestTick < oldestBufferedSnapshotTick) oldestBufferedSnapshotTick = buffer.OldestTick;

                if (!hasFrame || buffer.LatestTick > latestReceivedSnapshotTick)
                {
                    latestReceivedSnapshotTick = buffer.LatestTick;
                    hasFrame = true;
                }
            }

            return hasFrame;
        }

        /// <summary>
        /// 根据playbackTick，获取包围playbackTick的两个快照，进行快照插值
        /// </summary>
        private void PresentEntity(EntityTrack track, EntityCharacter character, double currentPlaybackTick)
        {
            SnapshotBuffer buffer = track.Buffer;
            if (buffer.Count == 0) return;

            // 当前播放头Tick位置小于缓存的最旧的快照，直接使用最旧的快照
            if (currentPlaybackTick < buffer.OldestTick)
            {
                if (buffer.TryGetOldest(out EntityAuthoritySnapshot oldestSnapshot))
                {
                    ApplyCrossed(track, character, buffer, oldestSnapshot.snapshotTick);    // 直接应用离散状态
                    ApplySnapshot(character, oldestSnapshot);   // 写入移动连续状态插值位置
                }

                return;
            }
            
            // 获取包围当前播放头Tick位置的两个快照
            if (!buffer.TryGetFrame(currentPlaybackTick, out EntityAuthoritySnapshot previousSnapshot, out EntityAuthoritySnapshot nextSnapshot))
            {
                // 当前播放头Tick位置大于缓存的最新的快照，直接使用最新的快照
                if (buffer.TryGetLatest(out EntityAuthoritySnapshot latestSnapshot))
                {
                    ApplyCrossed(track, character, buffer, latestSnapshot.snapshotTick);
                    ApplySnapshot(character, latestSnapshot);
                }

                return;
            }
            
            // 离散状态使用包围当前播放头Tick位置前一个Tick的状态
            ApplyCrossed(track, character, buffer, previousSnapshot.snapshotTick);
            
            // 播放头Tick处于两个快照之间，执行插值逻辑
            float snapshotIntervalTicks = nextSnapshot.snapshotTick - previousSnapshot.snapshotTick;
            float interpolationFactor = Mathf.Clamp01((float)((currentPlaybackTick - previousSnapshot.snapshotTick) / snapshotIntervalTicks));

            // 应用已经插值过的快照状态
            InterpolateSnapshot(character, previousSnapshot, nextSnapshot, interpolationFactor);
        }
        
        /// <summary>
        /// 播放头跨过快照时写入离散状态，离散状态不能插值
        /// </summary>
        private void ApplyCrossed(EntityTrack track, EntityCharacter character, SnapshotBuffer buffer, uint throughTick)
        {
            buffer.CopyFramesThrough(throughTick, track.AppliedSnapshotTick, track.HasAppliedSnapshot, _crossedFrames);
            for (int i = 0; i < _crossedFrames.Count; i++)
            {
                EntityAuthoritySnapshot snapshot = _crossedFrames[i];
                character.RestoreSnapshot(snapshot.state);
                track.AppliedSnapshotTick = snapshot.snapshotTick;
                track.HasAppliedSnapshot = true;
            }
        }
               
        #region 组件插值逻辑
        
        /// <summary>
        /// 直接应用快照表现，对于非离散状态则应使用插值，例如移动逻辑
        /// </summary>
        private void ApplySnapshot(EntityCharacter character, in EntityAuthoritySnapshot snapshot)
        {
            // 应用移动快照
            MovementSnapshot movement = snapshot.state.movement;
            character.PresentReplicaPose(movement.rootPosition, movement.meshRotation, snapshot.state.view.viewRotation);
        }

        private void InterpolateSnapshot(EntityCharacter character, in EntityAuthoritySnapshot previousSnapshot, in EntityAuthoritySnapshot nextSnapshot, float interpolationFactor)
        {
            // 插值实体姿态
            InterpolatePose(character, previousSnapshot, nextSnapshot, interpolationFactor);
        }
        
        /// <summary>
        /// 插值实体移动旋转相关表现
        /// </summary>
        private void InterpolatePose(EntityCharacter character, in EntityAuthoritySnapshot previousSnapshot, in EntityAuthoritySnapshot nextSnapshot, float interpolationFactor)
        {
            MovementSnapshot previousMovement = previousSnapshot.state.movement;
            MovementSnapshot nextMovement = nextSnapshot.state.movement;

            // 旋转使用球形插值
            character.PresentReplicaPose(Vector3.Lerp(previousMovement.rootPosition, nextMovement.rootPosition, interpolationFactor),
                Quaternion.Slerp(previousMovement.meshRotation, nextMovement.meshRotation, interpolationFactor),
                Quaternion.Slerp(previousSnapshot.state.view.viewRotation, nextSnapshot.state.view.viewRotation, interpolationFactor));
        }

        #endregion

        /// <summary>
        /// 清空缓冲与播放时间。
        /// </summary>
        public void Clear()
        {
            _tracks.Clear();
            _expiredEntityIds.Clear();
            _crossedFrames.Clear();

            _currentPlaybackTick = 0;
            _hasStartedPlayback = false;
            _isWaitingForSnapshots = false;
            _smoothedRemainingSnapshotTicks = 0f;
        }
    }
}