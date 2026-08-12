using System;
using System.Collections.Generic;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 按 Tick 排序的有界预测历史，用于权威校正后的回滚与重放。
    /// </summary>
    public sealed class EntityPredictionHistory
    {
        private readonly List<EntityPredictionFrame> _frames;
        private readonly int _capacity;

        #region 属性

        public int Count => _frames.Count;
        public uint LatestTick => _frames.Count == 0 ? 0 : _frames[^1].Tick;

        #endregion

        /// <summary>
        /// 创建指定容量的预测历史。
        /// </summary>
        public EntityPredictionHistory(int capacity)
        {
            if (capacity < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "预测历史容量不能小于 2。");
            }

            _capacity = capacity;
            _frames = new List<EntityPredictionFrame>(capacity);
        }

        /// <summary>
        /// 记录或覆盖一个预测 Tick。
        /// </summary>
        public void Add(in EntityPredictionFrame frame)
        {
            int insertIndex = _frames.Count;
            for (int i = 0; i < _frames.Count; i++)
            {
                if (_frames[i].Tick == frame.Tick)
                {
                    _frames[i] = frame;
                    return;
                }

                if (_frames[i].Tick > frame.Tick)
                {
                    insertIndex = i;
                    break;
                }
            }

            _frames.Insert(insertIndex, frame);
            while (_frames.Count > _capacity)
            {
                _frames.RemoveAt(0);
            }
        }

        /// <summary>
        /// 查找指定 Tick 的预测帧。
        /// </summary>
        public bool TryGet(uint tick, out EntityPredictionFrame frame)
        {
            for (int i = 0; i < _frames.Count; i++)
            {
                if (_frames[i].Tick == tick)
                {
                    frame = _frames[i];
                    return true;
                }
            }

            frame = default;
            return false;
        }

        /// <summary>
        /// 将指定 Tick 之后的帧复制到目标列表，供调用方按序重放。
        /// </summary>
        public void CopyAfter(uint tick, List<EntityPredictionFrame> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            for (int i = 0; i < _frames.Count; i++)
            {
                if (_frames[i].Tick > tick)
                {
                    destination.Add(_frames[i]);
                }
            }
        }

        /// <summary>
        /// 删除已经被服务端确认的预测帧。
        /// </summary>
        public void RemoveThrough(uint tick)
        {
            int removeCount = 0;
            while (removeCount < _frames.Count && _frames[removeCount].Tick <= tick)
            {
                removeCount++;
            }

            if (removeCount > 0)
            {
                _frames.RemoveRange(0, removeCount);
            }
        }

        /// <summary>
        /// 清空预测历史。
        /// </summary>
        public void Clear()
        {
            _frames.Clear();
        }
    }
}
