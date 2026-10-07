using System;
using System.Collections.Generic;

namespace GamePlay.Simulation
{
    /// <summary>
    /// 客户端预测帧缓存。
    /// 保存最近一段时间内的预测结果，用于服务器校正后的回滚重演。
    /// </summary>
    public sealed class PredictionFrameBuffer
    {
        private readonly List<EntityPredictionFrame> _frames;
        private readonly int _maxFrameCount;

        #region 属性

        /// <summary>
        /// 当前缓存帧数量。
        /// </summary>
        public int Count => _frames.Count;

        /// <summary>
        /// 最新预测 Tick。
        /// </summary>
        public uint LatestTick => _frames.Count > 0 ? _frames[^1].tick : 0;

        #endregion
        
        public PredictionFrameBuffer(int maxFrameCount)
        {
            if (maxFrameCount < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(maxFrameCount), "预测缓存容量不能小于 2。");
            }

            _maxFrameCount = maxFrameCount;
            _frames = new List<EntityPredictionFrame>(maxFrameCount);
        }
        
        /// <summary>
        /// 添加或更新预测帧。
        /// </summary>
        public void AddFrame(in EntityPredictionFrame frame)
        {
            for (int i = 0; i < _frames.Count; i++)
            {
                EntityPredictionFrame current = _frames[i];
                if (current.tick == frame.tick)
                {
                    _frames[i] = frame;
                    return;
                }

                if (current.tick > frame.tick)
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
        /// 获取指定 Tick 的预测帧。
        /// </summary>
        public bool TryGetFrame(uint tick, out EntityPredictionFrame frame)
        {
            for (int i = 0; i < _frames.Count; i++)
            {
                if (_frames[i].tick == tick)
                {
                    frame = _frames[i];
                    return true;
                }
            }

            frame = default;
            return false;
        }

        /// <summary>
        /// 获取指定 Tick 之后的所有预测帧。
        /// 用于服务器校正后的输入重演。
        /// </summary>
        public void GetFramesAfter(uint tick, List<EntityPredictionFrame> result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            result.Clear();

            for (int i = 0; i < _frames.Count; i++)
            {
                if (_frames[i].tick > tick)
                {
                    result.Add(_frames[i]);
                }
            }
        }
        
        /// <summary>
        /// 删除指定 Tick 以及之前的预测帧。
        /// </summary>
        public void RemoveFramesUntil(uint tick)
        {
            int removeCount = 0;

            while (removeCount < _frames.Count &&
                   _frames[removeCount].tick <= tick)
            {
                removeCount++;
            }

            if (removeCount > 0)
            {
                _frames.RemoveRange(0, removeCount);
            }
        }

        /// <summary>
        /// 清空缓存。
        /// </summary>
        public void Clear()
        {
            _frames.Clear();
        }

        /// <summary>
        /// 删除多余的缓冲帧
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