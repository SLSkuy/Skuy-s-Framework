using Framework.FiniteStateMachine;

namespace Framework
{
    /// <summary>
    /// 全局游戏状态：主菜单
    /// </summary>
    public class MainMenuState : StateBase<GameState>
    {
        public override GameState StateKey => GameState.MainMenu;

        public MainMenuState(StateMachine<GameState> manager) : base(manager)
        {
        }
    }
}