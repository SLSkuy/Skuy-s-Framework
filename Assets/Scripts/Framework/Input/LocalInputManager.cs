using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 进程级本地输入：开战核通过 Global 获取，不经过壳类型。
    /// </summary>
    public sealed class LocalInputManager : SubSystemBase
    {
        private GameObject _host;

        #region 属性
        public override int Priority => (int)SubSystemPriority.LocalInputManager;
        public LocalInputProvider Provider { get; private set; }
        #endregion

        #region 子系统生命周期

        public override void Init()
        {
            GameObject root = GameObject.Find("[GameRoot]");
            _host = new GameObject("[LocalInput]");
            _host.transform.SetParent(root.transform);
            
            Provider = _host.AddComponent<LocalInputProvider>();
        }

        public override void Destroy()
        {
            if (_host)
            {
                Object.Destroy(_host);
                _host = null;
            }

            Provider = null;
        }

        #endregion
    }
}
