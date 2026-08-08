using Framework.StateMachine;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 全局游戏状态：加载中
    /// </summary>
    public class LoadingState : EnumStateBase<GameState>
    {
        public override int StateKey => (int)GameState.Loading;

        public LoadingState(EnumStateMachine<GameState> enumStateMachine) : base(enumStateMachine) { }

        public override void Enter()
        {
            Time.timeScale = 1f;
        }
    }
}
