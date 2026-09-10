using Core;
using Framework;
using GamePlay.GameSession;
using GamePlay.Room;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 玩法流程核心，与 GameCore 同级，UI 意图入口，并拥有会话级登记
    /// </summary>
    public sealed class ProcedureCore : MonoSingleton<ProcedureCore>
    {
        private ProcedureJoinClientHandler _joinClientHandler;
        private EnumStateMachine<GameProcedure> _fsm;
        private MatchDebugHud _matchHud;
        private bool _joinInFlight;
        
        #region 属性
        public GameProcedure CurrentProcedure => _fsm.CurrentState;
        public bool SessionIsHost => Global.TryGet(out RoomManager battle) && battle.IsInMatch && 
                                     battle.SessionRole == SessionRole.Host;
        #endregion

        /// <summary>
        /// 本机玩：建本机对局并进入对局。
        /// </summary>
        public void StartLocal()
        {
            if (_joinInFlight) return;

            OpenHostSession(false);
            _fsm.ChangeState(GameProcedure.Match);
        }

        /// <summary>
        /// 开多人：本机入座、开听、对局已提交，进入对局。
        /// </summary>
        public void HostMultiplayer()
        {
            if (_joinInFlight) return;

            OpenHostSession(true);
            _fsm.ChangeState(GameProcedure.Match);
        }

        /// <summary>
        /// 加入远端：先连接，被接受前仍在菜单且没有对局；接受后才登记名册与玩法粘合点。
        /// </summary>
        public void JoinRemote()
        {
            if (_joinInFlight) return;

            _joinInFlight = true;
            _joinClientHandler = new ProcedureJoinClientHandler();
            _joinClientHandler.JoinSettled += HandleJoinSettled;
            _joinClientHandler.StartJoin();
        }

        /// <summary>
        /// 解散对局并回到菜单。加入尚未完成时取消加入。
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
            if (!Global.TryGet(out RoomManager battle))
            {
                battle = Global.Register<RoomManager>();
            }

            if (battle.IsInMatch)
            {
                return;
            }

            battle.SessionEnded += HandleSessionEnded;
            if (acceptsRemoteJoin)
            {
                // 创建主机房间
                battle.CreateHostRoom();
            }
            else
            {
                // 创建本地房间
                battle.CreateLocalRoom();
            }

            LaunchGame(SessionRole.Host);
        }

        /// <summary>
        /// 打开游戏管理器，正式开启游戏
        /// </summary>
        private void LaunchGame(SessionRole role)
        {
            if (!Global.TryGet(out GameManager gameManager))
            {
                gameManager = Global.Register<GameManager>();
            }

            gameManager.InitMatch(role);
        }

        #region 流程控制

        public void OnMatchEntered()
        {
            ShowMatchHud();
            if (!Global.Get<GameManager>().EnterMatch())
            {
                _fsm.ChangeState(GameProcedure.Menu);
            }
        }

        /// <summary>
        /// 关闭当前存在的对局，或取消尚未完成的加入。
        /// </summary>
        public void TearDownSession()
        {
            _joinInFlight = false;
            HideMatchHud();
            
            if (_joinClientHandler != null)
            {
                _joinClientHandler.JoinSettled -= HandleJoinSettled;
                _joinClientHandler.CancelJoin();
                _joinClientHandler = null;    
            }
            
            if (Global.TryGet(out RoomManager battle))
            {
                battle.SessionEnded -= HandleSessionEnded;
            }

            Global.Unregister<GameManager>();
            Global.Unregister<RoomManager>();

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
            if (!Global.TryGet(out RoomManager battle) || !battle.IsInMatch)
            {
                return System.Array.Empty<uint>();
            }

            return battle.GetPlayerIds();
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

        private void HandleJoinSettled(bool accepted)
        {
            _joinInFlight = false;

            if (accepted)
            {
                _joinClientHandler.JoinSettled -= HandleJoinSettled;
                _joinClientHandler = null;
                
                Global.Get<RoomManager>().SessionEnded += HandleSessionEnded;
                LaunchGame(SessionRole.Client);
                _fsm.ChangeState(GameProcedure.Match);
                return;
            }

            TearDownSession();
        }

        private void HandleSessionEnded()
        {
            if (Global.TryGet(out RoomManager battle))
            {
                battle.SessionEnded -= HandleSessionEnded;
            }

            _fsm.ChangeState(GameProcedure.Menu);
        }

        #endregion
    }
}
