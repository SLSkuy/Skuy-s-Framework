using Framework;
using GamePlay.Battle;
using GamePlay.Simulator;

namespace GamePlay.GameSession
{
    /// <summary>
    /// 战局内流程：仅在房间开战之后由房间持有，不创建房间。
    /// </summary>
    public sealed class GameManager : SubSystemBase
    {
        private readonly BattleRoom _room;
        private readonly LocalSimulationHost _simulationHost;

        public GameManager(BattleRoom room, LocalSimulationHost simulationHost)
        {
            _room = room;
            _simulationHost = simulationHost;
        }

        #region 属性
        public override int Priority => 200;
        public GameplayPhase Phase { get; private set; } = GameplayPhase.Idle;
        #endregion

        /// <summary>
        /// 对局已启动模拟后进入关卡进行中。
        /// </summary>
        public void BeginMatch()
        {
            Phase = GameplayPhase.InLevel;
        }

        /// <summary>
        /// 对局结束时复位相位。不解散房间、不停止模拟（由房间 EndMatch 处理）。
        /// </summary>
        public void NotifyMatchEnded()
        {
            Phase = GameplayPhase.Idle;
        }

        #region 子系统生命周期

        public override void Destroy()
        {
            NotifyMatchEnded();
        }

        #endregion
    }
}
