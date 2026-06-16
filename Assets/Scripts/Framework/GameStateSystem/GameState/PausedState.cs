using Framework.FiniteStateMachine;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 全局游戏状态：暂停
    /// </summary>
    public class PauseState : StateBase<GameState>
    {
        public override GameState StateKey => GameState.GamePaused;

        public PauseState(StateMachine<GameState> stateManager) : base(stateManager)
        {
        }

        public override void Enter()
        {
            Time.timeScale = 0f;

            Debug.Log("游戏暂停");
        }

        public override void Exit()
        {
            Time.timeScale = 1f;

            Debug.Log("退出暂停");
        }
    }
}