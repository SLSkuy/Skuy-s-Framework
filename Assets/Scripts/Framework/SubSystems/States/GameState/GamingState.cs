using Framework.StateMachine;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 全局游戏状态：游戏中
    /// </summary>
    public class GamingState : EnumStateBase<GameState>
    {
        public override int StateKey => (int)GameState.Gaming;

        public GamingState(EnumStateMachine<GameState> enumStateMachine) : base(enumStateMachine) { }

        public override void Enter()
        {
            Time.timeScale = 1f;
        }
    }
}