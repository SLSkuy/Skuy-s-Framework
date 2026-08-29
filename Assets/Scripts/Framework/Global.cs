using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
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

        /// <summary>
        /// 由 SubSystemBase 内部调用，外部不应直接使用
        /// </summary>
        public static void Unregister(Type type)
        {
            SubSystems.Remove(type);
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
        /// 切换游戏状态
        /// </summary>
        /// <param name="state"></param>
        public static void ChangeState(GameState state)
        {
            Get<GameStateManager>()?.ChangeState(state);
        }

        /// <summary>
        /// 获取托管资源。外部不需要保存句柄，资源由 ResourceManager 自动回收。
        /// </summary>
        public static T GetAsset<T>(string path) where T : Object
        {
            return Get<ResourceManager>()?.GetAsset<T>(path);
        }

        /// <summary>
        /// 批量获取托管资源，适合配置表、ScriptableObject 数据目录
        /// </summary>
        public static IReadOnlyList<T> GetAllAssets<T>(string path) where T : Object
        {
            return Get<ResourceManager>()?.LoadAllAssets<T>(path);
        }

        /// <summary>
        /// 旧资源加载方法，需要在退出时手动调用Dispose清空资源，不推荐使用
        /// </summary>
        /// <param name="path"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static ResourceHandle<T> Load<T>(string path) where T : Object
        {
            return Get<ResourceManager>()?.Load<T>(path);
        }

        /// <summary>
        /// 实例化托管 Prefab，实例销毁时自动释放资源引用
        /// </summary>
        public static GameObject Instantiate(string path, Transform parent = null, bool worldPositionStays = false)
        {
            return Get<ResourceManager>()?.Instantiate(path, parent, worldPositionStays);
        }

        /// <summary>
        /// 实例化托管 Prefab，实例销毁时自动释放资源引用
        /// </summary>
        public static GameObject Instantiate(string path, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            return Get<ResourceManager>()?.Instantiate(path, position, rotation, parent);
        }

        /// <summary>
        /// 释放托管缓存，仅清除 ResourceManager 的缓存保留标记，不销毁业务对象实例
        /// </summary>
        public static void Release(string path)
        {
            Get<ResourceManager>()?.ReleaseManagedCache(path);
        }

        /// <summary>
        /// 按路径释放资源，适用于释放整组资源
        /// </summary>
        /// <param name="prefix"></param>
        public static void ReleaseByPrefix(string prefix)
        {
            Get<ResourceManager>()?.ReleaseManagedCacheByPrefix(prefix);
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        public static Task<ResourceHandle<T>> LoadResourceAsync<T>(string path) where T : Object
        {
            return Get<ResourceManager>()?.LoadAsync<T>(path);
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
