using System.Threading.Tasks;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// Unity Resource方法资源加载器
    /// </summary>
    public class ResourcesLoader : IResourceLoader
    {
        public T Load<T>(string key) where T : Object
        {
            return Resources.Load<T>(key);
        }

        public async Task<T> LoadAsync<T>(string key) where T : Object
        {
            ResourceRequest request = Resources.LoadAsync<T>(key);

            await request;

            return request.asset as T;
        }

        public T[] LoadAll<T>(string key) where T : Object
        {
            return Resources.LoadAll<T>(key);
        }

        public void Unload(Object asset)
        {
            if (asset == null) return;

            if (asset is GameObject or Component) return;

            Resources.UnloadAsset(asset);
        }
    }
}
