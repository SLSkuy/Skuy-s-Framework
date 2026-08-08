using Framework.StateMachine;

namespace Framework
{
    /// <summary>
    /// 全局游戏状态：主菜单
    /// </summary>
    public class MainMenuState : EnumStateBase<GameState>
    {
        public override int StateKey => (int)GameState.MainMenu;

        public MainMenuState(EnumStateMachine<GameState> manager) : base(manager) { }
    }
}