using EventProcess;

namespace Framework
{
    /// 游戏状态管理器全局事件列表
    public abstract class GameStateEvent
    {
        public class StateChange : AEvent<StateChangeData> { }
        public struct StateChangeData
        {
            public GameState OldState;
            public GameState NewState;
        }
    }
}