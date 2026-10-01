using Framework;

namespace UI
{
    /// <summary>
    /// LoadingPanel 的界面逻辑
    /// </summary>
    public partial class LoadingPanel
    {
        private SceneLoader _sceneLoader;
        private bool _tracking;
        
        #region 生命周期

        protected override void Init()
        {
            _progressBar.fillAmount = 0f;
        }
        
        protected override void AddListener()
        {
            _sceneLoader = Global.Get<SceneLoader>();
            _sceneLoader.Completed += HandleCompleted;
            _sceneLoader.Failed += HandleFailed;
        }

        protected override void RemoveListener()
        {
            _sceneLoader.Completed -= HandleCompleted;
            _sceneLoader.Failed -= HandleFailed;
            base.RemoveListener();
        }
        
        private void Update()
        {
            if (!_tracking) return;

            _progressBar.fillAmount = _sceneLoader.CurrentProgress;
        }
        
        protected override void OnShow()
        {
            _progressBar.fillAmount = _sceneLoader.CurrentProgress;
            _tracking = true;
        }
        
        protected override void OnHide()
        {
            _tracking = false;
        }

        protected override void UpdateView()
        {
            // TODO: 用界面属性刷新控件显示
        }

        #endregion

        #region UI回调

        private void HandleCompleted(string sceneName)
        {
            Hide();
        }

        private void HandleFailed(string sceneName, string errorMessage)
        {
            Hide();
        }

        #endregion
    }
}
