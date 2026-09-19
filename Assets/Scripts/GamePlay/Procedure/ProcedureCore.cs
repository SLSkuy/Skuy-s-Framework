using Core;
using Framework;
using GamePlay.Room;
using NetSync;
using Network;
using UnityEngine;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 玩法流程核心：菜单与对局门闩，界面意图入口。
    /// </summary>
    public sealed class ProcedureCore : MonoSingleton<ProcedureCore>
    {
        private ProcedureHandler _handler;
        private EnumStateMachine<ProcedureState> _fsm;
        private bool _joinInFlight;

        /// <summary>
        /// 本机玩：建本机对局并进入对局。
        /// </summary>
        public void StartLocal()
        {
            if (_joinInFlight) return;

            OpenHostSession(false);
            _fsm.ChangeState(ProcedureState.Match);
        }

        /// <summary>
        /// 开多人：本机入座、开听、对局已提交，进入对局。
        /// </summary>
        public void HostMultiplayer()
        {
            if (_joinInFlight) return;

            OpenHostSession(true);
            _fsm.ChangeState(ProcedureState.Match);
        }

        /// <summary>
        /// 加入远端：先连接，被接受前仍在菜单且没有对局；接受后才登记名册并进入对局。
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
        /// 解散对局并回到菜单。加入尚未完成时取消加入。
        /// </summary>
        public void LeaveSession()
        {
            if (_joinInFlight)
            {
                TearDownSession();
                return;
            }

            _fsm.ChangeState(ProcedureState.Menu);
        }

        /// <summary>
        /// 取消尚未完成的加入，或拆除名册并加载菜单入口。玩法对象由对局态离开时拆除。
        /// </summary>
        public void TearDownSession()
        {
            bool joining = _joinInFlight;
            _joinInFlight = false;

            _handler.Unbind();
            if (joining)
            {
                Global.Get<NetClient>().StopClient();
                Global.Unregister<NetClient>();
                return;
            }

            if (Global.TryGet(out RoomManager battle))
            {
                battle.SessionEnded -= HandleSessionEnded;
                Global.Unregister<RoomManager>();
                Global.LoadScene(GlobalConstants.MENU_SCENE_NAME);
            }
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

        private void HandleSessionEnded()
        {
            if (Global.TryGet(out RoomManager battle))
            {
                battle.SessionEnded -= HandleSessionEnded;
            }

            _fsm.ChangeState(ProcedureState.Menu);
        }

        #region 生命周期

        protected override void Init()
        {
            _handler = new ProcedureHandler(this);
            _fsm = new EnumStateMachine<ProcedureState>();
            _fsm.RegisterState(new ProcedureMenuState(_fsm, this));
            _fsm.RegisterState(new ProcedureMatchState(_fsm, this));
            _fsm.ChangeState(ProcedureState.Menu);
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

        #region 网络消息处理

        /// <summary>
        /// 加入响应到达：拒绝则结束加入；接受则接管名册并进入对局。
        /// </summary>
        public void HandleGameJoinResponse(Game_Join_Response response)
        {
            if (!response.Accepted)
            {
                HandleConnectionFailed();
                return;
            }

            _joinInFlight = false;
            RoomManager room = Global.Register<RoomManager>();
            room.HandleGameJoinResponseJoin(response);
            room.SessionEnded += HandleSessionEnded;
            _fsm.ChangeState(ProcedureState.Match);
        }

        /// <summary>
        /// 连接失败或加入被拒。
        /// </summary>
        public void HandleConnectionFailed()
        {
            TearDownSession();
        }

        #endregion
    }
}
