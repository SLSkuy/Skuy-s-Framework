using Framework;
using Network;
namespace GamePlay.Procedure
{
    /// <summary>
    /// 远端加入状态，建立可靠连接并等待加入响应
    /// </summary>
    public sealed class ProcedureJoiningState : EnumStateBase<ProcedureState>
    {
        private readonly ProcedureHandler _handler;
        private bool _joinAccepted;
        
        public override int StateKey => (int)ProcedureState.Joining;
        
        public ProcedureJoiningState(EnumStateMachine<ProcedureState> stateMachine, ProcedureCore procedures)
            : base(stateMachine)
        {
            _handler = new ProcedureHandler(procedures);
        }
        
        /// <summary>
        /// 标记连接成功，退出状态时不自动断开连接
        /// </summary>
        public void CommitJoin()
        {
            _joinAccepted = true;
        }
        
        #region 状态周期
        
        public override void Enter()
        {
            _joinAccepted = false;
            
            NetClient client = Global.Register<NetClient>();
            _handler.Bind();
            client.StartReliableConnect();
        }
        
        public override void Exit()
        {
            _handler.Unbind();
            
            if (_joinAccepted)
            {
                _joinAccepted = false;
                return;
            }
            
            if (!Global.TryGet(out NetClient client)) return;
            client.StopClient();
            Global.Unregister<NetClient>();
        }
        
        #endregion
    }
}