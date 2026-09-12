using System;
using System.Collections.Generic;
using YooAsset;
using Object = UnityEngine.Object;

namespace Framework
{
    /// <summary>
    /// 全局服务定位器，解耦业务层对 GameCore 的直接依赖
    /// </summary>
    public static class Global
    {
        private static readonly Dictionary<Type, ISubSystem> SubSystems = new();

        /// <summary>
        /// 由 SubSystemBase 内部调用，外部不应直接使用
        /// </summary>
        public static void Register(ISubSystem system)
        {
            SubSystems[system.GetType()] = system;
        }

        public static T Register<T>() where T : class, ISubSystem, new()
        {
            return Get<SystemManager>().RegisterSystem<T>();
        }

        /// <summary>
        /// 由 SubSystemBase 内部调用，外部不应直接使用
        /// </summary>
        public static void Unregister(ISubSystem system)
        {
            SubSystems.Remove(system.GetType());
        }

        public static void Unregister<T>() where T : class, ISubSystem, new()
        {
            Get<SystemManager>().UnregisterSystem<T>();
        }

        /// <summary>
        /// 获取已注册的子系统
        /// </summary>
        public static T Get<T>() where T : class, ISubSystem
        {
            SubSystems.TryGetValue(typeof(T), out var system);
            return system as T;
        }

        /// <summary>
        /// 尝试获取子系统
        /// </summary>
        public static bool TryGet<T>(out T system) where T : class, ISubSystem
        {
            if (SubSystems.TryGetValue(typeof(T), out var s))
            {
                system = s as T;
                return system != null;
            }

            system = null;
            return false;
        }

        /// <summary>
        /// 获取数据代理，转发到 DataProxyManager
        /// </summary>
        public static T GetDataProxy<T>() where T : class, IDataProxy
        {
            return Get<DataProxyManager>()?.GetDataProxy<T>();
        }
        
        /// <summary>
        /// 尝试获取数据代理，更推荐使用该方法，转发到 DataProxyManager
        /// </summary>
        public static bool TryGetDataProxy<T>(out T proxy) where T : class, IDataProxy
        {
            if (Get<DataProxyManager>()?.TryGetDataProxy(out T p)??false)
            {
                proxy = p;
                return p!=null;
            }

            proxy = null;
            return false;
        }
        
        /// <summary>
        /// 注册数据代理，转发到 DataProxyManager
        /// </summary>
        public static T RegisterDataProxy<T>() where T : class, IDataProxy, new()
        {
            return Get<DataProxyManager>()?.RegisterDataProxy<T>();
        }

        /// <summary>
        /// 注销数据代理
        /// </summary>
        public static void UnregisterDataProxy<T>() where T : class, IDataProxy
        {
            Get<DataProxyManager>()?.UnregisterDataProxy<T>();
        }

        /// <summary>
        /// 退出游戏。UI 与流程不要依赖壳类型。
        /// </summary>
        public static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }

        /// <summary>
        /// 同步加载资源
        /// </summary>
        public static AssetHandle Load<T>(string path) where T : Object
        {
            return Get<ResourceManager>().Load<T>(path);
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        public static AssetHandle LoadAsync<T>(string path) where T : Object
        {
            return Get<ResourceManager>().LoadAsync<T>(path);
        }

        public static void LoadScene(string sceneName)
        {
            Get<SceneLoader>().LoadScene(sceneName);
        }

        /// <summary>
        /// 程序退出时清理所有注册
        /// </summary>
        public static void Clear()
        {
            SubSystems.Clear();
        }
    }
}
