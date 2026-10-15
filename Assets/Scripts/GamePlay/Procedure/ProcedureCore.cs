using Core;
using Framework;
using GamePlay.DataProxy;
using GamePlay.Room;
using UnityEngine;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 玩法流程核心：界面意图入口，并持有一次对局的寿命（名册、关卡、NetClient / NetServer）。
    /// 状态只持有自己那一段的东西，对局在离开菜单时打开、回到菜单的那条边上拆除。
    /// </summary>
    public sealed class ProcedureCore : MonoSingleton<ProcedureCore>
    {
        private StateMachine<ProcedureState> _fsm;
        private RoomManager _room;
        private bool _isSessionOpen;

        #region 属性
        /// <summary>
        /// 当前这一次带任务的切换。同时只有一个。
        /// </summary>
        public ProcedureTransition PendingTransition { get; private set; }
        #endregion

        #region 状态切换

        /// <summary>
        /// 请求切换新的状态。没有任务时立刻切换；有任务时先进入加载状态，任务完成后再进入目标状态。
        /// 加载中再次请求会停掉当前任务，换成这一次。
        /// </summary>
        public void RequestState(ProcedureState state, ILoadTask task = null)
        {
            if (task == null)
            {
                _fsm.ChangeState(state);
                return;
            }

            PendingTransition = new ProcedureTransition(state, task);
            _fsm.ChangeState(ProcedureState.Loading);
        }

        /// <summary>
        /// 经由加载闸门换场景。当前状态立刻退出，场景就绪之后才进入目标状态。
        /// </summary>
        public void RequestScene(ProcedureState nextState, string sceneName)
        {
            RequestState(nextState, new SceneLoadTask(sceneName));
        }

        #endregion
        
        #region 游戏会话管理

        /// <summary>
        /// 创建联机房间
        /// </summary>
        public void StartHostPlay(RoomConfig config = null)
        {
            _room.StartServer(config);
            RequestScene(ProcedureState.Match, GlobalConstants.LEVEL_SCENE_NAME);
            
            _isSessionOpen = true;
        }

        /// <summary>
        /// 加入远程房间
        /// </summary>
        public void StartMultiPlay()
        {
            _room.StartConnecting();
            _room.SessionConnected += OnSessionConnected;
            _room.SessionDisconnected += OnSessionEnded;
            _room.SessionReconnectFailed += OnReconnectFailed;
            
            _isSessionOpen = true;
        }

        /// <summary>
        /// 返回到主菜单
        /// </summary>
        public void BackToMainMenu()
        {
            RequestScene(ProcedureState.Menu, GlobalConstants.MENU_SCENE_NAME);
        }
        
        private void CloseSession()
        {
            if (!_isSessionOpen) return;
            
            _room.SessionConnected -= OnSessionConnected;
            _room.SessionDisconnected -= OnSessionEnded;
            _room.SessionReconnectFailed -= OnReconnectFailed;
            _room.Dissolve();
            
            _isSessionOpen = false;
        }

        #endregion
        
        #region 生命周期

        private void StartUp()
        {
            InitDataProxy();
            
            _room = Global.Register<RoomManager>();
            
            _fsm.ChangeState(ProcedureState.Menu);
        }
        
        /// <summary>
        /// 初始化需要的游戏业务数据
        /// </summary>
        private void InitDataProxy()
        {
            Global.RegisterDataProxy<EntityConfigProxy>();
            Global.RegisterDataProxy<GameSaveDataProxy>();
            Global.RegisterDataProxy<PlayerDataProxy>();
            Global.RegisterDataProxy<RoomDataProxy>();
        }

        protected override void Init()
        {
            _fsm = new StateMachine<ProcedureState>();
            _fsm.OnStateChange += OnProcedureStateChange;
            _fsm.RegisterState(new ProcedureMenuState(_fsm, this));
            _fsm.RegisterState(new ProcedureMatchState(_fsm, this));
            _fsm.RegisterState(new ProcedureLoadingState(_fsm, this));
            
            // 子系统就绪之后才启动流程，已经就绪时立刻回调
            AppCore.OnAppReady += StartUp;
            AppCore.OnAppQuit += ShutDown;
        }

        private void Update()
        {
            _fsm.Update(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            _fsm.FixedUpdate(Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            _fsm.LateUpdate();
        }

        protected override void Destroy()
        {
            CloseSession();
            Global.Unregister<RoomManager>();
            
            _fsm.OnStateChange -= OnProcedureStateChange;
            _fsm.Stop();
            
            AppCore.OnAppReady -= StartUp;
            AppCore.OnAppQuit -= ShutDown;
        }

        #endregion

        #region 事件回调
        
        /// <summary>
        /// 加载任务失败，没有进入目标状态。目标不是菜单时再请求回菜单；
        /// 已经是菜单则直接进入菜单状态，避免停在加载态。
        /// </summary>
        public void FailTransition(ProcedureState nextState)
        {
            if (nextState == ProcedureState.Menu)
            {
                _fsm.ChangeState(ProcedureState.Menu);
                return;
            }

            BackToMainMenu();
        }

        /// <summary>
        /// 回到菜单意味着这一场对局结束，在旧状态退出之后、菜单进入之前拆掉。
        /// </summary>
        private void OnProcedureStateChange(ProcedureState from, ProcedureState to)
        {
            if (to == ProcedureState.Menu) CloseSession();
        }

        private void OnSessionEnded()
        {
            BackToMainMenu();
        }

        private void OnReconnectFailed()
        {
            BackToMainMenu();
        }

        #endregion

        #region 事件回调

        /// <summary>
        /// 加入被接受，接管已有连接并补上名册。
        /// </summary>
        public void OnSessionConnected()
        {
            RequestScene(ProcedureState.Match, GlobalConstants.LEVEL_SCENE_NAME);
        }

        #endregion
    }
}
