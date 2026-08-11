using System.Collections.Generic;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 网络同步模块注册表接口，提供模块枚举与类型化查询。
    /// </summary>
    public interface INetSyncModuleRegistry
    {
        /// <summary>
        /// 已注册同步模块。
        /// </summary>
        IReadOnlyList<INetSyncComponent> Modules { get; }

        /// <summary>
        /// 刷新实体下的同步模块。
        /// </summary>
        void Refresh();

        /// <summary>
        /// 查询指定同步模块。
        /// </summary>
        bool TryGetModule(SyncModuleID moduleId, out INetSyncComponent component);

        /// <summary>
        /// 查询指定类型的同步模块。
        /// </summary>
        bool TryGetModule<TModule>(SyncModuleID moduleId, out TModule module)
            where TModule : class, INetSyncComponent;
    }
}
