using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace Framework
{
    public sealed partial class InstantiateManager
    {
        /// <summary>
        /// 异步执行预制体实例化。
        /// </summary>
        private Task<GameObject> InstantiateAsyncFromLocation(string location, ResGroup group, InstantiateOptions options)
        {
            if (!ValidateLocation(location)) return Task.FromResult<GameObject>(null);
            if (!TryGetOrCreateEntry(location, group, out SpawnEntry entry))
                return Task.FromResult<GameObject>(null);

            GameObject pooled = RentFromPool(entry, options);
            if (pooled) return Task.FromResult(pooled);

            PendingInstantiate pending = new(location, group, options);
            entry.Pending.Add(pending);
            entry.PendingCount++;

            AssetHandle handle = GetOrLoadHandle(location, entry, true);
            if (handle == null)
            {
                entry.Pending.Remove(pending);
                entry.PendingCount--;
                pending.Completion.TrySetResult(null);
                RemoveUnusedEntry(location, entry);
                return pending.Completion.Task;
            }

            if (handle.IsDone)
                CompletePending(entry);
            else
                EnsureCompletion(entry);

            return pending.Completion.Task;
        }

        /// <summary>
        /// 句柄完成时，结束该资源池上的全部等待请求。
        /// </summary>
        private void EnsureCompletion(SpawnEntry entry)
        {
            if (entry.Handle == null || entry.CompletionBound)
                return;

            entry.OnCompleted ??= _ => CompletePending(entry);
            entry.CompletionBound = true;
            entry.Handle.Completed += entry.OnCompleted;
        }

        private void CompletePending(SpawnEntry entry)
        {
            if (entry.Pending.Count == 0)
                return;

            string location = entry.Pending[0].Location;
            AssetHandle handle = entry.Handle;
            if (handle == null || !handle.IsValid || handle.AssetObject is not GameObject)
            {
                LogInstantiateFailed(location);
                FailPending(entry);
                DisposeHandle(entry);
                RemoveUnusedEntry(location, entry);
                return;
            }

            Prewarm(entry, handle);

            while (entry.Pending.Count > 0)
            {
                PendingInstantiate pending = entry.Pending[0];
                entry.Pending.RemoveAt(0);
                entry.PendingCount--;

                if (pending.Group != entry.Group)
                {
                    Debug.LogError($"[{nameof(InstantiateManager)}] '{pending.Location}' is already in {entry.Group}, refused {pending.Group}.");
                    pending.Completion.TrySetResult(null);
                    continue;
                }

                GameObject instance = RentFromPool(entry, pending.Options);
                if (!instance)
                    instance = InstantiateFromHandle(entry, handle, pending.Options);
                if (!instance)
                {
                    LogInstantiateFailed(pending.Location);
                    pending.Completion.TrySetResult(null);
                    continue;
                }

                pending.Completion.TrySetResult(instance);
            }

            RemoveUnusedEntry(location, entry);
        }

        private void FailPending(SpawnEntry entry)
        {
            for (int i = 0; i < entry.Pending.Count; i++)
            {
                PendingInstantiate pending = entry.Pending[i];
                if (!pending.Completion.Task.IsCompleted)
                    pending.Completion.TrySetResult(null);
            }

            entry.PendingCount = 0;
            entry.Pending.Clear();
        }

        private void CancelScenePending()
        {
            foreach (SpawnEntry entry in _entries.Values)
            {
                if (!entry.Policy.sceneClear)
                    continue;

                for (int i = entry.Pending.Count - 1; i >= 0; i--)
                {
                    PendingInstantiate pending = entry.Pending[i];
                    if (!pending.Completion.Task.IsCompleted)
                        pending.Completion.TrySetResult(null);

                    entry.Pending.RemoveAt(i);
                }

                entry.PendingCount = 0;
            }
        }

        /// <summary>
        /// 销毁切场景分组中仍借出的实例。场景卸载后已销毁的实例只从登记中移除。
        /// </summary>
        private void DestroySceneLent()
        {
            _lentScratch.Clear();
            foreach (KeyValuePair<GameObject, InstanceEntry> pair in _lent)
            {
                if (pair.Value.Spawn.Policy.sceneClear)
                    _lentScratch.Add(pair.Key);
            }

            for (int i = 0; i < _lentScratch.Count; i++)
            {
                GameObject instance = _lentScratch[i];
                if (!_lent.TryGetValue(instance, out InstanceEntry tracked))
                    continue;

                _lent.Remove(instance);
                tracked.Spawn.LentCount--;
                DestroyInstance(instance);
            }

            _lentScratch.Clear();
        }

        /// <summary>
        /// 清理所有等待中的异步实例化请求。
        /// </summary>
        private void ClearPending()
        {
            foreach (SpawnEntry entry in _entries.Values)
            {
                for (int i = 0; i < entry.Pending.Count; i++)
                {
                    PendingInstantiate pending = entry.Pending[i];
                    if (!pending.Completion.Task.IsCompleted)
                        pending.Completion.TrySetResult(null);
                }

                entry.Pending.Clear();
                entry.PendingCount = 0;
            }
        }
    }
}
