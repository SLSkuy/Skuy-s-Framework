using System;

namespace Framework
{
    /// <summary>
    /// 场景层。放置场景界面。批量关闭界面时不处理这一层。
    /// </summary>
    public class SceneLayer : UIBaseLayer<ISceneController>
    {
        #region 事件

        /// <summary>
        /// 场景界面已经隐藏
        /// </summary>
        public event Action<string> SceneHidden;

        #endregion

        #region 面板控制器管理方法
        
        public override void ShowUI(ISceneController controller) => controller.Show();
        public override void ShowUI<TProps>(ISceneController controller, TProps props) => controller.Show(props);
        public override void HideUI(ISceneController controller) => controller.Hide();
        
        #endregion

        #region 面板层管理方法

        protected override void ProcessUIRegister(string uiControllerID, ISceneController controller)
        {
            base.ProcessUIRegister(uiControllerID, controller);
            controller.OutTransitionFinished += OnOutFinished;
        }

        protected override void ProcessUIUnregister(string uiControllerID, ISceneController controller)
        {
            base.ProcessUIUnregister(uiControllerID, controller);
            controller.OutTransitionFinished -= OnOutFinished;
        }

        private void OnOutFinished(IUIController controller)
        {
            SceneHidden?.Invoke(controller.UIControllerID);
        }

        #endregion
    }
}
