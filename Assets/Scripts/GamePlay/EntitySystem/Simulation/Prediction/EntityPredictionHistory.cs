using System;
using System.Collections.Generic;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 客户端预测命令缓存
    /// </summary>
    public sealed class EntityPredictionHistory
    {
        private readonly List<EntityPredictionState> _frames;
        private readonly int _capacity;

        #region 属性
        public int Count => _frames.Count;
        public uint LatestTick => _frames.Count == 0 ? 0 : _frames[^1].tick;
        #endregion

        public EntityPredictionHistory(int capacity)
        {
            if (capacity < 2) throw new ArgumentOutOfRangeException(nameof(capacity), "预测历史容量不能小于 2。");
            _capacity = capacity;
            _frames = new List<EntityPredictionState>(capacity);
        }

        public void Add(in EntityPredictionState state)
        {
            int insertIndex = _frames.Count;
            for (int i = 0; i < _frames.Count; i++)
            {
                if (_frames[i].tick == state.tick)
                {
                    _frames[i] = state;
                    return;
                }
                if (_frames[i].tick > state.tick)
                {
                    insertIndex = i;
                    break;
                }
            }
            _frames.Insert(insertIndex, state);
            while (_frames.Count > _capacity) _frames.RemoveAt(0);
        }

        public bool TryGet(uint tick, out EntityPredictionState state)
        {
            for (int i = 0; i < _frames.Count; i++)
            {
                if (_frames[i].tick == tick)
                {
                    state = _frames[i];
                    return true;
                }
            }
            state = default;
            return false;
        }

        public void CopyAfter(uint tick, List<EntityPredictionState> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            for (int i = 0; i < _frames.Count; i++) if (_frames[i].tick > tick) destination.Add(_frames[i]);
        }

        public void RemoveThrough(uint tick)
        {
            int removeCount = 0;
            while (removeCount < _frames.Count && _frames[removeCount].tick <= tick) removeCount++;
            if (removeCount > 0) _frames.RemoveRange(0, removeCount);
        }

        public void Clear() => _frames.Clear();
    }
}
