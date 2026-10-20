using Framework;
using GamePlay.Room;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 玩法流程。只调度已经拉起的房间和场景，不登记数据、不创建房间。
    /// 对局在离开菜单时打开，回到菜单的那条边上拆除。
    /// </summary>
    public sealed class ProcedureModule : SubSystemBase
    {
        private StateMachine<ProcedureState> _fsm;
        private RoomManager _room;
        private bool _isSessionOpen;

        #region 属性
        public override int Priority => (int)GamePlaySystemPriority.Procedure;

        /// <summary>
        /// 当前这一次带任务的切换。同时只有一个。
        /// </summary>
        public ProcedureTransition PendingTransition { get; private set; }
        #endregion

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

        /// <summary>
        /// 创建联机房间
        /// </summary>
        public void StartHostPlay(RoomConfig config = null)
        {
            _room.StartServer(config);
            RequestScene(ProcedureState.Match, GlobalConst.LEVEL_SCENE_NAME);

            _isSessionOpen = true;
        }

        /// <summary>
        /// 加入远程房间
        /// </summary>
        public void StartMultiPlay()
        {
            _room.StartConnecting();
            _isSessionOpen = true;
        }

        /// <summary>
        /// 返回到主菜单
        /// </summary>
        public void BackToMainMenu()
        {
            RequestScene(ProcedureState.Menu, GlobalConst.MENU_SCENE_NAME);
        }

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

        private void CloseSession()
        {
            if (!_isSessionOpen) return;

            _room.Dissolve();
            _isSessionOpen = false;
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

        /// <summary>
        /// 加入被接受，接管已有连接并补上名册。
        /// </summary>
        private void OnSessionConnected()
        {
            RequestScene(ProcedureState.Match, GlobalConst.LEVEL_SCENE_NAME);
        }

        #region 子系统生命周期

        public override void Init()
        {
            _room = Global.Get<RoomManager>();

            _fsm = new StateMachine<ProcedureState>();
            _fsm.OnStateChange += OnProcedureStateChange;
            _fsm.RegisterState(new ProcedureMenuState(_fsm, this));
            _fsm.RegisterState(new ProcedureMatchState(_fsm, this));
            _fsm.RegisterState(new ProcedureLoadingState(_fsm, this));
            _fsm.ChangeState(ProcedureState.Menu);
        }

        public override void BindEvents()
        {
            _room.SessionConnected += OnSessionConnected;
            _room.SessionDisconnected += OnSessionEnded;
            _room.SessionReconnectFailed += OnReconnectFailed;
        }

        public override void UnbindEvents()
        {
            _fsm.OnStateChange -= OnProcedureStateChange;
            _room.SessionConnected -= OnSessionConnected;
            _room.SessionDisconnected -= OnSessionEnded;
            _room.SessionReconnectFailed -= OnReconnectFailed;
        }

        public override void Update(float deltaTime)
        {
            _fsm.Update(deltaTime);
        }

        public override void FixedUpdate(float fixedDeltaTime)
        {
            _fsm.FixedUpdate(fixedDeltaTime);
        }

        public override void LateUpdate()
        {
            _fsm.LateUpdate();
        }

        public override void Destroy()
        {
            _fsm.Stop();
        }

        #endregion
    }
}
