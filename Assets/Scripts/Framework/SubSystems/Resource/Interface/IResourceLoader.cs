using System.Threading.Tasks;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 资源加载器接口，定义资源加载器方法
    /// </summary>
    public interface IResourceLoader
    {
        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="key">资源名称</param>
        /// <typeparam name="T">资源类型</typeparam>
        T Load<T>(string key) where T : Object;
        
        /// <summary>
        /// 异步加载
        /// </summary>
        /// <param name="key">资源名称</param>
        /// <typeparam name="T">资源类型</typeparam>
        Task<T> LoadAsync<T>(string key) where T : Object;

        /// <summary>
        /// 批量加载资源
        /// </summary>
        /// <param name="key">资源目录</param>
        /// <typeparam name="T">资源类型</typeparam>
        T[] LoadAll<T>(string key) where T : Object;
        
        /// <summary>
        /// 卸载资源
        /// </summary>
        /// <param name="asset">资源对象</param>
        void Unload(Object asset);
    }
}
