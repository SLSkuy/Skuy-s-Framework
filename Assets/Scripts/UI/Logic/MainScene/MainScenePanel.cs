using Core;
using Framework;
using GamePlay.Procedure;

namespace UI
{
    /// <summary>
    /// MainScenePanel 的界面逻辑
    /// </summary>
    public partial class MainScenePanel
    {
        private AppModelProxy _appModelProxy;
        
        #region 生命周期

        protected override void Init()
        {
            _appModelProxy = Global.GetDataProxy<AppModelProxy>();
        }

        protected override void AddListener()
        {
            // TODO: 订阅界面以外的事件
            // <ui-bind:add>
            _exitBtn.onClick.AddListener(OnExitBtn);
            _hostPlayBtn.onClick.AddListener(OnHostPlayBtn);
            _localPlayBtn.onClick.AddListener(OnLocalPlayBtn);
            _multiPlayBtn.onClick.AddListener(OnMultiPlayBtn);
            _renameBtn.onClick.AddListener(OnRenameBtn);
            _settings.onClick.AddListener(OnSettings);
            // </ui-bind:add>
        }

        protected override void RemoveListener()
        {
            // TODO: 取消订阅，并交给基类清理控制器事件
            // <ui-bind:remove>
            _exitBtn.onClick.RemoveListener(OnExitBtn);
            _hostPlayBtn.onClick.RemoveListener(OnHostPlayBtn);
            _localPlayBtn.onClick.RemoveListener(OnLocalPlayBtn);
            _multiPlayBtn.onClick.RemoveListener(OnMultiPlayBtn);
            _renameBtn.onClick.RemoveListener(OnRenameBtn);
            _settings.onClick.RemoveListener(OnSettings);
            // </ui-bind:remove>
            base.RemoveListener();
        }

        protected override void UpdateView()
        {
            _nameInputField.text = _appModelProxy.PlayerName;
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
            
        }

        private void OnExitBtn()
        {
            Global.QuitGame();
        }

        private void OnRenameBtn()
        {
            string playerName = _nameInputField.text;
            _appModelProxy.Rename(playerName);
        }

        #endregion
    }
}
