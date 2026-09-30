using Framework;
using GamePlay.Procedure;

namespace UI.MainScene
{
    /// <summary>
    /// MainScenePanel 的界面逻辑
    /// </summary>
    public partial class MainScenePanel
    {
        #region 生命周期

        protected override void Init()
        {
            // TODO: 初始化界面数据与控件状态
            // <ui-bind:add>
            _hostPlayBtn.onClick.AddListener(OnHostPlayBtn);
            _localPlayBtn.onClick.AddListener(OnLocalPlayBtn);
            _multiPlayBtn.onClick.AddListener(OnMultiPlayBtn);
            _settings.onClick.AddListener(OnSettings);
            // </ui-bind:add>
        }

        protected override void AddListener()
        {
            // TODO: 订阅界面以外的事件
        }

        protected override void RemoveListener()
        {
            // TODO: 取消订阅，并交给基类清理控制器事件
            // <ui-bind:remove>
            _hostPlayBtn.onClick.RemoveListener(OnHostPlayBtn);
            _localPlayBtn.onClick.RemoveListener(OnLocalPlayBtn);
            _multiPlayBtn.onClick.RemoveListener(OnMultiPlayBtn);
            _settings.onClick.RemoveListener(OnSettings);
            // </ui-bind:remove>
            base.RemoveListener();
        }

        protected override void UpdateView()
        {
            // TODO: 用界面属性刷新控件显示
        }

        #endregion

        #region UI回调

        private void OnLocalPlayBtn()
        {
            ProcedureCore.Instance.StartLocalPlay();
        }

        private void OnHostPlayBtn()
        {
            ProcedureCore.Instance.HostMultiplay();
        }

        private void OnMultiPlayBtn()
        {
            ProcedureCore.Instance.JoinRemote();
        }

        private void OnSettings()
        {
            Global.QuitGame();
        }

        #endregion
    }
}
