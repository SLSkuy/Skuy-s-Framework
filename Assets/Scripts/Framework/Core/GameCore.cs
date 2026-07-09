using UnityEngine;
using Framework;
using Network;
using UIFramework;

namespace Core
{
    public class GameCore : MonoSingleton<GameCore>
    {
        #region 组件
        public SystemManager SystemMgr { get; private set; }
        public TimerManager TimerMgr { get; private set; }
        public NetManager NetMgr { get; private set; }
        public DataProxyManager DataProxyMgr { get; private set; }
        public ResourceManager ResourceMgr { get; private set; }
        public PoolManager PoolMgr { get; private set; }
        public GameStateManager GameStateMgr { get; private set; }
        public SceneLoader SceneMgr { get; private set; }
        public UIManager UIMgr { get; private set; }
        #endregion

        #region 属性
        public GameState CurrentState => GameStateMgr.CurrentState;
        public bool IsPaused => GameStateMgr.IsPaused();
        #endregion
        
        private void InitializeGameCore()
        {
            Application.targetFrameRate = 60;
            
            InitSubSystems();
            InitDataProxy();
            InitUI();
        }

        #region 全局方法

        /// <summary>
        /// 初始化所有子系统
        /// </summary>
        private void InitSubSystems()
        {
            // 初始化子系统管理模块
            SystemMgr = new SystemManager();
            SystemMgr._Init();
            
            // 框架模块
            ResourceMgr = SystemMgr.RegisterSystem<ResourceManager>();
            PoolMgr = SystemMgr.RegisterSystem<PoolManager>();
            TimerMgr = SystemMgr.RegisterSystem<TimerManager>();
            NetMgr = SystemMgr.RegisterSystem<NetManager>();
            DataProxyMgr = SystemMgr.RegisterSystem<DataProxyManager>();
            SceneMgr = SystemMgr.RegisterSystem<SceneLoader>();
            UIMgr = SystemMgr.RegisterSystem<UIManager>();
            
            // 游戏状态管理模块
            GameStateMgr = SystemMgr.RegisterSystem<GameStateManager>();
        }

        /// <summary>
        /// 初始化全局游戏数据
        /// </summary>
        private void InitDataProxy()
        {
            // TODO: 初始化全局数据代理
        }

        private void InitUI()
        {
            // TODO: 初始化全局UI
        }

        /// <summary>
        /// 退出游戏
        /// </summary>
        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        
        #endregion

        #region 生命周期

        protected override void Init()
        {
            InitializeGameCore();
        }

        private void Update()
        {
            if (IsPaused) return;
            
            SystemMgr.Update(Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (IsPaused) return;
            
            SystemMgr.LateUpdate();
        }

        private void FixedUpdate()
        {
            if (IsPaused) return;
            
            SystemMgr.FixedUpdate(Time.fixedDeltaTime);
        }

        protected override void Destroy()
        {
            SystemMgr.Destroy();
            Global.Clear();
        }

        /// <summary>
        /// 程序退出清理
        /// </summary>
        private void OnApplicationQuit()
        {
            // 先清理所有的子模块
            ShutDown();
        }

        #endregion
    }
}
