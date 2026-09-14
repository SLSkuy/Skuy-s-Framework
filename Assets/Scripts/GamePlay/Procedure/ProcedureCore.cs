using Core;
using Framework;
using GamePlay.GameCore;
using GamePlay.Room;
using NetSync;
using Network;
using UnityEngine;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 玩法流程核心，控制游戏整体流程
    /// </summary>
    public sealed class ProcedureCore : MonoSingleton<ProcedureCore>
    {
        private ProcedureHandler _handler;
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
            NetClient client = Global.Register<NetClient>();
            _handler.Bind();
            client.StartReliableConnect();
        }

        /// <summary>
        /// 加入响应到达：拒绝则结束加入；接受则接管名册并进入对局。
        /// </summary>
        public void CompleteJoin(Game_Join_Response response)
        {
            _handler.Unbind();
            if (!response.Accepted)
            {
                FailJoin();
                return;
            }

            _joinInFlight = false;
            RoomManager room = Global.Register<RoomManager>();
            room.CompleteClientJoin(response);
            room.SessionEnded += HandleSessionEnded;
            _fsm.ChangeState(GameProcedure.Match);
        }

        /// <summary>
        /// 连接失败或加入被拒。
        /// </summary>
        public void FailJoin()
        {
            TearDownSession();
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
        }

        #region 流程控制

        public void OnMatchEntered()
        {
            ShowMatchHud();
            GameManager gameplay = Global.Register<GameManager>();
            gameplay.OrchestrationFailed += HandleOrchestrationFailed;
        }

        /// <summary>
        /// 关闭当前存在的对局，或取消尚未完成的加入。
        /// </summary>
        public void TearDownSession()
        {
            bool joining = _joinInFlight;
            _joinInFlight = false;
            HideMatchHud();

            _handler.Unbind();
            if (joining)
            {
                // 加入过程中还没有房间管理器，需要手动关闭连接
                Global.Get<NetClient>().StopClient();
                Global.Unregister<NetClient>();
                return;
            }

            // 开启过房间后才注销子模块
            if (Global.TryGet(out RoomManager battle))
            {
                battle.SessionEnded -= HandleSessionEnded;
                
                Global.Unregister<GameManager>();
                Global.Unregister<RoomManager>();
                Global.LoadScene(GlobalConstants.MENU_SCENE_NAME);
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

        protected override void Init()
        {
            _handler = new ProcedureHandler(this);
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

        private void HandleOrchestrationFailed()
        {
            LeaveSession();
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
