using System;
using Framework;
using UnityEngine.SceneManagement;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 换场景这一段工作。这里只等场景 IO 的回调，并在开载前清掉上一场留下的资源和界面。
    /// 加载界面由加载状态持有。已经在目标场景时当场结束。
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
