using System.Collections.Generic;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体网络同步模块注册表，集中收集和查询实体下挂载的同步模块。
    /// </summary>
    [DisallowMultipleComponent]
    public class NetSyncModuleRegistry : MonoBehaviour, INetSyncModuleRegistry
    {
        private readonly List<INetSyncComponent> _modules = new();
        private readonly Dictionary<SyncModuleID, INetSyncComponent> _moduleMap = new();

        #region 属性
        public IReadOnlyList<INetSyncComponent> Modules => _modules;
        #endregion

        #region 模块注册
        /// <summary>
        /// 刷新实体下的同步模块。
        /// </summary>
        public void Refresh()
        {
            _modules.Clear();
            _moduleMap.Clear();

            MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is not INetSyncComponent syncComponent) continue;

                _modules.Add(syncComponent);
                _moduleMap[syncComponent.ModuleId] = syncComponent;
            }
        }

        /// <summary>
        /// 查询指定同步模块。
        /// </summary>
        public bool TryGetModule(SyncModuleID moduleId, out INetSyncComponent component)
        {
            if (_moduleMap.Count == 0)
            {
                Refresh();
            }

            return _moduleMap.TryGetValue(moduleId, out component);
        }

        /// <summary>
        /// 查询指定类型的同步模块。
        /// </summary>
        public bool TryGetModule<TModule>(SyncModuleID moduleId, out TModule module)
            where TModule : class, INetSyncComponent
        {
            module = null;
            if (!TryGetModule(moduleId, out INetSyncComponent component)) return false;

            module = component as TModule;
            return module != null;
        }
        #endregion

        #region 生命周期
        private void Awake()
        {
            Refresh();
        }
        #endregion
    }
}
