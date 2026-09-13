using UnityEngine;
using UnityEngine.SceneManagement;

namespace Framework
{
    /// <summary>
    /// 场景 IO：发出 Started/Completed/Failed，维护视觉进度与最短展示。不认识加载面板。
    /// </summary>
    public class SceneLoader : SubSystemBase
    {
        private const float INITIAL_DELAY = 0.3f;
        private const float MIN_LOAD_DURATION = 1.5f;
        private const float FULL_HOLD_DURATION = 0.2f;

        private AsyncOperation _currentOperation;
        private string _currentSceneName;
        private string _loadingSceneName;

        private float _elapsed;
        private float _holdElapsed;

        private bool _isLoading;
        private bool _isActivating;
        private bool _isHolding;

        #region 属性
        public override int Priority => (int)SubSystemPriority.SceneLoader;
        public float CurrentProgress { get; private set; }
        public bool IsCompleted { get; private set; }
        
        /// <summary>
        /// 是否正在加载场景（含最短展示与满格持有）。
        /// </summary>
        public bool IsLoading => _isLoading;
        #endregion

        // ReSharper disable Unity.PerformanceAnalysis
        public void LoadScene(string sceneName)
        {
            if (_isLoading)
            {
                Debug.LogWarning($"[SceneLoader] 正在加载 '{_loadingSceneName}'，忽略重复请求 '{sceneName}'");
                return;
            }

            if (SceneManager.GetActiveScene().name == sceneName)
            {
                Debug.LogWarning($"[SceneLoader] 当前已是 {sceneName} 场景，无需重复加载");
                return;
            }

            _loadingSceneName = sceneName;
            _currentOperation = null;

            _isLoading = true;
            _isActivating = false;
            _isHolding = false;
            IsCompleted = false;

            _elapsed = 0f;
            _holdElapsed = 0f;
            CurrentProgress = 0f;

            EventBus.Get<SceneLoadEvent.Started>().Dispatch(new SceneLoadEvent.StartedData
            {
                SceneName = _loadingSceneName
            });
        }

        public override void Update(float deltaTime)
        {
            if (!_isLoading) return;

            float dt = Time.unscaledDeltaTime;
            _elapsed += dt;

            if (_isHolding)
            {
                CurrentProgress = 1f;
                _holdElapsed += dt;
                if (_holdElapsed >= FULL_HOLD_DURATION)
                    CompleteLoading();
                return;
            }

            if (_elapsed < INITIAL_DELAY) return;

            if (_currentOperation == null)
            {
                _currentOperation = SceneManager.LoadSceneAsync(_loadingSceneName);
                if (_currentOperation == null)
                {
                    FailLoading($"[SceneLoader] 场景 '{_loadingSceneName}' 不存在或无法加载");
                    return;
                }

                _currentOperation.allowSceneActivation = false;
            }

            float realProgress = Mathf.Clamp01(_currentOperation.progress / 0.9f);
            float timeProgress = MIN_LOAD_DURATION <= 0f ? 1f : Mathf.Clamp01((_elapsed - INITIAL_DELAY) / MIN_LOAD_DURATION);
            CurrentProgress = Mathf.Min(realProgress, timeProgress) * 0.95f;

            if (realProgress >= 1f && timeProgress >= 1f && !_isActivating)
            {
                _isActivating = true;
                _currentOperation.allowSceneActivation = true;
            }

            if (_currentOperation.isDone && _isActivating)
                BeginFullHold();
        }

        private void BeginFullHold()
        {
            CurrentProgress = 1f;
            _currentSceneName = _loadingSceneName;
            Global.Get<SpawnManager>().Clear();
            Global.Get<ResourceManager>().ClearUnused();
            _isHolding = true;
            _holdElapsed = 0f;
        }

        private void CompleteLoading()
        {
            _isLoading = false;
            _isActivating = false;
            _isHolding = false;
            IsCompleted = true;
            EventBus.Get<SceneLoadEvent.Completed>().Dispatch(new SceneLoadEvent.CompletedData
            {
                SceneName = _currentSceneName
            });
        }

        private void FailLoading(string errorMessage)
        {
            _isLoading = false;
            _isActivating = false;
            _isHolding = false;
            _currentOperation = null;
            IsCompleted = false;
            EventBus.Get<SceneLoadEvent.Failed>().Dispatch(new SceneLoadEvent.FailedData
            {
                SceneName = _loadingSceneName,
                ErrorMessage = errorMessage
            });
        }
    }
}
