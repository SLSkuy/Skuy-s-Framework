using Framework;
using GamePlay.Battle;
using UnityEngine;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 玩法流程核心，与 GameCore 同级，UI 意图入口，并拥有会话级登记
    /// </summary>
    public sealed class ProcedureCore : MonoSingleton<ProcedureCore>
    {
        private EnumStateMachine<GameProcedure> _fsm;
        private SessionIntent _sessionIntent;
        private bool _startMatchWhenLobbyReady;

        #region 属性
        public GameProcedure CurrentProcedure => _fsm.CurrentState;
        #endregion

        /// <summary>
        /// 单机：大厅建不接远端的房间后立即开战。
        /// </summary>
        public void StartLocal()
        {
            _sessionIntent = SessionIntent.Local;
            _startMatchWhenLobbyReady = true;
            _fsm.ChangeState(GameProcedure.Lobby);
        }

        /// <summary>
        /// 多人开房：建可加入房间并停在大厅。
        /// </summary>
        public void HostMultiplayer()
        {
            _sessionIntent = SessionIntent.Host;
            _startMatchWhenLobbyReady = false;
            _fsm.ChangeState(GameProcedure.Lobby);
        }

        /// <summary>
        /// 加入远端：建客户端房间并停在大厅。
        /// </summary>
        public void JoinRemote()
        {
            _sessionIntent = SessionIntent.Join;
            _startMatchWhenLobbyReady = false;
            _fsm.ChangeState(GameProcedure.Lobby);
        }

        /// <summary>
        /// 房主请求开战。
        /// </summary>
        public void RequestStartMatch()
        {
            if (!Global.TryGet(out BattleManager battle))
            {
                return;
            }

            if (battle.ActiveRoom == null)
            {
                return;
            }

            if (battle.ActiveRoom.SessionRole != BattleSessionRole.Host)
            {
                return;
            }

            _fsm.ChangeState(GameProcedure.Match);
        }

        /// <summary>
        /// 结束对局但保留房间，回到大厅。
        /// </summary>
        public void EndMatch()
        {
            if (CurrentProcedure != GameProcedure.Match)
            {
                return;
            }

            _fsm.ChangeState(GameProcedure.Lobby);
        }

        /// <summary>
        /// 解散会话并回到菜单。
        /// </summary>
        public void LeaveSession()
        {
            _fsm.ChangeState(GameProcedure.Menu);
        }

        internal void OnLobbyEntered()
        {
            if (!Global.TryGet(out BattleManager battle))
            {
                battle = Global.Register<BattleManager>();
            }

            if (battle.ActiveRoom != null)
            {
                return;
            }

            switch (_sessionIntent)
            {
                case SessionIntent.Local:
                    battle.CreateLocalRoom();
                    break;
                case SessionIntent.Host:
                    battle.CreateHostRoom();
                    break;
                case SessionIntent.Join:
                    battle.JoinRemoteRoom();
                    break;
            }
        }

        internal void OnMatchEntered()
        {
            if (!Global.Get<BattleManager>().StartBattle())
            {
                _fsm.ChangeState(GameProcedure.Lobby);
            }
        }

        internal void OnMatchExited()
        {
            if (!Global.TryGet(out BattleManager battle))
            {
                return;
            }

            if (battle.ActiveRoom == null)
            {
                return;
            }

            battle.StopBattle();
        }

        internal void TearDownSession()
        {
            _sessionIntent = SessionIntent.None;
            _startMatchWhenLobbyReady = false;
            if (!Global.TryGet(out BattleManager battle))
            {
                return;
            }

            Global.Unregister<BattleManager>();
        }

        #region 生命周期

        private void Start()
        {
            _fsm = new EnumStateMachine<GameProcedure>();
            _fsm.RegisterState(new ProcedureMenuState(_fsm, this));
            _fsm.RegisterState(new ProcedureLobbyState(_fsm, this));
            _fsm.RegisterState(new ProcedureMatchState(_fsm, this));
            _fsm.ChangeState(GameProcedure.Menu);
        }

        private void Update()
        {
            _fsm.Update(Time.deltaTime);
            if (!_startMatchWhenLobbyReady || CurrentProcedure != GameProcedure.Lobby)
            {
                return;
            }

            _startMatchWhenLobbyReady = false;
            RequestStartMatch();
        }

        private void FixedUpdate()
        {
            _fsm.FixedUpdate(Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            _fsm.LateUpdate();
        }

        #endregion
    }
}
