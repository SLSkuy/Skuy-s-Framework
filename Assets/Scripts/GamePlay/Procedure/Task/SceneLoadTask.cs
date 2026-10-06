using System;
using Framework;
using UnityEngine.SceneManagement;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 换场景这一段工作。场景 IO 自带加载界面、最短展示与激活时的资源清理，这里只等它的回调。
    /// 已经在目标场景时当场结束。
    /// </summary>
    public sealed class SceneLoadTask : ILoadTask
    {
        private readonly string _sceneName;
        private SceneLoader _sceneLoader;

        public SceneLoadTask(string sceneName)
        {
            _sceneName = sceneName;
        }

        #region 事件
        public event Action Finished;
        #endregion

        public bool IsFailed { get; private set; }

        public void Start()
        {
            if (SceneManager.GetActiveScene().name == _sceneName)
            {
                Finished?.Invoke();
                return;
            }

            _sceneLoader = Global.Get<SceneLoader>();
            _sceneLoader.Completed += OnSceneLoadCompleted;
            _sceneLoader.Failed += OnSceneLoadFailed;
            _sceneLoader.LoadScene(_sceneName);

            ClearRes();
            
            Global.ShowUI("LoadingPanel");
        }

        public void Update(float deltaTime)
        {

        }

        public void Stop()
        {
            if (_sceneLoader == null) return;

            _sceneLoader.Completed -= OnSceneLoadCompleted;
            _sceneLoader.Failed -= OnSceneLoadFailed;
            _sceneLoader = null;
        }

        private void ClearRes()
        {
            Global.Get<InstantiateManager>().ClearSceneGroups();
            Global.Get<ResourceManager>().ClearUnused();
            Global.HideAllUI();
        }

        #region 事件回调

        private void OnSceneLoadCompleted(string sceneName)
        {
            Finished?.Invoke();
        }

        private void OnSceneLoadFailed(string sceneName, string errorMessage)
        {
            IsFailed = true;
            Finished?.Invoke();
        }

        #endregion
    }
}
