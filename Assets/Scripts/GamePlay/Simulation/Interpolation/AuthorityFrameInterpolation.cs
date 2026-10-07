using System.Collections.Generic;
using GamePlay.EntitySystem;
using UnityEngine;

namespace GamePlay.Simulation
{
    /// <summary>
    /// 缓冲权威帧，插值 Replica 实体快照
    /// </summary>
    public sealed class AuthorityFrameInterpolation
    {
        private sealed class EntityTrack
        {
            public AuthorityFrameBuffer Buffer;
            public uint AppliedSnapshotTick;
            public bool HasAppliedSnapshot;
        }

        private readonly List<EntityAuthorityFrame> _crossedFrames = new();
        private readonly Dictionary<uint, EntityTrack> _tracks = new();
        private readonly List<uint> _expiredEntityIds = new();
        private readonly Simulator _simulator;
        
        private readonly float _snapshotTickRate;
        private readonly int _delayTicks;
        private readonly int _capacity;

        /// <summary>
        /// 当前播放的服务端Tick，取出包围当前Tick的两个快照，进行插值显示
        /// </summary>
        private double _playbackTick;
        private bool _hasPlayback;

        public AuthorityFrameInterpolation(Simulator simulator, int interpolationDelayTicks, int snapshotTickRate, int bufferCapacity = 32)
        {
            _simulator = simulator;
            _delayTicks = interpolationDelayTicks;  // 延迟插值帧，避免出现没有下一帧作为插值样本
            _snapshotTickRate = snapshotTickRate;
            _capacity = bufferCapacity;
        }

        /// <summary>
        /// 写入一帧权威快照，不改实体。
        /// </summary>
        public void AddFrame(uint entityId, in EntityAuthorityFrame frame)
        {
            if (!_tracks.TryGetValue(entityId, out EntityTrack track))
            {
                track = new EntityTrack
                {
                    Buffer = new AuthorityFrameBuffer(_capacity)
                };

                _tracks.Add(entityId, track);
            }

            track.Buffer.AddFrame(frame);
        }

        /// <summary>
        /// 沿快照 tick 推进播放时刻，并写出 Replica 画面。
        /// </summary>
        public void Present(float deltaTime)
        {
            if (_tracks.Count == 0) return;

            if (!TryGetLatestTick(out uint latestTick)) return;

            // 获取最新和最旧的快照，判断当前客户端播放服务端快照的Tick位置
            double newestPlayableTick = latestTick > _delayTicks ? latestTick - _delayTicks : 0;
            if (!_hasPlayback)
            {
                // 当前时间头不处于Tick范围内，强制将快照播放时间头防止再最新位置
                // 通常出现在首次接收服务端快照时，丢失前面的服务端Tick时间
                // 则直接从收到的最新服务端Tick开始推进模拟时间
                if (!CanStartPlayback()) return;

                _playbackTick = newestPlayableTick;
                _hasPlayback = true;
            }
            else
            {
                _playbackTick += deltaTime * _snapshotTickRate;
                if (_playbackTick > newestPlayableTick)
                {
                    _playbackTick = newestPlayableTick;
                }
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

                PresentEntity(pair.Value, character, _playbackTick);
            }

            for (int i = 0; i < _expiredEntityIds.Count; i++)
            {
                _tracks.Remove(_expiredEntityIds[i]);
            }
        }

        /// <summary>
        /// 判断是否已经形成可播放窗口。
        /// </summary>
        private bool CanStartPlayback()
        {
            foreach (EntityTrack track in _tracks.Values)
            {
                AuthorityFrameBuffer buffer = track.Buffer;
                if (buffer.Count < 2) continue;

                if (buffer.LatestTick - buffer.OldestTick >= _delayTicks)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryGetLatestTick(out uint latestTick)
        {
            latestTick = 0;
            bool hasFrame = false;

            foreach (EntityTrack track in _tracks.Values)
            {
                AuthorityFrameBuffer buffer = track.Buffer;
                if (buffer.Count == 0) continue;

                if (!hasFrame || buffer.LatestTick > latestTick)
                {
                    latestTick = buffer.LatestTick;
                    hasFrame = true;
                }
            }

            return hasFrame;
        }

        /// <summary>
        /// 根据playbackTick，获取包围playbackTick的两个快照，进行快照插值
        /// </summary>
        private void PresentEntity(EntityTrack track, EntityCharacter character, double playbackTick)
        {
            AuthorityFrameBuffer buffer = track.Buffer;
            if (buffer.Count == 0) return;

            // 当前播放头Tick位置小于缓存的最旧的快照，直接使用最旧的快照
            if (playbackTick < buffer.OldestTick)
            {
                if (buffer.TryGetOldest(out EntityAuthorityFrame oldest))
                {
                    ApplyCrossed(track, character, buffer, oldest.snapshotTick);    // 直接应用离散状态
                    ApplySnapshot(character, oldest);   // 写入移动连续状态插值位置
                }

                return;
            }
            
            // 获取包围当前播放头Tick位置的两个快照
            if (!buffer.TryGetFrame(playbackTick, out EntityAuthorityFrame from, out EntityAuthorityFrame to))
            {
                // 当前播放头Tick位置大于缓存的最新的快照，直接使用最新的快照
                if (buffer.TryGetLatest(out EntityAuthorityFrame latest))
                {
                    ApplyCrossed(track, character, buffer, latest.snapshotTick);
                    ApplySnapshot(character, latest);
                }

                return;
            }
            
            // 离散状态使用包围当前播放头Tick位置前一个Tick的状态
            ApplyCrossed(track, character, buffer, from.snapshotTick);
            
            // 播放头Tick处于两个快照之间，执行插值逻辑
            float span = to.snapshotTick - from.snapshotTick;
            float alpha = Mathf.Clamp01((float)((playbackTick - from.snapshotTick) / span));

            // 应用已经插值过的快照状态
            InterpolateSnapshot(character, from, to, alpha);
        }
        
        /// <summary>
        /// 播放头跨过快照时写入离散状态，离散状态不能插值
        /// </summary>
        private void ApplyCrossed(EntityTrack track, EntityCharacter character, AuthorityFrameBuffer buffer, uint throughTick)
        {
            buffer.CopyFramesThrough(throughTick, track.AppliedSnapshotTick, track.HasAppliedSnapshot, _crossedFrames);
            for (int i = 0; i < _crossedFrames.Count; i++)
            {
                EntityAuthorityFrame frame = _crossedFrames[i];
                character.RestoreSnapshot(frame.state);
                track.AppliedSnapshotTick = frame.snapshotTick;
                track.HasAppliedSnapshot = true;
            }
        }
               
        #region 组件插值逻辑
        
        /// <summary>
        /// 直接应用快照表现，对于非离散状态则应使用插值，例如移动逻辑
        /// </summary>
        private void ApplySnapshot(EntityCharacter character, in EntityAuthorityFrame frame)
        {
            // 应用移动快照
            MovementSnapshot movement = frame.state.movement;
            character.PresentReplicaPose(movement.rootPosition, movement.meshRotation, frame.state.view.viewRotation);
        }

        private void InterpolateSnapshot(EntityCharacter character, in EntityAuthorityFrame from, in EntityAuthorityFrame to, float alpha)
        {
            // 插值实体姿态
            InterpolatePose(character, from, to, alpha);   
        }
        
        /// <summary>
        /// 插值实体移动旋转相关表现
        /// </summary>
        private void InterpolatePose(EntityCharacter character, in EntityAuthorityFrame from, in EntityAuthorityFrame to, float alpha)
        {
            MovementSnapshot fromMovement = from.state.movement;
            MovementSnapshot toMovement = to.state.movement;

            // 旋转使用球形插值
            character.PresentReplicaPose(
                Vector3.Lerp(fromMovement.rootPosition, toMovement.rootPosition, alpha),
                Quaternion.Slerp(fromMovement.meshRotation, toMovement.meshRotation, alpha),
                Quaternion.Slerp(from.state.view.viewRotation, to.state.view.viewRotation, alpha));
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

            _playbackTick = 0;
            _hasPlayback = false;
        }
    }
}