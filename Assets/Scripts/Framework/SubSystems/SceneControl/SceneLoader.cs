using UnityEngine;
using UnityEngine.SceneManagement;

namespace Framework
{
    /// <summary>
    /// 场景加载器，场景加载完毕/失败会触发 SceneLoadEvent.Completed/Failed 全局事件
    /// </summary>
    public class SceneLoader : SubSystemBase
    {
        public override int Priority => (int)SubSystemPriority.SceneLoader;

        public float CurrentProgress { get; private set; }

        /// <summary>
        /// 场景是否已经完成加载
        /// </summary>
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// 是否正在加载场景（含最短展示时间）。
        /// </summary>
        public bool IsLoading => _isLoading;

        /// <summary>
        /// 视觉上的最短加载时间
        /// </summary>
        private const float MIN_LOAD_DURATION = 1.5f;

        /// <summary>
        /// 开始加载前的延迟
        /// </summary>
        private const float INITIAL_DELAY = 0.3f;

        private AsyncOperation _currentOperation;
        private string _currentSceneName;
        private string _loadingSceneName;

        private float _elapsed;

        private bool _isLoading;
        private bool _isActivating;

        public void LoadScene(string sceneName)
        {
            if (_isLoading)
            {
                Debug.LogWarning($"[SceneLoader] 正在加载 '{_loadingSceneName}'，忽略重复请求 '{sceneName}'");
                return;
            }

            _loadingSceneName = sceneName;

            _isLoading = true;
            _isActivating = false;
            IsCompleted = false;

            _elapsed = 0;
            CurrentProgress = 0;

            _currentOperation = null;

            // 显示将要加载的界面
            // TODO: 加载动画
            // Global.Get<UIManager>().ShowUI("LoadingProgressBar");
        }

        public override void Update(float deltaTime)
        {
            if (!_isLoading) return;

            _elapsed += deltaTime;
            if (_elapsed < INITIAL_DELAY) return;

            if (_currentOperation == null)
            {
                _currentOperation = SceneManager.LoadSceneAsync(_loadingSceneName);
                if (_currentOperation == null)
                {
                    DispatchFailed($"[SceneLoader] 场景 '{_loadingSceneName}' 不存在或无法加载");
                    _isLoading = false;
                    return;
                }

                _currentOperation.allowSceneActivation = false;
            }

            float realProgress = Mathf.Clamp01(_currentOperation.progress / 0.9f);
            float timeProgress = Mathf.Clamp01((_elapsed - INITIAL_DELAY) / MIN_LOAD_DURATION);

            // 最大只显示到95%
            CurrentProgress = Mathf.Min(realProgress, timeProgress) * 0.95f;

            // 真实加载完成
            if (realProgress >= 1f && timeProgress >= 1f && !_isActivating)
            {
                _isActivating = true;
                _currentOperation.allowSceneActivation = true;
            }

            if (_currentOperation.isDone && _isActivating)
            {
                _isLoading = false;
                _currentSceneName = _loadingSceneName;
                IsCompleted = true;
                
                Global.Get<ResourceManager>().ClearUnused();
                DispatchCompleted();
            }
        }

        private void DispatchCompleted()
        {
            EventBus.Get<SceneLoadEvent.Completed>().Dispatch(new SceneLoadEvent.CompletedData
            { 
                SceneName = _currentSceneName
            });
        }

        private void DispatchFailed(string errorMessage)
        {
            EventBus.Get<SceneLoadEvent.Failed>().Dispatch(new SceneLoadEvent.FailedData
            {
                SceneName = _loadingSceneName,
                ErrorMessage = errorMessage
            });
        }
    }
}