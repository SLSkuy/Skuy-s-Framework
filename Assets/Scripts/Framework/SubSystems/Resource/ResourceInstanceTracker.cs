using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 跟踪Prefab实例，用于资源销毁
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ResourceInstanceTracker : MonoBehaviour
    {
        private ResourceManager _manager;
        private string _key;
        private bool _initialized;

        public void Init(ResourceManager manager, string key)
        {
            _manager = manager;
            _key = key;
            _initialized = true;
        }

        private void OnDestroy()
        {
            if (!_initialized) return;

            _initialized = false;
            _manager?.ReleaseInstance(_key);
            _manager = null;
            _key = null;
        }
    }
}
