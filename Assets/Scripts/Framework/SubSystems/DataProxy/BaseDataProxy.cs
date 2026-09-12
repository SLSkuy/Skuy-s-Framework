using System.Collections.Generic;

namespace Framework
{
    /// <summary>
    /// 数据代理基类，定义虚函数，封装资源获取方法，让外部能便捷访问
    /// </summary>
    public abstract class BaseDataProxy : IDataProxy
    {
        public abstract string DataName { get; }
        public abstract bool IsNeedSaveToLocal { get; }
        public bool IsInitialized { get; private set; }

        private readonly HashSet<string> _loadedAssetPaths = new();
        private readonly HashSet<string> _loadedAssetPrefixes = new();

        public virtual void Init()
        {
        }

        public void _Init()
        {
            if (IsInitialized) return;

            Load();
            Init();
            IsInitialized = true;
        }

        /// <summary>
        /// 加载本地数据，适用于存储在本地的数据
        /// 若需加载本地数据需重写加载逻辑
        /// </summary>
        public virtual void Load()
        {
        }

        /// <summary>
        /// 保存数据道本地，适用于需存储在本地的数据
        /// 若需保存数据需重写保存逻辑
        /// </summary>
        public virtual void Save()
        {
        }

        /// <summary>
        /// 清理当前的所有数据
        /// </summary>
        public virtual void Clear()
        {
        }

        public void _Clear()
        {
            if (!IsInitialized) return;

            if (IsNeedSaveToLocal) Save();
            Clear();
            IsInitialized = false;
        }
    }
}
