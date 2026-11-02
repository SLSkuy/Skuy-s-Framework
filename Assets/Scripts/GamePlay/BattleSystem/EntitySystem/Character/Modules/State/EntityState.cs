namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 实体状态
    /// </summary>
    public enum EntityState : uint
    {
        IDLE = 0,
        WALK = 1,
        RUN = 2,
        AIRBORNE = 3,
        ACTION = 4,
    }
}
