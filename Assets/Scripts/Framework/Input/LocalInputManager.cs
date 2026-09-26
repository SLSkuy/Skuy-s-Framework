using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 进程级本地输入：开战核通过 Global 获取，不经过壳类型。
    /// </summary>
    public sealed class LocalInputManager : SubSystemBase
    {
        private GameObject _container;

        #region 属性
        public override int Priority => (int)SubSystemPriority.LocalInputManager;
        public LocalInputProvider Provider { get; private set; }
        #endregion

        #region 子系统生命周期

        public override void Init()
        {
            GameObject root = GameObject.Find("[GameRoot]");
            _container = new GameObject("[LocalInput]");
            _container.transform.SetParent(root.transform);
            
            Provider = _container.AddComponent<LocalInputProvider>();
        }

        public override void Destroy()
        {
            if (_container)
            {
                Object.Destroy(_container);
                _container = null;
            }

            Provider = null;
        }

        #endregion
    }
}
