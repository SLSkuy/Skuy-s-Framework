using Framework.StateMachine;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 全局游戏状态：暂停
    /// </summary>
    public class PauseState : EnumStateBase<GameState>
    {
        public override int StateKey => (int)GameState.GamePaused;

        public PauseState(EnumStateMachine<GameState> enumStateManager) : base(enumStateManager) { }

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