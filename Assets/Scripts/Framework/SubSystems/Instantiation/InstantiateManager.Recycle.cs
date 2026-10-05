using System.Collections.Generic;
using UnityEngine;
using YooAsset;
using Object = UnityEngine.Object;

namespace Framework
{
    public sealed partial class InstantiateManager
    {
        /// <summary>
        /// 获取或加载指定资源位置的预制体句柄。
        /// </summary>
        private AssetHandle LoadHandle(string location, ResEntry entry, bool async)
        {
            if (entry.Handle != null) return entry.Handle;
            AssetHandle handle = async ? _resourceManager.LoadAsync<GameObject>(location) : _resourceManager.Load<GameObject>(location);

            if (handle == null || !handle.IsValid)
            {
                if (!async) LogInstantiateFailed(location);
                return null;
            }

            entry.Handle = handle;
            if (!handle.IsDone) return handle;
            if (handle.AssetObject is not GameObject)
            {
                if (!async) LogInstantiateFailed(location);

                DisposeHandle(entry);
                return null;
            }

            return handle;
        }

        /// <summary>
        /// 不进池实例化使用已缓存的预制体句柄，否则加载一次并在实例化后释放。
        /// </summary>
        private AssetHandle ResolveUnpooledHandle(string location, out bool disposeHandle)
        {
            if (_entries.TryGetValue(location, out ResEntry entry) && entry.Handle != null && entry.Handle.IsValid
                && entry.Handle.IsDone && entry.Handle.AssetObject is GameObject)
            {
                disposeHandle = false;
                return entry.Handle;
            }

            AssetHandle handle = _resourceManager.Load<GameObject>(location);
            if (handle == null || !handle.IsValid || handle.AssetObject is not GameObject)
            {
                LogInstantiateFailed(location);
                handle?.Dispose();
                disposeHandle = false;
                return null;
            }

            disposeHandle = true;
            return handle;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// 卸载已经没有借出和等待加载的资源池。仍在使用时不释放句柄。
        /// </summary>
        private bool TryUnloadEntry(string location, ResEntry entry)
        {
            if (entry.LentCount > 0 || entry.PendingCount > 0)
                return false;

            while (entry.Idle.Count > 0)
                DestroyInstance(entry.Idle.Dequeue().Instance);

            DisposeHandle(entry);
            _entries.Remove(location);
            return true;
        }

        /// <summary>
        /// 当资源池已经没有任何使用者时移除资源池。
        /// </summary>
        private void RemoveUnusedEntry(string location, ResEntry entry)
        {
            if (entry.LentCount > 0 || entry.PendingCount > 0 || entry.Idle.Count > 0)
                return;

            if (!_entries.TryGetValue(location, out ResEntry current) || !ReferenceEquals(current, entry))
                return;

            DisposeHandle(entry);
            _entries.Remove(location);
        }

        private void DisposeHandle(ResEntry entry)
        {
            AssetHandle handle = entry.Handle;
            if (handle == null)
                return;

            if (entry.CompletionBound && entry.OnCompleted != null)
            {
                handle.Completed -= entry.OnCompleted;
                entry.CompletionBound = false;
            }

            entry.Handle = null;
            handle.Dispose();
        }

        private void TickIdleUnload(float deltaTime)
        {
            _unloadScratch.Clear();

            foreach (KeyValuePair<string, ResEntry> pair in _entries)
            {
                ResEntry entry = pair.Value;

                if (!entry.Policy.idleUnload || entry.LentCount > 0 || entry.PendingCount > 0)
                {
                    entry.IdleElapsed = 0f;
                    continue;
                }

                if (entry.Idle.Count == 0)
                {
                    entry.IdleElapsed = 0f;
                    continue;
                }

                entry.IdleElapsed += deltaTime;

                if (entry.IdleElapsed >= entry.Policy.idleUnloadSeconds) _unloadScratch.Add(pair.Key);
            }

            foreach (var location in _unloadScratch)
            {
                if (!_entries.TryGetValue(location, out ResEntry entry)) continue;
                TryUnloadEntry(location, entry);
            }

            _unloadScratch.Clear();
        }

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// 取得资源池。不存在则按本次分组创建。已存在且分组不同则拒绝。
        /// </summary>
        private bool TryGetOrCreateEntry(string location, ResGroup group, out ResEntry entry)
        {
            if (_entries.TryGetValue(location, out entry))
            {
                if (entry.Group == group)
                    return true;

                Debug.LogError($"[{nameof(InstantiateManager)}] '{location}' is already in {entry.Group}, refused {group}.");
                entry = null;
                return false;
            }

            entry = new ResEntry
            {
                Group = group,
                Policy = ResGroupPolicy.Get(group)
            };
            _entries.Add(location, entry);
            return true;
        }

        private static bool ValidateLocation(string location)
        {
            if (!string.IsNullOrEmpty(location)) return true;

            LogInstantiateFailed(location);
            return false;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private static void LogInstantiateFailed(string location)
        {
            if (string.IsNullOrEmpty(location))
                Debug.LogError("[SpawnManager] Instantiate failed: asset location is empty.");
            else
                Debug.LogError($"[SpawnManager] Instantiate failed: '{location}'.");
        }

        private void ClearLentInstances()
        {
            foreach (GameObject instance in _lent.Keys) DestroyInstance(instance);

            _lent.Clear();
        }

        private void ClearIdleInstances()
        {
            foreach (ResEntry entry in _entries.Values)
            {
                while (entry.Idle.Count > 0) DestroyInstance(entry.Idle.Dequeue().Instance);
            }
        }

        private void ClearEntries()
        {
            foreach (ResEntry entry in _entries.Values)
                DisposeHandle(entry);

            _entries.Clear();
        }

        private void ClearUnpooledInstances()
        {
            foreach (GameObject instance in _unpooled)
                DestroyInstance(instance);

            _unpooled.Clear();
        }

        private static void DestroyInstance(GameObject instance)
        {
            if (!instance) return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(instance);
                return;
            }
#endif

            Object.Destroy(instance);
        }
    }
}
