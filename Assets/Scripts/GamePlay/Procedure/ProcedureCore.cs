using Core;
using Framework;
using GamePlay.Room;
using NetSync;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 玩法流程核心：菜单与对局门闩，界面意图入口。
    /// </summary>
    public sealed class ProcedureCore : MonoSingleton<ProcedureCore>
    {
        private EnumStateMachine<ProcedureState> _fsm;
        private ProcedureJoiningState _joiningState;

        /// <summary>
        /// 本机玩：建本机对局并进入对局。
        /// </summary>
        public void StartLocalPlay()
        {
            if (_fsm.CurrentState != ProcedureState.Menu) return;

            OpenHostSession(false);
            _fsm.ChangeState(ProcedureState.Match);
        }

        /// <summary>
        /// 开多人：本机入座、开听、对局已提交，进入对局。
        /// </summary>
        public void HostMultiplay()
        {
            if (_fsm.CurrentState != ProcedureState.Menu) return;

            OpenHostSession(true);
            _fsm.ChangeState(ProcedureState.Match);
        }

        /// <summary>
        /// 加入远端：先连接，被接受前仍在菜单且没有对局；接受后才登记名册并进入对局。
        /// </summary>
        public void JoinRemote()
        {
            if (_fsm.CurrentState != ProcedureState.Menu) return;
            _fsm.ChangeState(ProcedureState.Joining);
        }

        /// <summary>
        /// 解散对局并回到菜单
        /// </summary>
        public void BackToMainMenu()
        {
            if (_fsm.CurrentState == ProcedureState.Menu) return;
            _fsm.ChangeState(ProcedureState.Menu);
        }

        /// <summary>
        /// 拆除名册并加载菜单入口。加入中的连接由 Joining 状态退出时拆除。
        /// </summary>
        public void TearDownSession()
        {
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
            _fsm = new EnumStateMachine<ProcedureState>();
            _fsm.RegisterState(new ProcedureMenuState(_fsm, this));
            _fsm.RegisterState(new ProcedureJoiningState(_fsm, this));
            _fsm.RegisterState(new ProcedureMatchState(_fsm, this));
            _fsm.ChangeState(ProcedureState.Menu);
        }

        private void Update()
        {
            // ===== DEBUG =====
            if (Keyboard.current.f5Key.wasPressedThisFrame)
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
            if (_fsm.CurrentState == ProcedureState.Joining)
            {
                // 取消连接
                _joiningState.Exit();
            }

            if (Global.TryGet(out RoomManager battle))
            {
                battle.SessionEnded -= HandleSessionEnded;
            }
        }

        private void OnApplicationQuit()
        {
            ShutDown();
        }

        #endregion

        #region 网络消息处理

        /// <summary>
        /// 加入响应到达：拒绝则结束加入；接受则接管名册并进入对局。
        /// </summary>
        public void HandleGameJoinResponse(Room_Join_Response response)
        {
            if (_fsm.CurrentState != ProcedureState.Joining) return;
            if (!response.Accepted)
            {
                HandleConnectionFailed();
                return;
            }

            _joiningState.CommitJoin();
            RoomManager room = Global.Register<RoomManager>();
            room.HandleJoinResponse(response);
            room.SessionEnded += HandleSessionEnded;
            _fsm.ChangeState(ProcedureState.Match);
        }

        /// <summary>
        /// 连接失败或加入被拒。
        /// </summary>
        public void HandleConnectionFailed()
        {
            if (_fsm.CurrentState != ProcedureState.Joining) return;
            _fsm.ChangeState(ProcedureState.Menu);
        }

        #endregion
    }
}
