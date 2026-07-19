using Framework.FiniteStateMachine;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 全局游戏状态：加载中
    /// </summary>
    public class LoadingState : StateBase<GameState>
    {
        public override GameState StateKey => GameState.Loading;

        public LoadingState(StateMachine<GameState> stateMachine) : base(stateMachine)
        {
        }

        public override void Enter()
        {
            Time.timeScale = 1f;
        }
    }
}
