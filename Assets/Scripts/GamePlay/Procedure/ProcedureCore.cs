using System.Collections.Generic;
using Framework;
using GamePlay.Battle;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 玩法流程核心，与 GameCore 同级，UI 意图入口，并拥有会话级登记
    /// </summary>
    public sealed class ProcedureCore : MonoSingleton<ProcedureCore>
    {
        public const string MenuSceneName = "MainScene";
        public const string LevelSceneName = "GameScene";

        private EnumStateMachine<GameProcedure> _fsm;
        private SessionIntent _sessionIntent;
        private MatchDebugHud _matchHud;

        #region 属性
        public GameProcedure CurrentProcedure => _fsm.CurrentState;
        #endregion

        /// <summary>
        /// 本机玩：建本机房并进入对局，切关后再开战。
        /// </summary>
        public void StartLocal()
        {
            _sessionIntent = SessionIntent.Local;
            OpenHostSession(false);
            _fsm.ChangeState(GameProcedure.Match);
        }

        /// <summary>
        /// 开多人：本机入座、开听、对局已提交，切关后再开战。
        /// </summary>
        public void HostMultiplayer()
        {
            _sessionIntent = SessionIntent.Host;
            OpenHostSession(true);
            _fsm.ChangeState(GameProcedure.Match);
        }

        /// <summary>
        /// 加入远端：先连接，被接受前没有房间；接受后才进入对局。
        /// </summary>
        public void JoinRemote()
        {
            _sessionIntent = SessionIntent.Join;
            if (!Global.TryGet(out BattleManager battle))
            {
                battle = Global.Register<BattleManager>();
            }

            battle.JoinSettled -= HandleJoinSettled;
            battle.JoinSettled += HandleJoinSettled;
            battle.JoinRemoteRoom();
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
        /// 结束对局即解散，回到菜单。
        /// </summary>
        public void EndMatch()
        {
            LeaveSession();
        }

        /// <summary>
        /// 解散会话并回到菜单。
        /// </summary>
        public void LeaveSession()
        {
            _fsm.ChangeState(GameProcedure.Menu);
        }

        /// <summary>
        /// 供常驻 HUD 读取名册，不把战局管理器交给界面。
        /// </summary>
        public void CopyRosterPlayerIds(List<uint> buffer)
        {
            buffer.Clear();
            if (!Global.TryGet(out BattleManager battle) || battle.ActiveRoom == null)
            {
                return;
            }

            battle.ActiveRoom.CopyPlayerIds(buffer);
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

            if (_sessionIntent == SessionIntent.Join)
            {
                battle.JoinRemoteRoom();
            }
        }

        internal void OnMatchEntered()
        {
            EventBus.Get<SceneLoadEvent.Completed>().AddListener(HandleLevelLoaded);
            ShowMatchHud();
            if (SceneManager.GetActiveScene().name == LevelSceneName)
            {
                StartBattleAfterLevelReady();
                return;
            }

            Global.LoadScene(LevelSceneName);
        }

        internal void OnMatchExited()
        {
            EventBus.Get<SceneLoadEvent.Completed>().RemoveListener(HandleLevelLoaded);
            if (!Global.TryGet(out BattleManager battle) || battle.ActiveRoom == null)
            {
                return;
            }

            battle.StopBattle();
        }

        internal void TearDownSession()
        {
            _sessionIntent = SessionIntent.None;
            HideMatchHud();
            EventBus.Get<SceneLoadEvent.Completed>().RemoveListener(HandleLevelLoaded);
            if (Global.TryGet(out BattleManager battle))
            {
                battle.JoinSettled -= HandleJoinSettled;
            }

            Global.Unregister<BattleManager>();

            if (Global.TryGet(out SceneLoader loader) && loader.IsLoading || 
                SceneManager.GetActiveScene().name != MenuSceneName)
            {
                Global.LoadScene(MenuSceneName);
            }
        }

        private void HandleJoinSettled(bool accepted)
        {
            if (Global.TryGet(out BattleManager battle))
            {
                battle.JoinSettled -= HandleJoinSettled;
            }

            if (accepted)
            {
                _fsm.ChangeState(GameProcedure.Match);
                return;
            }

            TearDownSession();
        }

        private void OpenHostSession(bool acceptsRemoteJoin)
        {
            if (!Global.TryGet(out BattleManager battle))
            {
                battle = Global.Register<BattleManager>();
            }

            if (battle.ActiveRoom != null)
            {
                return;
            }

            if (acceptsRemoteJoin)
            {
                battle.CreateHostRoom();
            }
            else
            {
                battle.CreateLocalRoom();
            }
        }

        private void HandleLevelLoaded(SceneLoadEvent.CompletedData data)
        {
            if (data.SceneName != LevelSceneName)
            {
                return;
            }

            StartBattleAfterLevelReady();
        }

        private void StartBattleAfterLevelReady()
        {
            if (!Global.TryGet(out BattleManager battle) || battle.ActiveRoom == null)
            {
                _fsm.ChangeState(GameProcedure.Menu);
                return;
            }

            if (!battle.StartBattle())
            {
                _fsm.ChangeState(GameProcedure.Menu);
            }
        }

        private void ShowMatchHud()
        {
            if (_matchHud == null)
            {
                _matchHud = gameObject.AddComponent<MatchDebugHud>();
            }

            _matchHud.enabled = true;
        }

        private void HideMatchHud()
        {
            if (_matchHud != null)
            {
                _matchHud.enabled = false;
            }
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
