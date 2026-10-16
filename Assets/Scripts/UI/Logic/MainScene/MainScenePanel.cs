using Framework;
using GamePlay.DataProxy;
using GamePlay.Procedure;

namespace UI
{
    /// <summary>
    /// MainScenePanel 的界面逻辑
    /// </summary>
    public partial class MainScenePanel
    {
        private PlayerDataProxy _playerData;
        
        #region 生命周期

        protected override void Init()
        {
            _playerData = Global.GetDataProxy<PlayerDataProxy>();
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
            _nameInputField.text = _playerData.PlayerName;
        }

        #endregion

        #region UI回调

        private void OnLocalPlayBtn()
        {

        }

        private void OnHostPlayBtn()
        {
            Global.Get<ProcedureModule>().StartHostPlay();
        }

        private void OnMultiPlayBtn()
        {
            Global.Get<ProcedureModule>().StartMultiPlay();
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
            _playerData.Rename(playerName);
        }

        #endregion
    }
}
