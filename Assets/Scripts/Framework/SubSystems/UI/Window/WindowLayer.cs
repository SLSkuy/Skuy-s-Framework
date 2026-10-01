using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 窗口（Window）的Layer
    /// 通过访问管理对象的controller进行窗口的控制
    /// 窗口是一种有历史记录和顺序的UI界面
    /// 历史和队列只记窗口名，需要显示时再取回控制器
    /// </summary>
    public class WindowLayer : UIBaseLayer<IWindowController>
    {
        #region 内部成员

        /// <summary>
        /// 辅助界面用于显示弹窗等窗口
        /// 带蒙黑
        /// </summary>
        [SerializeField] private WindowPriorityLayer priorityLayerWindow;

        public IWindowController CurrentWindow { get; private set; }

        /// <summary>
        /// 按窗口名取回控制器。未在册时由外部实例化。
        /// </summary>
        public event Func<string, IWindowController> ResolveWindow;

        /// <summary>
        /// 窗口已经显示，取消空闲计时。
        /// </summary>
        public event Action<string> WindowShown;

        /// <summary>
        /// 窗口已经隐藏，开始空闲计时。
        /// </summary>
        public event Action<string> WindowHidden;

        private List<string> _readyToShow;
        private Queue<string> _windowQueue;
        private Stack<string> _windowHistory;

        public event Action RequestedScreenBlock;
        public event Action RequestedScreenUnBlock;
        
        #endregion
        
        #region 窗口控制器管理方法
        
        public override void ShowUI(IWindowController controller)
        {
            ShowUI<IWindowProperties>(controller, null);    
        }
        
        public override void ShowUI<TProps>(IWindowController controller, TProps props)
        {
            IWindowProperties windowProperties = props as IWindowProperties;
            if (ShouldEnqueue(controller))
            {
                Enqueue(controller);
                return;
            }

            DoShow(controller, windowProperties);
        }

        public override void HideUI(IWindowController controller)
        {
            if (controller == CurrentWindow)
            {
                string id = controller.UIControllerID;
                CurrentWindow = null;
                _windowHistory.Pop();
                _readyToShow.Remove(id);
                BlockScreen(controller);
                controller.Hide();
                WindowHidden?.Invoke(id);

                if (_windowQueue.Count > 0)
                    ShowNextInQueue();
                else if (_windowHistory.Count > 0)
                    ShowPreviousInHistory();
            }
            else
            {
                Debug.LogError($"[WindowLayer] Hide requested on WindowID {controller.UIControllerID}, but it is not the current Window");
            }
        }

        public override void HideAllUI(bool isAnimate = true)
        {
            base.HideAllUI(isAnimate);
            CurrentWindow = null;
            priorityLayerWindow.RefreshDarken();
            _windowHistory.Clear();
        }

        #endregion
        
        #region 窗口层管理方法

        public override void Initialize()
        {
            base.Initialize();
            _windowQueue = new Queue<string>();
            _windowHistory = new Stack<string>();
            _readyToShow = new List<string>();
        }
        
        public override void ReParentUI(IUIController controller, Transform uiTransform)
        {
            // 判断是否为弹窗，若是则添加到辅助层进行管理
            if (controller is IWindowController { IsPopup: true })
            {
                priorityLayerWindow.AddUI(uiTransform);
            }
            else if (controller is IWindowController)
            {
                // 普通窗口
                base.ReParentUI(controller, uiTransform);
            }
            else
            {
                Debug.LogError($"[WindowLayer] ReParent failed, controller is null");
            }
        }

        protected override void ProcessUIRegister(string uiControllerID, IWindowController controller)
        {
            base.ProcessUIRegister(uiControllerID, controller);
            controller.InTransitionFinished += OnInAnimationFinished;
            controller.OutTransitionFinished += OnOutAnimationFinished;
            controller.CloseRequested += OnCloseRequested;
        }

        protected override void ProcessUIUnregister(string uiControllerID, IWindowController controller)
        {
            base.ProcessUIUnregister(uiControllerID, controller);
            controller.InTransitionFinished -= OnInAnimationFinished;
            controller.OutTransitionFinished -= OnOutAnimationFinished;
            controller.CloseRequested -= OnCloseRequested;
        }

        private void OnCloseRequested(IUIController controller)
        {
            HideUI(controller as IWindowController);
        }
        
        /// <summary>
        /// 判断窗口优先级决定是否加入待显示队列
        /// </summary>
        /// <param name="controller">窗口控制器</param>
        /// <returns>是否应该加入待显示队列</returns>
        private bool ShouldEnqueue(IWindowController controller)
        {
            if (CurrentWindow == null && _windowQueue.Count == 0)
                return false;

            return controller.Priority == WindowPriority.Enqueue;
        }

        /// <summary>
        /// 队列只记窗口名。实例可以随后出册，轮到它时再取回。
        /// </summary>
        private void Enqueue(IWindowController controller)
        {
            string id = controller.UIControllerID;
            if (_readyToShow.Contains(id) || (CurrentWindow != null && CurrentWindow.UIControllerID == id))
            {
                Debug.LogWarning($"[WindowLayer] {id} is already in queue or showing");
                return;
            }

            _readyToShow.Add(id);
            _windowQueue.Enqueue(id);
            controller.Hide();
            WindowHidden?.Invoke(id);
        }

        private void ShowNextInQueue()
        {
            if (_windowQueue.Count == 0)
                return;

            string id = _windowQueue.Dequeue();
            _readyToShow.Remove(id);
            if (ResolveWindow != null)
            {
                IWindowController controller = ResolveWindow(id);
                if (controller == null)
                    return;

                DoShow(controller, null);
            }
        }

        private void ShowPreviousInHistory()
        {
            if (_windowHistory.Count == 0)
                return;

            string id = _windowHistory.Pop();
            if (ResolveWindow != null)
            {
                IWindowController controller = ResolveWindow(id);
                if (controller == null)
                    return;

                DoShow(controller, null);
            }
        }

        /// <summary>
        /// 处理窗口显示逻辑。历史只记下窗口名。
        /// </summary>
        /// <param name="controller">窗口控制器</param>
        /// <param name="properties">本次显示使用的窗口属性，不写入历史或队列</param>
        private void DoShow(IWindowController controller, IWindowProperties properties)
        {
            if (controller == CurrentWindow)
            {
                Debug.LogWarning($"[WindowLayer] {controller.UIControllerID} is already show");
                return;
            }

            // 弹窗强制留在最前，不把底下的窗口送去隐藏
            if (CurrentWindow != null && CurrentWindow.HideOnForegroundLost
                && !CurrentWindow.IsPopup && !controller.IsPopup)
            {
                string coveredId = CurrentWindow.UIControllerID;
                CurrentWindow.Hide();
                WindowHidden?.Invoke(coveredId);
            }
            
            _windowHistory.Push(controller.UIControllerID);
            BlockScreen(controller);

            // 启用蒙黑层
            if (controller.IsPopup)
                priorityLayerWindow.DarkenBg();
            
            controller.Show(properties);
            CurrentWindow = controller;
            WindowShown?.Invoke(controller.UIControllerID);
        }
        
        /// <summary>
        /// 窗口动画过渡时禁止额外点击操作
        /// </summary>
        private void BlockScreen(IUIController controller)
        {
            RequestedScreenBlock?.Invoke();
        }

        /// <summary>
        /// 窗口动画过渡完毕时恢复点击操作
        /// </summary>
        private void UnBlockScreen(IUIController controller)
        {
            RequestedScreenUnBlock?.Invoke();
        }
        
        /// <summary>
        /// 进入窗口动画播放完毕回调
        /// </summary>
        private void OnInAnimationFinished(IUIController controller)
        {
            UnBlockScreen(controller);
        }

        /// <summary>
        /// 隐藏窗口动画播放完毕回调
        /// </summary>
        private void OnOutAnimationFinished(IUIController controller)
        {
            UnBlockScreen(controller);
            if (controller is IWindowController { IsPopup: true })
                priorityLayerWindow.RefreshDarken();
        }
        
        #endregion

        #region 生命周期

        public void OnDestroy()
        {
            ResolveWindow = null;
            WindowShown = null;
            WindowHidden = null;
            RequestedScreenBlock = null;
            RequestedScreenUnBlock = null;
        }

        #endregion
    }
}
