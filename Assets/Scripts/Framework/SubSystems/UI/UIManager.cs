using System;
using System.Collections.Generic;
using System.Text;
using Framework.Core;
using Framework.Panel;
using Framework.Window;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Framework
{
    /// <summary>
    /// UI框架，声明所有的对外接口
    /// 充当UIManager的作用
    /// </summary>
    public class UIManager : SubSystemBase
    {
        public override int Priority => (int)SubSystemPriority.UIManager;
        
        #region 内部成员
        
        // string -> UIConfig
        private readonly Dictionary<string, UIConfig> _uiConfigs = new();
        private readonly Dictionary<string, GameObject> _uiObj = new();
        
        // UI类别层级管理器
        private Transform _container;
        private PanelLayer _panelLayer;
        private WindowLayer _windowLayer;
        
        private GraphicRaycaster _graphicRaycaster;
        
        #endregion
        
        #region 框架内部管理方法

        public override void Init()
        {
            // 获取UI容器
            _container = Global.Instantiate("UI/UIManager").transform;
            _container.name = "[UIRoot]";
            Object.DontDestroyOnLoad(_container);
            
            // 初始化Panel层级管理器
            if (!_panelLayer)
            {
                _panelLayer = _container.GetComponentInChildren<PanelLayer>();
                if (_panelLayer)
                {
                    _panelLayer.Initialize();
                }
                else
                {
                    Debug.LogError("[UIFramework] UI Frame lacks Panel Layer]");
                }
            }
            
            // 初始化Window层级管理器
            if (!_windowLayer)
            {
                _windowLayer = _container.GetComponentInChildren<WindowLayer>();
                if (_windowLayer)
                {
                    _windowLayer.Initialize();
                    _windowLayer.RequestedScreenBlock += BlockScreen;
                    _windowLayer.RequestedScreenUnBlock += UnblockScreen;
                }
                else
                {
                    Debug.LogError("[UIFramework] UI Frame lacks Window Layer]");
                }
            }
            
            _graphicRaycaster = _container.GetComponent<GraphicRaycaster>();
        }

        /// <summary>
        /// 实例化并注册UI配置中的所有UI界面
        /// </summary>
        public void RegisterUIConfig(UIConfig config)
        {
            if (_uiConfigs.TryAdd(config.uiSceneName, config))
            {
                foreach (var entry in config.uiToRegister)
                {
                    var res = GetUIRes(config.uiSceneName, entry.uiName);
                    if (res == null)
                    {
                        Debug.LogError($"[UIFramework] {GetUIPath(config.uiSceneName, entry.uiName)} 路径下不存在UI预制体 : {entry.uiName}");
                        return;
                    }
                    
                    GameObject obj = Object.Instantiate(res);
                    IUIController controller = obj.GetComponent<IUIController>();
                    RegisterUI(controller.UIControllerID, controller, obj.transform);
                    if(!entry.isEnableOnRegister)controller.Hide();
                    _uiObj[entry.uiName] = obj;
                }
            }
        }

        /// <summary>
        /// 销毁UI配置中的所有UI界面
        /// </summary>
        public void UnregisterUIConfig(string uiSceneName)
        {
            if (_uiConfigs.TryGetValue(uiSceneName, out var config))
            {
                foreach (var entry in config.uiToRegister)
                {
                    UnregisterUI(entry.uiName);
                    Object.Destroy(_uiObj[entry.uiName]);
                    _uiObj.Remove(entry.uiName);
                }

                _uiConfigs.Remove(uiSceneName);
            }
        }
        
        #endregion
        
        #region 框架对外暴露方法

        private void BlockScreen()
        {
            _graphicRaycaster.enabled = false;
        }

        private void UnblockScreen()
        {
            _graphicRaycaster.enabled = true;
        }

        public void ShowPanel(string id)
        {
            _panelLayer.ShowUIByID(id);
        }

        public void ShowPanel<T>(string id, T p) where T : IUIProperties
        {
            _panelLayer.ShowUIByID(id, p);
        }

        public void HidePanel(string id)
        {
            _panelLayer.HideUIByID(id);
        }

        public void OpenWindow(string id)
        {
            _windowLayer.ShowUIByID(id);
        }

        public void OpenWindow<T>(string id, T p) where T : IUIProperties
        {
            _windowLayer.ShowUIByID(id, p);
        }

        public void CloseWindow(string id)
        {
            _windowLayer.HideUIByID(id);
        }

        public void CloseCurrentWindow()
        {
            if(_windowLayer.CurrentWindow != null)CloseWindow(_windowLayer.CurrentWindow.UIControllerID);
        }

        /// <summary>
        /// 根据传入的ID显示对应的UI界面，不分面板还是窗口
        /// </summary>
        /// <param name="id">UI界面ID</param>
        public void ShowUI(string id)
        {
            if (IsUIRegistered(id, out var type)) {
                if (type == typeof(IWindowController)) {
                    OpenWindow(id);
                }
                else if (type == typeof(IPanelController)) {
                    ShowPanel(id);
                }
            }
            else {
                Debug.LogError($"[UIFramework] Tried to open Screen id {id} but it's not registered as Window or Panel!");
            }
        }

        /// <summary>
        /// 根据传入的ID显示对应的UI界面，不分面板还是窗口，同时设置其属性
        /// </summary>
        /// <param name="id">UI界面ID</param>
        /// <param name="p">UI界面属性参数</param>
        /// <typeparam name="T">UI界面属性类型</typeparam>
        public void ShowUI<T>(string id, T p) where T : IUIProperties
        {
            if (IsUIRegistered(id, out var type)) {
                if (type == typeof(IWindowController)) {
                    OpenWindow(id, p);
                }
                else if (type == typeof(IPanelController)) {
                    ShowPanel(id, p);
                }
            }
            else {
                Debug.LogError($"[UIFramework] Tried to open Screen id {id} but it's not registered as Window or Panel!");
            }
        }

        /// <summary>
        /// 根据传入的ID关闭对应的UI界面，不分面板还是窗口
        /// </summary>
        /// <param name="id"></param>
        public void HideUI(string id)
        {
            if (IsUIRegistered(id, out var type)) {
                if (type == typeof(IWindowController)) {
                    CloseWindow(id);
                }
                else if (type == typeof(IPanelController)) {
                    HidePanel(id);
                }
            }
            else {
                Debug.LogError($"[UIFramework] Tried to open Screen id {id} but it's not registered as Window or Panel!");
            }
        }

        public void RegisterUI(string id, IUIController uiController, Transform uiTransform)
        {
            switch (uiController)
            {
                case IWindowController window when uiTransform:
                    _windowLayer.RegisterUIController(id, window);
                    _windowLayer.ReParentUI(window, uiTransform);
                    break;
                case IPanelController panel when uiTransform:
                    _panelLayer.RegisterUIController(id, panel);
                    _panelLayer.ReParentUI(panel, uiTransform);
                    break;
                default:
                    Debug.LogError("[UIFramework] Transform is null or Unknown uiController");
                    break;
            }
        }

        public void UnregisterUI(string id, IUIController uiController)
        {
            switch (uiController)
            {
                case IWindowController window:
                    _windowLayer.UnregisterUIController(id, window);
                    break;
                case IPanelController panel:
                    _panelLayer.UnregisterUIController(id, panel);
                    break;
                default:
                    Debug.LogError($"[UIFramework] {id} is not registered");
                    break;
            }
        }

        public void UnregisterUI(string id)
        {
            IUIController uiController = GetUIController(id);
            UnregisterUI(id, uiController);
        }

        public void HideAllUI(bool animate = true)
        {
            _panelLayer.HideAllUI(animate);
            _windowLayer.HideAllUI(animate);
        }

        public bool IsUIRegistered(string id, out Type type)
        {
            if (_windowLayer.IsRegistered(id))
            {
                type = typeof(IWindowController);
                return true;
            }
            if (_panelLayer.IsRegistered(id))
            {
                type = typeof(IPanelController);
                return true;
            }

            type = null;
            return false;
        }

        private IUIController GetUIController(string id)
        {
            if (_windowLayer.IsRegistered(id))
            {
                return _windowLayer.GetUIController(id);
            }
            else if (_panelLayer.IsRegistered(id))
            {
                return _panelLayer.GetUIController(id);
            }
            return null;
        }

        private readonly StringBuilder _sb = new();
        private string GetUIPath(string uiScene, string uiControllerID)
        {
            _sb.Clear();
            _sb.Append("UI/Prefabs/");
            _sb.Append(uiScene);
            _sb.Append("/");
            _sb.Append(uiControllerID);
            return _sb.ToString();
        }

        private GameObject GetUIRes(string uiScene,string uiControllerID)
        {
            var prefab = Global.GetAsset<GameObject>(GetUIPath(uiScene, uiControllerID));
            return prefab;
        }
        
        #endregion
    }
}
