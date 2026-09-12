using UnityEngine;
using Framework;

namespace Core
{
    /// <summary>
    /// 游戏核心逻辑管理器，不负责游戏流程的处理，只负责工具的管理
    /// </summary>
    public class GameCore : MonoSingleton<GameCore>
    {
        #region 子系统
        public SystemManager SystemMgr { get; private set; }
        public TimerManager TimerMgr { get; private set; }
        public DataProxyManager DataProxyMgr { get; private set; }
        public ResourceManager ResourceMgr { get; private set; }
        public SpawnManager SpawnMgr { get; private set; }
        public PoolManager PoolMgr { get; private set; }
        public LocalInputManager LocalInputMgr { get; private set; }
        public SceneLoader SceneMgr { get; private set; }
        public UIManager UIMgr { get; private set; }
        public CameraManager CameraMgr { get; private set; }
        #endregion

        private GameCoreConfig _config;

        private void InitializeGameCore()
        {
            _config = GameCoreConfig.Instance;
            Application.targetFrameRate = _config.targetFrame;

            SystemMgr = new SystemManager();
            SystemMgr._Init();
            
            // 资源核心模块加载完毕之后再加载其他系统
            ResourceMgr = SystemMgr.RegisterSystem<ResourceManager>();
            ResourceMgr.SetProvider(new YooAssetProvider(_config.defaultPackageName, _config.resourceMode));
            ResourceMgr.OnResourceReady += InitSystems;
        }

        #region Global

        private void InitSystems()
        {
            InitSubSystems();
            InitDataProxy();
            InitUI();
        }

        /// <summary>
        /// 初始化所有子系统
        /// </summary>
        private void InitSubSystems()
        {
            SpawnMgr = SystemMgr.RegisterSystem<SpawnManager>();
            PoolMgr = SystemMgr.RegisterSystem<PoolManager>();
            TimerMgr = SystemMgr.RegisterSystem<TimerManager>();
            DataProxyMgr = SystemMgr.RegisterSystem<DataProxyManager>();
            SceneMgr = SystemMgr.RegisterSystem<SceneLoader>();
            LocalInputMgr = SystemMgr.RegisterSystem<LocalInputManager>();
            UIMgr = SystemMgr.RegisterSystem<UIManager>();
            CameraMgr = SystemMgr.RegisterSystem<CameraManager>();
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

        #endregion

        #region 生命周期

        protected override void Init()
        {
            InitializeGameCore();
        }

        private void Update()
        {
            SystemMgr.Update(Time.deltaTime);
        }

        private void LateUpdate()
        {
            SystemMgr.LateUpdate();
        }

        private void FixedUpdate()
        {
            SystemMgr.FixedUpdate(Time.fixedDeltaTime);
        }

        protected override void Destroy()
        {
            ResourceMgr.OnResourceReady -= InitSystems;
            SystemMgr.Destroy();
            Global.Clear();
        }

        /// <summary>
        /// 程序退出清理
        /// </summary>
        private void OnApplicationQuit()
        {
            ShutDown();
        }

        #endregion
    }
}
