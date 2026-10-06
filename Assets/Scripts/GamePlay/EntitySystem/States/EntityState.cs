namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体状态
    /// </summary>
    public enum EntityState : uint
    {
        IDLE = 0,
        WALK = 1,
        RUN = 2,
        SPRINT = 3,
        AIRBORNE = 4,
    }
}
