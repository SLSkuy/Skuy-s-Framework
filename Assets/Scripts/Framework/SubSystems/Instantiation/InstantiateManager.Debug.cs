#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace Framework
{
    public sealed partial class InstantiateManager
    {
        public struct DebugGroupStat
        {
            public ResGroup Group;
            public int Active;
            public int Idle;
        }

        public struct DebugLocationStat
        {
            public string Location;
            public ResGroup Group;
            public int Lent;
            public int Idle;
            public int Pending;
            public float IdleElapsed;
            public long MemoryBytes;
        }

        public struct DebugInstanceInfo
        {
            public GameObject Instance;
            public bool Active;
        }

        /// <summary>
        /// 当前池条目数量。仅调试查询时读取。
        /// </summary>
        public int DebugEntryCount => _entries.Count;

        /// <summary>
        /// 当前不进池实例数量。仅调试查询时读取。
        /// </summary>
        public int DebugUnpooledCount => _unpooled.Count;

        /// <summary>
        /// 当前等待加载的请求数量。仅调试查询时读取。
        /// </summary>
        public int DebugPendingCount
        {
            get
            {
                int count = 0;
                foreach (ResEntry entry in _entries.Values)
                    count += entry.PendingCount;
                return count;
            }
        }

        /// <summary>
        /// 按分组汇总借出与空闲数量。
        /// </summary>
        public void CopyDebugGroupStats(List<DebugGroupStat> destination)
        {
            destination.Clear();
            int groupCount = (int)ResGroup.Temp + 1;
            for (int i = 0; i < groupCount; i++)
            {
                destination.Add(new DebugGroupStat
                {
                    Group = (ResGroup)i,
                    Active = 0,
                    Idle = 0
                });
            }

            foreach (ResEntry entry in _entries.Values)
            {
                int index = (int)entry.Group;
                DebugGroupStat stat = destination[index];
                stat.Active += entry.LentCount;
                stat.Idle += entry.Idle.Count;
                destination[index] = stat;
            }
        }

        /// <summary>
        /// 按资源位置汇总借出、空闲、等待和实例内存。
        /// </summary>
        public void CopyDebugLocationStats(List<DebugLocationStat> destination)
        {
            destination.Clear();
            var lentMemory = new Dictionary<ResEntry, long>();
            foreach (KeyValuePair<GameObject, InstanceEntry> pair in _lent)
            {
                if (!pair.Key)
                    continue;

                long size = Profiler.GetRuntimeMemorySizeLong(pair.Key);
                lentMemory.TryGetValue(pair.Value.Res, out long current);
                lentMemory[pair.Value.Res] = current + size;
            }

            foreach (KeyValuePair<string, ResEntry> pair in _entries)
            {
                ResEntry entry = pair.Value;
                lentMemory.TryGetValue(entry, out long lentBytes);
                destination.Add(new DebugLocationStat
                {
                    Location = pair.Key,
                    Group = entry.Group,
                    Lent = entry.LentCount,
                    Idle = entry.Idle.Count,
                    Pending = entry.PendingCount,
                    IdleElapsed = entry.IdleElapsed,
                    MemoryBytes = lentBytes + MeasureIdleMemory(entry)
                });
            }
        }

        /// <summary>
        /// 列出一个资源位置下的借出与空闲实例。
        /// </summary>
        public bool TryCopyDebugInstances(string location, List<DebugInstanceInfo> destination)
        {
            destination.Clear();
            if (!_entries.TryGetValue(location, out ResEntry entry))
                return false;

            foreach (InstanceEntry tracked in entry.Idle)
            {
                destination.Add(new DebugInstanceInfo
                {
                    Instance = tracked.Instance,
                    Active = false
                });
            }

            foreach (KeyValuePair<GameObject, InstanceEntry> pair in _lent)
            {
                if (!ReferenceEquals(pair.Value.Res, entry))
                    continue;

                destination.Add(new DebugInstanceInfo
                {
                    Instance = pair.Key,
                    Active = true
                });
            }

            return true;
        }

        /// <summary>
        /// 列出不进池的实例。
        /// </summary>
        public void CopyDebugUnpooled(List<GameObject> destination)
        {
            destination.Clear();
            foreach (GameObject instance in _unpooled)
                destination.Add(instance);
        }

        private static long MeasureIdleMemory(ResEntry entry)
        {
            long bytes = 0;
            foreach (InstanceEntry tracked in entry.Idle)
            {
                if (tracked.Instance)
                    bytes += Profiler.GetRuntimeMemorySizeLong(tracked.Instance);
            }

            return bytes;
        }
    }
}
#endif
