namespace Framework
{
    /// <summary>
    /// 数据代理基类，定义虚函数，封装资源获取方法，让外部能便捷访问
    /// </summary>
    public abstract class BaseDataProxy : IDataProxy
    {
        public abstract string DataPath { get; }
        public abstract bool IsNeedSaveToLocal { get; }
        public bool IsInitialized { get; private set; }
        
        /// <summary>
        /// 标记数据为脏，需要重新存储，避免重复写入存档文件
        /// </summary>
        private bool _dataChanged = false;

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
        /// 标记数据为脏数据，数据已经变更，需要重新保存
        /// </summary>
        public void MarkDataDirty()
        {
            if(!_dataChanged) _dataChanged = true;
        }

        /// <summary>
        /// 加载本地数据，适用于存储在本地的数据
        /// 若需加载本地数据需重写加载逻辑
        /// </summary>
        public virtual void Load()
        {
            
        }

        private void _Save()
        {
            // 只有变更了的数据才需要重新保存
            if (!_dataChanged) return;
            
            _dataChanged = false;
            Save();
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
        public virtual void Destroy()
        {
            
        }

        public void _Destroy()
        {
            if (!IsInitialized) return;

            if (IsNeedSaveToLocal) _Save();
            Destroy();
            IsInitialized = false;
        }
    }
}
