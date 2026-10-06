using Framework;
using NetSync;
using Network;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 开局准备。本机玩与开多人的对局在打开时就已就绪，加入远端要先建可靠连接并等加入响应。
    /// </summary>
    public sealed class ProcedurePreparingState : ProcedureStateBase
    {
        private readonly ProcedureHandler _handler;

        public override ProcedureState StateKey => ProcedureState.Preparing;

        public ProcedurePreparingState(StateMachine<ProcedureState> stateMachine, ProcedureCore procedure)
            : base(stateMachine, procedure)
        {
            _handler = new ProcedureHandler(this);
        }

        public override void ProcessIntent(ProcedureIntent intent)
        {
            if (intent != ProcedureIntent.BackToMenu) return;

            Procedure.RequestMenu();
        }

        #region 状态周期

        public override void Enter()
        {
            if (Procedure.MatchIntent != ProcedureIntent.JoinRemote)
            {
                Procedure.RequestScene(ProcedureState.Match, GlobalConstants.LEVEL_SCENE_NAME);
                return;
            }

            _handler.Bind();
            Global.Get<NetClient>().StartReliableConnect();
        }

        public override void Exit()
        {
            _handler.Unbind();
        }

        #endregion

        #region 客户端消息处理

        /// <summary>
        /// 加入响应到达：被拒则回菜单，被接受则接管名册并去加载关卡。
        /// </summary>
        public void HandleGameJoinResponse(Room_Join_Response response)
        {
            Procedure.HandleGameJoinResponse(response);
        }

        /// <summary>
        /// 可靠连接没能建起来，回菜单。
        /// </summary>
        public void HandleConnectionFailed()
        {
            Procedure.RequestMenu();
        }

        #endregion
    }
}
