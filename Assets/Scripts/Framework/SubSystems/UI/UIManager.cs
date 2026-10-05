using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YooAsset;
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

        private const string PANEL_LOCATION_PREFIX = "UI_";
        private const float PANEL_VISIBLE_ELAPSED = -1f;

        /// <summary>
        /// UIManager 缓存的UI，手动 Register 的界面不在此表中，不参与超时回收。
        /// </summary>
        private sealed class CachedUI
        {
            public GameObject Instance;
            public IUIController Controller;
            
            /// <summary>
            /// 隐藏计时器，超过阈值返回到对象池中
            /// </summary>
            public float HiddenElapsed;
        }

        private readonly Dictionary<string, CachedUI> _ownedUIs = new();
        private readonly List<string> _unloadScratch = new();
        
        // UI类别层级管理器
        private Transform _container;
        private PanelLayer _panelLayer;
        private WindowLayer _windowLayer;
        private SceneLayer _sceneLayer;
        
        private GraphicRaycaster _graphicRaycaster;
        
        #endregion
        
        #region 框架内部管理方法

        public override void Init()
        {
            // 获取UI容器
            _container = Global.InstantiateUnpooled("UI_UIManager", new InstantiateOptions(true)).transform;
            _container.name = "[UIManager]";
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
                    _windowLayer.ResolveWindow += ResolveWindow;
                    _windowLayer.WindowShown += MarkScreenVisible;
                    _windowLayer.WindowHidden += BeginScreenIdle;
                    _windowLayer.RequestedScreenBlock += BlockScreen;
                    _windowLayer.RequestedScreenUnBlock += UnblockScreen;
                }
                else
                {
                    Debug.LogError("[UIFramework] UI Frame lacks Window Layer]");
                }
            }

            if (!_sceneLayer)
            {
                _sceneLayer = _container.GetComponentInChildren<SceneLayer>();
                if (_sceneLayer)
                {
                    _sceneLayer.Initialize();
                    _sceneLayer.SceneHidden += BeginScreenIdle;
                }
                else
                {
                    Debug.LogError("[UIFramework] UI Frame lacks Scene Layer]");
                }
            }

            _graphicRaycaster = _container.GetComponent<GraphicRaycaster>();
        }

        public override void Update(float deltaTime)
        {
            float timeout = ResGroupPolicy.Get(ResGroup.UI).idleUnloadSeconds;
            _unloadScratch.Clear();

            foreach (KeyValuePair<string, CachedUI> pair in _ownedUIs)
            {
                CachedUI cached = pair.Value;
                if (cached.HiddenElapsed < 0f)
                    continue;

                cached.HiddenElapsed += deltaTime;
                if (cached.HiddenElapsed >= timeout)
                    _unloadScratch.Add(pair.Key);
            }

            for (int i = 0; i < _unloadScratch.Count; i++)
                ReleaseCachedUI(_unloadScratch[i]);

            _unloadScratch.Clear();
        }

        public override void Destroy()
        {
            _unloadScratch.Clear();
            foreach (string id in _ownedUIs.Keys)
                _unloadScratch.Add(id);

            for (int i = 0; i < _unloadScratch.Count; i++)
                ReleaseCachedUI(_unloadScratch[i]);

            _unloadScratch.Clear();

            // 防止Unity对GameObject销毁顺序不同
            if (_container)
                Global.Release(_container.gameObject);
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

        // ========== Panel ==========
        public void ShowPanel(string id)
        {
            IUIController screen = LoadUI(id);
            if (screen is not IPanelController)
            {
                if (screen != null)
                    Debug.LogError($"[UIFramework] {id} is not a panel");
                return;
            }

            MarkScreenVisible(id);
            _panelLayer.ShowUIByID(id);
        }

        public void ShowPanel<T>(string id, T p) where T : IUIProperties
        {
            IUIController screen = LoadUI(id);
            if (screen is not IPanelController)
            {
                if (screen != null)
                    Debug.LogError($"[UIFramework] {id} is not a panel");
                return;
            }

            MarkScreenVisible(id);
            _panelLayer.ShowUIByID(id, p);
        }

        public void HidePanel(string id)
        {
            _panelLayer.HideUIByID(id);
            BeginScreenIdle(id);
        }
        // ========== Panel ==========

        // ========== Window ==========
        public void ShowWindow(string id)
        {
            IUIController screen = LoadUI(id);
            if (screen is IWindowController window)
            {
                _windowLayer.ShowUI(window);
                return;
            }

            if (screen != null)
                Debug.LogError($"[UIFramework] {id} is not a window");
        }

        public void ShowWindow<T>(string id, T p) where T : IUIProperties
        {
            IUIController screen = LoadUI(id);
            if (screen is IWindowController window)
            {
                _windowLayer.ShowUI(window, p);
                return;
            }

            if (screen != null)
                Debug.LogError($"[UIFramework] {id} is not a window");
        }

        public void HideWindow(string id)
        {
            _windowLayer.HideUIByID(id);
        }

        public void CloseCurrentWindow()
        {
            if(_windowLayer.CurrentWindow != null) HideWindow(_windowLayer.CurrentWindow.UIControllerID);
        }
        // ========== Window ==========
        
        // ========== Scene ==========
        public void ShowScene(string id)
        {
            IUIController screen = LoadUI(id);
            if (screen is not ISceneController)
            {
                if (screen != null)
                    Debug.LogError($"[UIFramework] {id} is not a scene screen");
                return;
            }

            MarkScreenVisible(id);
            _sceneLayer.ShowUIByID(id);
        }

        public void HideScene(string id)
        {
            _sceneLayer.HideUIByID(id);
        }
        // ========== Scene ==========

        public void ShowUI(string id)
        {
            switch (LoadUI(id))
            {
                case ISceneController:
                    MarkScreenVisible(id);
                    _sceneLayer.ShowUIByID(id);
                    break;
                case IWindowController:
                    _windowLayer.ShowUIByID(id);
                    break;
                case IPanelController:
                    MarkScreenVisible(id);
                    _panelLayer.ShowUIByID(id);
                    break;
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
            switch (LoadUI(id))
            {
                case ISceneController:
                    MarkScreenVisible(id);
                    _sceneLayer.ShowUIByID(id, p);
                    break;
                case IWindowController:
                    _windowLayer.ShowUIByID(id, p);
                    break;
                case IPanelController:
                    MarkScreenVisible(id);
                    _panelLayer.ShowUIByID(id, p);
                    break;
            }
        }

        /// <summary>
        /// 根据传入的ID关闭对应的UI界面，不分面板还是窗口
        /// </summary>
        /// <param name="id"></param>
        public void HideUI(string id)
        {
            if (IsUIRegistered(id, out var type))
            {
                if (type == typeof(IWindowController))
                    HideWindow(id);
                else if (type == typeof(IPanelController))
                    HidePanel(id);
                else if (type == typeof(ISceneController))
                    HideScene(id);
            }
            else
            {
                Debug.LogError($"[UIFramework] Tried to hide Screen id {id} but it's not registered as Window or Panel!");
            }
        }

        /// <summary>
        /// 注册UI面板
        /// </summary>
        public void RegisterUI(IUIController uiController, Transform uiTransform)
        {
            RegisterUI(uiController.UIControllerID, uiController, uiTransform);
        }

        /// <summary>
        /// 注册UI面板
        /// </summary>
        public void RegisterUI(string id, IUIController uiController, Transform uiTransform)
        {
            switch (uiController)
            {
                case ISceneController scene when uiTransform:
                    _sceneLayer.RegisterUIController(id, scene);
                    _sceneLayer.ReParentUI(scene, uiTransform);
                    break;
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
        
        public void UnregisterUI(string id)
        {
            IUIController uiController = GetUIController(id);
            UnregisterUI(id, uiController);
        }

        public void UnregisterUI(string id, IUIController uiController)
        {
            switch (uiController)
            {
                case ISceneController scene:
                    _sceneLayer.UnregisterUIController(id, scene);
                    break;
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

        public void HideAllUI(bool animate = true)
        {
            _panelLayer.HideAllUI(animate);
            _windowLayer.HideAllUI(animate);

            foreach (string id in _ownedUIs.Keys)
            {
                if (_sceneLayer.IsRegistered(id)) continue;
                BeginScreenIdle(id);
            }
        }

        public bool IsUIRegistered(string id, out Type type)
        {
            if (_sceneLayer.IsRegistered(id))
            {
                type = typeof(ISceneController);
                return true;
            }
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
        
        #endregion
        
        private IUIController GetUIController(string id)
        {
            if (_sceneLayer.IsRegistered(id))
            {
                return _sceneLayer.GetUIController(id);
            } 
            if (_windowLayer.IsRegistered(id))
            {
                return _windowLayer.GetUIController(id);
            }
            if (_panelLayer.IsRegistered(id))
            {
                return _panelLayer.GetUIController(id);
            }
            
            return null;
        }
        
        /// <summary>
        /// 已在册则直接返回。未在册时按界面名补上 <c>UI_</c> 前缀，从 <see cref="ResGroup.UI"/> 实例化并入册。
        /// </summary>
        private IUIController LoadUI(string id)
        {
            if (_sceneLayer.IsRegistered(id)) return _sceneLayer.GetUIController(id);
            if (_windowLayer.IsRegistered(id)) return _windowLayer.GetUIController(id);
            if (_panelLayer.IsRegistered(id)) return _panelLayer.GetUIController(id);

            // 不存在对应的UI实例，直接生成
            GameObject instance = Global.Instantiate(PANEL_LOCATION_PREFIX + id, ResGroup.UI, new InstantiateOptions(false));
            if (!instance) return null;

            // 实例此时尚未激活，界面脚本挂上之后才会触发其Awake
            UIView view = instance.GetComponent<UIView>();
            if (view) view.ApplyBindings(id);

            ISceneController scene = instance.GetComponent<ISceneController>();
            IWindowController window = instance.GetComponent<IWindowController>();
            IPanelController panel = instance.GetComponent<IPanelController>();
            IUIController controller = scene != null ? scene : window != null ? window : panel;
            if (controller == null)
            {
                Debug.LogError($"[UIFramework] {id} is not a screen");
                Global.Release(instance);
                return null;
            }

            RegisterUI(id, controller, instance.transform);
            _ownedUIs.Add(id, new CachedUI
            {
                Instance = instance,
                Controller = controller,
                HiddenElapsed = 0f
            });
            return controller;
        }

        /// <summary>
        /// 窗口队列缓存，解析缓存window对应controller
        /// </summary>
        private IWindowController ResolveWindow(string id)
        {
            return LoadUI(id) as IWindowController;
        }

        private void MarkScreenVisible(string id)
        {
            if (_ownedUIs.TryGetValue(id, out CachedUI owned))
                owned.HiddenElapsed = PANEL_VISIBLE_ELAPSED;
        }

        private void BeginScreenIdle(string id)
        {
            if (_ownedUIs.TryGetValue(id, out CachedUI owned) && owned.HiddenElapsed < 0f)
                owned.HiddenElapsed = 0f;
        }

        private void ReleaseCachedUI(string id)
        {
            if (!_ownedUIs.Remove(id, out CachedUI owned)) return;

            UnregisterUI(id, owned.Controller);
            Global.Release(owned.Instance);
        }
    }
}
