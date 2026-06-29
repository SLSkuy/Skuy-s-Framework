using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 资源对象信息
    /// </summary>
    public sealed class ResourceInfo
    {
        public readonly Object Asset;
        public int RefCount;
        public int InstanceCount;
        public bool ManagedCache;
        public readonly bool Persistent;

        public ResourceInfo(Object asset, bool persistent, int refCount = 0)
        {
            Asset = asset;
            Persistent = persistent;
            RefCount = refCount;
            InstanceCount = 0;
            ManagedCache = false;
        }
    }
}
