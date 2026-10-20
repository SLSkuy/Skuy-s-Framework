using Framework;
using GamePlay.Room;
using GamePlay.Simulation;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 对局流程状态，粘合其他的所有游戏功能模块。进入时关卡已经就绪。
    /// </summary>
    public sealed class ProcedureMatchState : ProcedureStateBase
    {
        public ProcedureMatchState(StateMachine<ProcedureState> stateMachine, ProcedureModule procedure)
            : base(stateMachine, procedure) { }

        private ISimulationKernel _kernel;
        private RoomManager _room;
        
        #region 属性
        public override ProcedureState StateKey => ProcedureState.Match;
        #endregion

        /// <summary>
        /// 初始化游戏
        /// </summary>
        private void InitGamePlay()
        {
            _room = Global.Get<RoomManager>();
            
            InitSimulationKernel();
        }

        /// <summary>
        /// 初始化网络同步模拟核
        /// </summary>
        private void InitSimulationKernel()
        {
            SessionRole role = _room.Role;
            _kernel = role == SessionRole.Host ? Global.Register<HostSimulationKernel>() 
                : Global.Register<ClientSimulationKernel>();
            _kernel.StartSession();
        }

        private void DestroyGamePlay()
        {
            DestroySimulationKernel();
            
            _room = null;
        }
        
        private void DestroySimulationKernel()
        {
            _kernel?.StopSession();
            _kernel = null;
            
            SessionRole role = _room.Role;
            if (role == SessionRole.Host) Global.Unregister<HostSimulationKernel>();
            else Global.Unregister<ClientSimulationKernel>();
        }
        
        #region 状态周期

        public override void Enter()
        {
            InitGamePlay();
        }

        public override void Exit()
        {
            DestroyGamePlay();   
        }
        
        #endregion
    }
}
