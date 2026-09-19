using Framework;
using Framework.Panel;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Loading
{
    /// <summary>
    /// 进程级加载面板：订阅场景加载事件，显示期间读取视觉进度。
    /// </summary>
    public class LoadingPanelController : PanelController
    {
        [SerializeField] private Image progressFill;

        private SceneLoader _sceneLoader;
        private bool _tracking;

        protected override void Init()
        {
            progressFill.fillAmount = 0f;
        }

        protected override void AddListener()
        {
            SceneLoader sceneLoader = Global.Get<SceneLoader>();
            sceneLoader.Completed += HandleCompleted;
            sceneLoader.Failed += HandleFailed;
        }

        protected override void RemoveListener()
        {
            SceneLoader sceneLoader = Global.Get<SceneLoader>();
            sceneLoader.Completed -= HandleCompleted;
            sceneLoader.Failed -= HandleFailed;
            base.RemoveListener();
        }

        protected override void OnShow()
        {
            _sceneLoader = Global.Get<SceneLoader>();
            progressFill.fillAmount = _sceneLoader.CurrentProgress;
            _tracking = true;
        }

        protected override void WhileHiding()
        {
            _tracking = false;
        }

        private void Update()
        {
            if (!_tracking) return;

            progressFill.fillAmount = _sceneLoader.CurrentProgress;
        }

        private void HandleCompleted(string sceneName)
        {
            Hide();
        }

        private void HandleFailed(string sceneName, string errorMessage)
        {
            Hide();
        }
    }
}
