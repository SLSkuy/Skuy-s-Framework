using System;
using Framework.StateMachine;

namespace Framework
{
    /// <summary>
    /// 游戏状态管理器
    /// </summary>
    public class GameStateManager : SubSystemBase
    {
        public override int Priority => (int)SubSystemPriority.GameStateManager;
        public GameState CurrentState => _fsm.CurrentState;
        
        private EnumStateMachine<GameState> _fsm;

        #region 事件
        public event Action<GameStateEvent.StateChangeData> OnGameStateChange;
        #endregion
        
        /// <summary>
        /// 切换游戏状态
        /// </summary>
        /// <param name="state"></param>
        public void ChangeState(GameState state)
        {
            _fsm.ChangeState(state);
        }
        
        private void RegisterStates()
        {
            _fsm.RegisterState(new MainMenuState(_fsm));
            _fsm.RegisterState(new LoadingState(_fsm));
            _fsm.RegisterState(new GamingState(_fsm));
            _fsm.RegisterState(new PauseState(_fsm));
        }

        /// <summary>
        /// 游戏状态变化事件
        /// </summary>
        private void DispatchStateChangeEvent(GameState from, GameState to)
        {
            GameStateEvent.StateChangeData data = new() {OldState = from, NewState = to};
            OnGameStateChange?.Invoke(data);
            EventBus.Get<GameStateEvent.StateChange>().Dispatch(data);
        }

        public bool IsPaused() => CurrentState == GameState.GamePaused;
        public bool IsGameStarted() => CurrentState != GameState.MainMenu && CurrentState != GameState.None;

        #region 生命周期
        
        public override void Init()
        {
            _fsm = new EnumStateMachine<GameState>();
            _fsm.OnStateChange += DispatchStateChangeEvent;
            RegisterStates();
            _fsm.ChangeState(GameState.MainMenu);
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
            _fsm.OnStateChange -= DispatchStateChangeEvent;
        }

        #endregion
    }
}