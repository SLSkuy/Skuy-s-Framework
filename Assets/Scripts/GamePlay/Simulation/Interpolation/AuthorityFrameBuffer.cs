using System;
using System.Collections.Generic;

namespace GamePlay.Simulation
{
    /// <summary>
    /// 远端权威帧缓存。按快照 tick 有序保存，供插值取前后两帧。
    /// </summary>
    public sealed class AuthorityFrameBuffer
    {
        private readonly List<EntityAuthorityFrame> _frames;
        private readonly int _maxFrameCount;

        #region 属性

        /// <summary>
        /// 当前缓存帧数量。
        /// </summary>
        public int Count => _frames.Count;

        /// <summary>
        /// 最旧快照 Tick。调用方需保证缓存非空。
        /// </summary>
        public uint OldestTick => _frames[0].snapshotTick;

        /// <summary>
        /// 最新快照 Tick。
        /// </summary>
        public uint LatestTick => _frames.Count > 0 ? _frames[^1].snapshotTick : 0;

        #endregion

        public AuthorityFrameBuffer(int maxFrameCount)
        {
            if (maxFrameCount < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(maxFrameCount), "权威帧缓存容量不能小于 2。");
            }

            _maxFrameCount = maxFrameCount;
            _frames = new List<EntityAuthorityFrame>(maxFrameCount);
        }

        /// <summary>
        /// 添加或覆盖同一快照 tick 的权威帧。
        /// </summary>
        public void AddFrame(in EntityAuthorityFrame frame)
        {
            for (int i = 0; i < _frames.Count; i++)
            {
                EntityAuthorityFrame current = _frames[i];
                if (current.snapshotTick == frame.snapshotTick)
                {
                    _frames[i] = frame;
                    return;
                }

                if (current.snapshotTick > frame.snapshotTick)
                {
                    _frames.Insert(i, frame);
                    TrimOverflow();
                    return;
                }
            }

            _frames.Add(frame);
            TrimOverflow();
        }

        /// <summary>
        /// 取包住播放时刻的前后两帧。播放时刻落在最新一帧上时没有后帧，返回 false。
        /// </summary>
        public bool TryGetFrame(double playbackTick, out EntityAuthorityFrame from, out EntityAuthorityFrame to)
        {
            from = default;
            to = default;
            if (_frames.Count < 2) return false;

            int fromIndex = -1;
            for (int i = 0; i < _frames.Count; i++)
            {
                if (_frames[i].snapshotTick <= playbackTick)
                {
                    fromIndex = i;
                    continue;
                }

                break;
            }

            if (fromIndex < 0 || fromIndex >= _frames.Count - 1) return false;

            from = _frames[fromIndex];
            to = _frames[fromIndex + 1];
            return true;
        }

        /// <summary>
        /// 取出已越过的帧之后、不超过 throughTick 的帧，按时间顺序写入。
        /// </summary>
        public void CopyFramesThrough(uint throughTick, uint appliedTick, bool hasApplied, List<EntityAuthorityFrame> destination)
        {
            destination.Clear();
            for (int i = 0; i < _frames.Count; i++)
            {
                uint tick = _frames[i].snapshotTick;
                if (tick > throughTick) break;
                if (hasApplied && tick <= appliedTick) continue;
                destination.Add(_frames[i]);
            }
        }

        /// <summary>
        /// 取最旧一帧。
        /// </summary>
        public bool TryGetOldest(out EntityAuthorityFrame frame)
        {
            if (_frames.Count == 0)
            {
                frame = default;
                return false;
            }

            frame = _frames[0];
            return true;
        }

        /// <summary>
        /// 取最新一帧。
        /// </summary>
        public bool TryGetLatest(out EntityAuthorityFrame frame)
        {
            if (_frames.Count == 0)
            {
                frame = default;
                return false;
            }

            frame = _frames[^1];
            return true;
        }

        /// <summary>
        /// 清空缓存。
        /// </summary>
        public void Clear()
        {
            _frames.Clear();
        }

        /// <summary>
        /// 删除多余的缓冲帧。
        /// </summary>
        private void TrimOverflow()
        {
            while (_frames.Count > _maxFrameCount)
            {
                _frames.RemoveAt(0);
            }
        }
    }
}
