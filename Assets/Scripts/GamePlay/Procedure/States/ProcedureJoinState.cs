using Framework;
using Network;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 远端加入状态，建立可靠连接并等待加入响应。不登记、不注销 NetClient。
    /// </summary>
    public sealed class ProcedureJoiningState : EnumStateBase<ProcedureState>
    {
        private readonly ProcedureHandler _handler;
        private readonly ProcedureCore _procedure;
        private bool _joinAccepted;
        
        public override int StateKey => (int)ProcedureState.Joining;
        
        public ProcedureJoiningState(EnumStateMachine<ProcedureState> stateMachine, ProcedureCore procedure)
            : base(stateMachine)
        {
            _procedure = procedure;
            _handler = new ProcedureHandler(procedure);
        }

        private void JoinResponse(bool response)
        {
            _joinAccepted = response;
        }
        
        #region 状态周期
        
        public override void Enter()
        {
            _procedure.OnJoinResponse += JoinResponse;
            _joinAccepted = false;
            
            NetClient client = Global.Get<NetClient>();
            _handler.Bind();
            client.StartReliableConnect();
        }
        
        public override void Exit()
        {
            _procedure.OnJoinResponse -= JoinResponse;
            _handler.Unbind();
            
            if (_joinAccepted)
            {
                _joinAccepted = false;
                return;
            }
            
            Global.Get<NetClient>().StopClient();
        }
        
        #endregion
    }
}
