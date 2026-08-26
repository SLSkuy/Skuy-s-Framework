using System;
using System.Collections.Generic;
using GamePlay.EntitySystem;

namespace GamePlay.MultiPlaySystem
{
    public sealed class SnapshotBuffer
    {
        private readonly List<EntityAuthorityState> _items;
        private readonly int _capacity;

        #region Properties
        public int Count => _items.Count;
        public uint OldestTick => _items.Count == 0 ? 0 : _items[0].snapshotTick;
        public uint LatestTick => _items.Count == 0 ? 0 : _items[^1].snapshotTick;
        #endregion

        public SnapshotBuffer(int capacity)
        {
            if (capacity < 2) throw new ArgumentOutOfRangeException(nameof(capacity), "快照缓冲容量不能小于 2。");
            _capacity = capacity;
            _items = new List<EntityAuthorityState>(capacity);
        }

        public void Add(in EntityAuthorityState snapshot)
        {
            int insertIndex = _items.Count;
            for (int i = 0; i < _items.Count; i++)
            {
                uint bufferedTick = _items[i].snapshotTick;
                if (bufferedTick == snapshot.snapshotTick)
                {
                    _items[i] = snapshot;
                    return;
                }
                if (bufferedTick > snapshot.snapshotTick)
                {
                    insertIndex = i;
                    break;
                }
            }
            _items.Insert(insertIndex, snapshot);
            while (_items.Count > _capacity) _items.RemoveAt(0);
        }

        public bool TrySample(double renderServerTime, double simulationTickInterval, out EntityAuthorityState from, out EntityAuthorityState to, out float t)
        {
            from = default;
            to = default;
            t = 0f;
            if (_items.Count < 2 || simulationTickInterval <= 0d) return false;
            while (_items.Count > 2 && ToServerTime(_items[1].snapshotTick, simulationTickInterval) <= renderServerTime)
                _items.RemoveAt(0);
            from = _items[0];
            to = _items[1];
            double fromTime = ToServerTime(from.snapshotTick, simulationTickInterval);
            double toTime = ToServerTime(to.snapshotTick, simulationTickInterval);
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
