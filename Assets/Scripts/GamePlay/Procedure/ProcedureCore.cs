using Core;
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
        private EnumStateMachine<GameProcedure> _fsm;
        private MatchDebugHud _matchHud;
        private bool _joinInFlight;

        #region 属性
        public GameProcedure CurrentProcedure => _fsm.CurrentState;
        public bool SessionIsHost => Global.TryGet(out BattleManager battle) && battle.ActiveRoom is { SessionRole: BattleSessionRole.Host };
        #endregion

        /// <summary>
        /// 本机玩：建本机房并进入对局，切关后再开战。
        /// </summary>
        public void StartLocal()
        {
            if (_joinInFlight)
            {
                return;
            }

            OpenHostSession(false);
            _fsm.ChangeState(GameProcedure.Match);
        }

        /// <summary>
        /// 开多人：本机入座、开听、对局已提交，切关后再开战。
        /// </summary>
        public void HostMultiplayer()
        {
            if (_joinInFlight)
            {
                return;
            }

            OpenHostSession(true);
            _fsm.ChangeState(GameProcedure.Match);
        }

        /// <summary>
        /// 加入远端：先连接，被接受前没有房间；接受后才进入对局。
        /// </summary>
        public void JoinRemote()
        {
            if (_joinInFlight)
            {
                return;
            }

            if (!Global.TryGet(out BattleManager battle))
            {
                battle = Global.Register<BattleManager>();
            }

            _joinInFlight = true;
            battle.JoinSettled += HandleJoinSettled;
            battle.SessionEnded += HandleSessionEnded;
            battle.JoinRemoteRoom();
        }

        /// <summary>
        /// 解散会话并回到菜单。加入尚未完成时取消加入。
        /// </summary>
        public void LeaveSession()
        {
            if (_joinInFlight)
            {
                TearDownSession();
                return;
            }

            _fsm.ChangeState(GameProcedure.Menu);
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

            battle.SessionEnded += HandleSessionEnded;
            if (acceptsRemoteJoin)
            {
                battle.CreateHostRoom();
            }
            else
            {
                battle.CreateLocalRoom();
            }
        }

        #region 流程控制

        public void OnMatchEntered()
        {
            EventBus.Get<SceneLoadEvent.Completed>().AddListener(HandleLevelLoaded);
            ShowMatchHud();
            if (SceneManager.GetActiveScene().name == GameConstants.LEVEL_SCENE_NAME)
            {
                StartBattleAfterLevelReady();
                return;
            }

            Global.LoadScene(GameConstants.LEVEL_SCENE_NAME);
        }

        public void OnMatchExited()
        {
            EventBus.Get<SceneLoadEvent.Completed>().RemoveListener(HandleLevelLoaded);
        }

        /// <summary>
        /// 关闭当前存在的战局会话
        /// </summary>
        public void TearDownSession()
        {
            _joinInFlight = false;
            HideMatchHud();
            EventBus.Get<SceneLoadEvent.Completed>().RemoveListener(HandleLevelLoaded);
            if (Global.TryGet(out BattleManager battle))
            {
                battle.JoinSettled -= HandleJoinSettled;
                battle.SessionEnded -= HandleSessionEnded;
            }

            Global.Unregister<BattleManager>();

            if (Global.TryGet(out SceneLoader loader) && loader.IsLoading ||
                SceneManager.GetActiveScene().name != GameConstants.MENU_SCENE_NAME)
            {
                Global.LoadScene(GameConstants.MENU_SCENE_NAME);
            }
        }

        #endregion

        // ========== HUD调试 ==========
        // ========== HUD调试 ==========
        // ========== HUD调试 ==========
        private void ShowMatchHud()
        {
            if (!_matchHud)
            {
                _matchHud = gameObject.AddComponent<MatchDebugHud>();
            }

            _matchHud.enabled = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>
        /// 供常驻 HUD 读取名册，不把战局管理器交给界面。
        /// </summary>
        public uint[] GetRosterPlayerIds()
        {
            if (!Global.TryGet(out BattleManager battle) || battle.ActiveRoom == null)
            {
                return System.Array.Empty<uint>();
            }

            return battle.ActiveRoom.GetPlayerIds();
        }

        private void HideMatchHud()
        {
            if (_matchHud)
            {
                _matchHud.enabled = false;
            }
        }
        // ========== HUD调试 ==========
        // ========== HUD调试 ==========
        // ========== HUD调试 ==========

        #region 生命周期

        private void Start()
        {
            _fsm = new EnumStateMachine<GameProcedure>();
            _fsm.RegisterState(new ProcedureMenuState(_fsm, this));
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

        #region 回调处理

        private void HandleLevelLoaded(SceneLoadEvent.CompletedData data)
        {
            if (CurrentProcedure != GameProcedure.Match || data.SceneName != GameConstants.LEVEL_SCENE_NAME)
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

        /// <summary>
        /// 处理房间加入流程
        /// </summary>
        private void HandleJoinSettled(bool accepted)
        {
            _joinInFlight = false;
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

        /// <summary>
        /// 处理会话关闭流程
        /// </summary>
        private void HandleSessionEnded()
        {
            if (Global.TryGet(out BattleManager battle))
            {
                battle.SessionEnded -= HandleSessionEnded;
            }

            _fsm.ChangeState(GameProcedure.Menu);
        }

        #endregion
    }
}
