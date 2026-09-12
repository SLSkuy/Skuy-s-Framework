using Object = UnityEngine.Object;

namespace Framework
{
    /// <summary>
    /// 资源策略：拥有加载、卸载与句柄生命周期。
    /// </summary>
    public interface IAssetProvider
    {
        /// <summary>
        /// 按资源位置同步加载，返回终态句柄（成功或失败，永不 null）。
        /// </summary>
        AssetHandle<T> Load<T>(AssetLocation location) where T : Object;

        /// <summary>
        /// 按资源位置异步加载，立即返回加载中句柄（成功或失败后终态，永不 null）。
        /// </summary>
        AssetHandle<T> LoadAsync<T>(AssetLocation location) where T : Object;

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
