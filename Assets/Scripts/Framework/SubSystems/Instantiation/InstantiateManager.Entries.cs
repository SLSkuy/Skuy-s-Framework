using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace Framework
{
    public sealed partial class InstantiateManager
    {
        /// <summary>
        /// 一个资源位置对应的实例化池
        /// </summary>
        private sealed class SpawnEntry
        {
            /// <summary>
            /// 该资源位置对应的预制体句柄
            /// </summary>
            public AssetHandle Handle;

            /// <summary>
            /// 当前空闲实例
            /// </summary>
            public readonly Queue<InstanceEntry> Idle = new();

            /// <summary>
            /// 当前借出的实例数量
            /// </summary>
            public int LentCount;

            /// <summary>
            /// 当前等待资源加载的请求
            /// </summary>
            public readonly List<PendingInstantiate> Pending = new();

            /// <summary>
            /// 当前等待资源加载的请求数量
            /// </summary>
            public int PendingCount;

            /// <summary>
            /// 资源完全空闲后的累计时间
            /// </summary>
            public float IdleElapsed;

            /// <summary>
            /// 该资源位置第一次建池时确定的分组
            /// </summary>
            public ResGroup Group;

            /// <summary>
            /// 建池时记下的分组策略
            /// </summary>
            public ResGroupPolicy.Setting Policy;

            /// <summary>
            /// 是否已经按策略预热过
            /// </summary>
            public bool Prewarmed;

            /// <summary>
            /// 句柄完成回调。只绑定一次，句柄替换后重新绑定。
            /// </summary>
            public Action<AssetHandle> OnCompleted;

            public bool CompletionBound;
        }

        /// <summary>
        /// 一个池化实例的元数据。IPoolable 在创建时缓存，借还不再查找组件。
        /// 分组策略不要求 IPoolable 时该数组可以为空，复位交给业务自己做。
        /// </summary>
        private sealed class InstanceEntry
        {
            public GameObject Instance;
            public SpawnEntry Spawn;
            public IPoolable[] Poolables;
        }

        /// <summary>
        /// 等待完成的异步实例化请求
        /// </summary>
        private sealed class PendingInstantiate
        {
            public readonly string Location;
            public readonly ResGroup Group;
            public readonly InstantiateOptions Options;
            public readonly TaskCompletionSource<GameObject> Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public PendingInstantiate(string location, ResGroup group, InstantiateOptions options)
            {
                Location = location;
                Group = group;
                Options = options;
            }
        }
    }
}
