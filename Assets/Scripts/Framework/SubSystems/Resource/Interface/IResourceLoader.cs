using YooAsset;
using Object = UnityEngine.Object;

namespace Framework
{
    /// <summary>
    /// 资源策略：拥有加载、卸载与句柄生命周期。
    /// </summary>
    public interface IResourceLoader
    {
        /// <summary>
        /// 初始化资源提供器
        /// </summary>
        void Init();
        
        /// <summary>
        /// 按资源位置同步加载
        /// </summary>
        AssetHandle Load<T>(string location) where T : Object;

        /// <summary>
        /// 按资源位置异步加载
        /// </summary>
        AssetHandle LoadAsync<T>(string location) where T : Object;
        
        /// <summary>
        /// 卸未使用资源，语义由策略定义。
        /// </summary>
        void ClearUnused();

        /// <summary>
        /// 关闭策略并卸掉仍存活的加载。
        /// </summary>
        void ClearAll();
    }
}
