using UnityEngine;
using Framework.FiniteStateMachine;

namespace Framework
{
    /// <summary>
    /// 全局游戏状态：游戏中
    /// </summary>
    public class GamingState : StateBase<GameState>
    {
        public override GameState StateKey => GameState.Gaming;

        public GamingState(StateMachine<GameState> stateMachine) : base(stateMachine)
        {
        }

        public override void Enter()
        {
            Time.timeScale = 1f;
        }
    }
}