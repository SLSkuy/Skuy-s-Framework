using Core;
using Framework;
using GamePlay.DataProxy;
using GamePlay.Room;
using NetSync;
using Network;
using UnityEngine;
using UnityEngine.InputSystem;

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
        private NetClient _netClient;
        private NetServer _netServer;
        private bool _isSessionOpen;

        #region 属性
        /// <summary>
        /// 这一场对局是怎么开起来的。菜单投递意图时写入，准备状态读取。
        /// </summary>
        public ProcedureIntent MatchIntent { get; private set; }
        #endregion

        /// <summary>
        /// 由 <see cref="AppCore"/> 在子系统就绪之后调用，流程从主菜单开始。
        /// </summary>
        public void StartUp()
        {
            // 初始化需要的局内数据
            InitDataProxy();
            
            // 直接切换到主菜单状态
            _fsm.ChangeState(ProcedureState.Menu);
        }

        private void InitDataProxy()
        {
            Global.RegisterDataProxy<EntityConfigProxy>();
        }

        #region 状态切换

        /// <summary>
        /// 投递界面意图，交给当前流程状态决定响应与否。
        /// </summary>
        public void PostIntent(ProcedureIntent intent)
        {
            if (_fsm.Current is ProcedureStateBase state)
            {
                state.ProcessIntent(intent);
                return;
            }

            if (intent == ProcedureIntent.BackToMenu)
            {
                RequestMenu();
            }
        }

        public void StartLocalPlay() => PostIntent(ProcedureIntent.LocalPlay);
        public void HostMultiplay() => PostIntent(ProcedureIntent.HostMultiplay);
        public void JoinRemote() => PostIntent(ProcedureIntent.JoinRemote);
        public void BackToMainMenu() => PostIntent(ProcedureIntent.BackToMenu);

        /// <summary>
        /// 经由加载闸门换场景。当前状态立刻退出，场景就绪之后才进入目标状态。
        /// </summary>
        public void RequestScene(ProcedureState nextState, string sceneName)
        {
            _fsm.RequestState(new StateTransition<ProcedureState>(nextState, new SceneLoadTask(sceneName)));
        }

        /// <summary>
        /// 回菜单：换回菜单场景，对局在进入菜单的那条边上拆掉。
        /// </summary>
        public void RequestMenu()
        {
            RequestScene(ProcedureState.Menu, GlobalConstants.MENU_SCENE_NAME);
        }

        #endregion
        
        #region 游戏会话管理

        /// <summary>
        /// 按意图打开对局。加入远端在被接受之前只登记客户端，还没有名册。
        /// </summary>
        public void OpenSession(ProcedureIntent intent)
        {
            MatchIntent = intent;
            _isSessionOpen = true;

            if (intent == ProcedureIntent.JoinRemote)
            {
                _netClient = Global.Register<NetClient>();
                _netClient.OnReconnectFailed += OnReconnectFailed;
                return;
            }

            if (intent == ProcedureIntent.HostMultiplay)
            {
                _netServer = Global.Register<NetServer>();
                OpenRoom(true);
                return;
            }

            OpenRoom(false);
        }

        private void OpenRoom(bool acceptsRemoteJoin)
        {
            _room = Global.Register<RoomManager>();
            _room.SessionEnded += OnSessionEnded;

            if (acceptsRemoteJoin)
            {
                _room.CreateHostRoom();
            }
            else
            {
                _room.CreateLocalRoom();
            }
        }

        /// <summary>
        /// 拆除对局：名册与传输登记。回到菜单的那条边上，以及程序退出时调用。
        /// </summary>
        private void CloseSession()
        {
            if (!_isSessionOpen) return;
            _isSessionOpen = false;

            if (_room != null)
            {
                _room.SessionEnded -= OnSessionEnded;
                Global.Unregister<RoomManager>();
                _room = null;
            }

            if (_netClient != null)
            {
                _netClient.OnReconnectFailed -= OnReconnectFailed;
                Global.Unregister<NetClient>();
                _netClient = null;
            }

            if (_netServer != null)
            {
                Global.Unregister<NetServer>();
                _netServer = null;
            }
        }

        #endregion
        
        #region 生命周期

        protected override void Init()
        {
            _fsm = new StateMachine<ProcedureState>();
            _fsm.OnStateChange += OnProcedureStateChange;
            _fsm.OnTransitionFailed += OnTransitionFailed;
            _fsm.RegisterState(new ProcedureMenuState(_fsm, this));
            _fsm.RegisterState(new ProcedurePreparingState(_fsm, this));
            _fsm.RegisterState(new ProcedureMatchState(_fsm, this));

            // 子系统就绪之后才启动流程，已经就绪时立刻回调
            AppCore.OnAppReady += StartUp;
            AppCore.OnAppQuit += ShutDown;
        }

        private void Update()
        {
            // ===== DEBUG =====
            if (Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame)
            {
                BackToMainMenu();
            }
            // ===== DEBUG =====
            
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
            AppCore.OnAppReady -= StartUp;
            AppCore.OnAppQuit -= ShutDown;
            _fsm.OnStateChange -= OnProcedureStateChange;
            _fsm.OnTransitionFailed -= OnTransitionFailed;

            // 退出时不走回菜单那条业务路径，只拆对局，避免在退出过程中发起场景加载
            _fsm.Stop();
            CloseSession();
        }

        #endregion

        #region 事件回调

        /// <summary>
        /// 回到菜单意味着这一场对局结束，在旧状态退出之后、菜单进入之前拆掉。
        /// </summary>
        private void OnProcedureStateChange(ProcedureState from, ProcedureState to)
        {
            if (to != ProcedureState.Menu) return;

            CloseSession();
        }

        private void OnSessionEnded()
        {
            RequestMenu();
        }

        private void OnReconnectFailed()
        {
            RequestMenu();
        }

        /// <summary>
        /// 加载任务失败时场景没有换过去，仍停在发起加载时的那张图，直接回菜单。
        /// </summary>
        private void OnTransitionFailed(StateTransition<ProcedureState> transition)
        {
            if (transition.NextState == ProcedureState.Menu) return;

            RequestMenu();
        }

        #endregion

        #region 客户端消息回调

        /// <summary>
        /// 加入被接受，接管已有连接并补上名册。
        /// </summary>
        public void HandleGameJoinResponse(Room_Join_Response response)
        {
            // 拒绝加入
            if (!response.Accepted)
            {
                RequestMenu();
                return;
            }
            
            _room = Global.Register<RoomManager>();
            _room.HandleJoinResponse(response);
            _room.SessionEnded += OnSessionEnded;
            
            RequestScene(ProcedureState.Match, GlobalConstants.LEVEL_SCENE_NAME);
        }

        #endregion
    }
}
