using System;
using System.Collections.Generic;

namespace GamePlay.EntitySystem
{
    public sealed class SnapshotBuffer<T> where T : struct, IEntitySnapshot
    {
        private readonly List<T> _items;
        private readonly int _capacity;

        #region Properties
        public int Count => _items.Count;
        public uint OldestTick => _items.Count == 0 ? 0 : _items[0].SnapshotTick;
        public uint LatestTick => _items.Count == 0 ? 0 : _items[^1].SnapshotTick;
        #endregion

        public SnapshotBuffer(int capacity)
        {
            if (capacity < 2) throw new ArgumentOutOfRangeException(nameof(capacity), "快照缓冲容量不能小于 2。");
            _capacity = capacity;
            _items = new List<T>(capacity);
        }

        public void Add(in T snapshot)
        {
            int insertIndex = _items.Count;
            for (int i = 0; i < _items.Count; i++)
            {
                uint bufferedTick = _items[i].SnapshotTick;
                if (bufferedTick == snapshot.SnapshotTick)
                {
                    _items[i] = snapshot;
                    return;
                }
                if (bufferedTick > snapshot.SnapshotTick)
                {
                    insertIndex = i;
                    break;
                }
            }
            _items.Insert(insertIndex, snapshot);
            while (_items.Count > _capacity) _items.RemoveAt(0);
        }

        public bool TrySample(double renderServerTime, double simulationTickInterval, out T from, out T to, out float t)
        {
            from = default;
            to = default;
            t = 0f;
            if (_items.Count < 2 || simulationTickInterval <= 0d) return false;
            while (_items.Count > 2 && ToServerTime(_items[1].SnapshotTick, simulationTickInterval) <= renderServerTime)
                _items.RemoveAt(0);
            from = _items[0];
            to = _items[1];
            double fromTime = ToServerTime(from.SnapshotTick, simulationTickInterval);
            double toTime = ToServerTime(to.SnapshotTick, simulationTickInterval);
            if (toTime <= fromTime)
            {
                t = 1f;
                return true;
            }
            t = (float)Math.Clamp((renderServerTime - fromTime) / (toTime - fromTime), 0d, 1d);
            return true;
        }

        public void Clear() => _items.Clear();

        private static double ToServerTime(uint tick, double simulationTickInterval) => tick * simulationTickInterval;
    }
}
