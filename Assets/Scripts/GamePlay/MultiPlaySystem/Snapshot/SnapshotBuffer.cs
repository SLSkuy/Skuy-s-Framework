using System;
using System.Collections.Generic;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 按服务端 Tick 排序的有界快照缓冲。重复 Tick 会覆盖旧值。
    /// </summary>
    public sealed class SnapshotBuffer<T> where T : struct, IEntitySnapshot
    {
        private readonly List<T> _items;
        private readonly int _capacity;

        public int Count => _items.Count;
        public uint OldestTick => _items.Count == 0 ? 0 : _items[0].SnapshotTick;
        public uint LatestTick => _items.Count == 0 ? 0 : _items[^1].SnapshotTick;

        public SnapshotBuffer(int capacity)
        {
            if (capacity < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Snapshot capacity must be at least two.");
            }

            _capacity = capacity;
            _items = new List<T>(capacity);
        }

        public void Clear() => _items.Clear();

        public void Add(in T snapshot)
        {
            int insertIndex = _items.Count;
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                uint bufferedTick = _items[i].SnapshotTick;
                if (bufferedTick == snapshot.SnapshotTick)
                {
                    _items[i] = snapshot;
                    return;
                }

                if (bufferedTick < snapshot.SnapshotTick)
                {
                    insertIndex = i + 1;
                    break;
                }

                insertIndex = i;
            }

            _items.Insert(insertIndex, snapshot);
            if (_items.Count > _capacity) _items.RemoveAt(0);
        }

        public bool TryGetLatest(out T snapshot)
        {
            if (_items.Count == 0)
            {
                snapshot = default;
                return false;
            }

            snapshot = _items[^1];
            return true;
        }

        /// <summary>
        /// 在服务端时间轴上查找渲染时刻两侧的快照。
        /// </summary>
        public bool TrySample(double renderServerTime, double simulationTickInterval,
            out T from, out T to, out float t)
        {
            if (_items.Count < 2 || simulationTickInterval <= 0d)
            {
                from = default;
                to = default;
                t = 0f;
                return false;
            }

            while (_items.Count > 2 && ToServerTime(_items[1].SnapshotTick, simulationTickInterval) <= renderServerTime)
            {
                _items.RemoveAt(0);
            }

            from = _items[0];
            to = _items[1];
            double fromTime = ToServerTime(from.SnapshotTick, simulationTickInterval);
            double toTime = ToServerTime(to.SnapshotTick, simulationTickInterval);
            double span = Math.Max(simulationTickInterval, toTime - fromTime);
            double sample = (renderServerTime - fromTime) / span;
            t = (float)Math.Max(0d, Math.Min(1d, sample));
            return true;
        }

        private static double ToServerTime(uint tick, double simulationTickInterval)
        {
            return tick * simulationTickInterval;
        }
    }
}
