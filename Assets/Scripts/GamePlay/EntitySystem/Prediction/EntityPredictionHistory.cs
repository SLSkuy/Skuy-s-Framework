using System;
using System.Collections.Generic;

namespace GamePlay.EntitySystem
{
    public sealed class EntityPredictionHistory
    {
        private readonly List<EntityPredictionFrame> _frames;
        private readonly int _capacity;

        #region Properties
        public int Count => _frames.Count;
        public uint LatestTick => _frames.Count == 0 ? 0 : _frames[^1].Tick;
        #endregion

        public EntityPredictionHistory(int capacity)
        {
            if (capacity < 2) throw new ArgumentOutOfRangeException(nameof(capacity), "预测历史容量不能小于 2。");
            _capacity = capacity;
            _frames = new List<EntityPredictionFrame>(capacity);
        }

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
            while (_frames.Count > _capacity) _frames.RemoveAt(0);
        }

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

        public void CopyAfter(uint tick, List<EntityPredictionFrame> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            for (int i = 0; i < _frames.Count; i++) if (_frames[i].Tick > tick) destination.Add(_frames[i]);
        }

        public void RemoveThrough(uint tick)
        {
            int removeCount = 0;
            while (removeCount < _frames.Count && _frames[removeCount].Tick <= tick) removeCount++;
            if (removeCount > 0) _frames.RemoveRange(0, removeCount);
        }

        public void Clear() => _frames.Clear();
    }
}
