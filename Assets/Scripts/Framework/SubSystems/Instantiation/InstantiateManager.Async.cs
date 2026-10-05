using System.Linq;
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
            if (!TryGetOrCreateEntry(location, group, out ResEntry entry))
                return Task.FromResult<GameObject>(null);

            GameObject pooled = RentFromPool(entry, options);
            if (pooled) return Task.FromResult(pooled);

            PendingInstantiate pending = new(location, group, options);
            entry.Pending.Add(pending);
            entry.PendingCount++;

            AssetHandle handle = LoadHandle(location, entry, true);
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
                WaitComplete(entry);

            return pending.Completion.Task;
        }

        /// <summary>
        /// 还没加载完成，添加监听事件
        /// 句柄完成时，结束该资源池上的全部等待请求
        /// </summary>
        private void WaitComplete(ResEntry entry)
        {
            if (entry.Handle == null || entry.CompletionBound) return;

            entry.OnCompleted ??= _ => CompletePending(entry);
            entry.CompletionBound = true;
            entry.Handle.Completed += entry.OnCompleted;
        }

        private void CompletePending(ResEntry entry)
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
                if (!instance) instance = InstantiateFromHandle(entry, handle, pending.Options);
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

        private void FailPending(ResEntry entry)
        {
            foreach (var pending in entry.Pending.Where(pending => !pending.Completion.Task.IsCompleted))
            {
                pending.Completion.TrySetResult(null);
            }

            entry.PendingCount = 0;
            entry.Pending.Clear();
        }

        private void CancelScenePending()
        {
            foreach (var entry in _entries.Values.Where(entry => entry.Policy.sceneClear))
            {
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
            foreach (var pair in _lent.Where(pair => pair.Value.Res.Policy.sceneClear))
            {
                _lentScratch.Add(pair.Key);
            }
            foreach (var instance in _lentScratch)
            {
                if (!_lent.Remove(instance, out InstanceEntry tracked)) continue;

                tracked.Res.LentCount--;
                DestroyInstance(instance);
            }

            _lentScratch.Clear();
        }

        /// <summary>
        /// 清理所有等待中的异步实例化请求。
        /// </summary>
        private void ClearPending()
        {
            foreach (ResEntry entry in _entries.Values)
            {
                foreach (var pending in entry.Pending.Where(pending => !pending.Completion.Task.IsCompleted))
                {
                    pending.Completion.TrySetResult(null);
                }

                entry.Pending.Clear();
                entry.PendingCount = 0;
            }
        }
    }
}
