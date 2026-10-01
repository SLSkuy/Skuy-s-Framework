using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;

namespace Framework
{
    public sealed partial class InstantiateManager
    {
        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// 同步执行预制体实例化。
        /// </summary>
        private GameObject InstantiateFromLocation(string location, ResGroup group, InstantiateOptions options)
        {
            if (!ValidateLocation(location)) return null;
            if (!TryGetOrCreateEntry(location, group, out SpawnEntry entry)) return null;

            GameObject pooled = RentFromPool(entry, options);
            if (pooled) return pooled;

            AssetHandle handle = GetOrLoadHandle(location, entry, false);
            if (handle == null)
            {
                RemoveUnusedEntry(location, entry);
                return null;
            }

            Prewarm(entry, handle);
            pooled = RentFromPool(entry, options);
            if (pooled) return pooled;

            return InstantiateFromHandle(entry, handle, options);
        }

        /// <summary>
        /// 尝试从对象池借出一个实例。
        /// </summary>
        private GameObject RentFromPool(SpawnEntry entry, InstantiateOptions options)
        {
            while (entry.Idle.Count > 0)
            {
                InstanceEntry tracked = entry.Idle.Dequeue();
                GameObject instance = tracked.Instance;

                if (!instance)
                    continue;

                ResetPooledInstance(tracked);
                ApplyOptions(instance, options);

                entry.LentCount++;
                _lent[instance] = tracked;
                entry.IdleElapsed = 0f;

                return instance;
            }

            return null;
        }

        /// <summary>
        /// 借出时套用与新建实例相同的父节点、位姿和激活状态。无父节点时放入当前激活场景。
        /// </summary>
        private static void ApplyOptions(GameObject instance, InstantiateOptions options)
        {
            if (options.Parent)
                instance.transform.SetParent(options.Parent, options.InWorldSpace);
            else
            {
                instance.transform.SetParent(null);
                SceneManager.MoveGameObjectToScene(instance, SceneManager.GetActiveScene());
            }

            instance.transform.SetPositionAndRotation(options.Position, options.Rotation);
            instance.SetActive(options.IsActive);
        }

        /// <summary>
        /// 还池或再次借出前重置。组件列表在创建时缓存。
        /// </summary>
        private static void ResetPooledInstance(InstanceEntry tracked)
        {
            IPoolable[] poolables = tracked.Poolables;
            for (int i = 0; i < poolables.Length; i++)
                poolables[i].Reset();
        }

        /// <summary>
        /// 从资源句柄实例化 GameObject，并登记为借出实例。
        /// </summary>
        private GameObject InstantiateFromHandle(SpawnEntry entry, AssetHandle handle, InstantiateOptions options)
        {
            InstanceEntry tracked = CreateTrackedInstance(entry, handle, options);
            if (tracked == null) return null;

            entry.LentCount++;
            _lent[tracked.Instance] = tracked;

            return tracked.Instance;
        }

        /// <summary>
        /// 按策略把空闲实例预热进池。只执行一次，数量不超过空闲上限。
        /// </summary>
        private void Prewarm(SpawnEntry entry, AssetHandle handle)
        {
            if (entry.Prewarmed)
                return;

            entry.Prewarmed = true;
            int count = entry.Policy.prewarmCount;
            if (count > entry.Policy.maxIdleCount)
                count = entry.Policy.maxIdleCount;

            InstantiateOptions options = new(false, _groupRoots[(int)entry.Group], false);
            for (int i = 0; i < count; i++)
            {
                InstanceEntry tracked = CreateTrackedInstance(entry, handle, options);
                if (tracked == null)
                    break;

                entry.Idle.Enqueue(tracked);
            }
        }

        private InstanceEntry CreateTrackedInstance(SpawnEntry entry, AssetHandle handle, InstantiateOptions options)
        {
            GameObject instance = handle.InstantiateSync(options);
            if (!instance) return null;

            IPoolable[] poolables = instance.GetComponentsInChildren<IPoolable>(true);
            if (poolables.Length == 0 && entry.Policy.requirePoolable)
            {
                DestroyInstance(instance);
                throw new InvalidOperationException($"实例 {instance.name} 未实现 {nameof(IPoolable)}，不能进入 {entry.Group} 池。");
            }

            return new InstanceEntry
            {
                Instance = instance,
                Spawn = entry,
                Poolables = poolables
            };
        }
    }
}
