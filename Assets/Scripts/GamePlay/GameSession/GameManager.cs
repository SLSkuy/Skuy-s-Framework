using Framework;

namespace GamePlay.GameSession
{
    /// <summary>
    /// 战局内流程：仅在房间开战之后由房间持有，不创建房间。
    /// </summary>
    public sealed class GameManager : SubSystemBase
    {
        #region 属性
        public override int Priority => 200;
        public GameplayPhase Phase { get; private set; } = GameplayPhase.Idle;
        #endregion
    }
}
