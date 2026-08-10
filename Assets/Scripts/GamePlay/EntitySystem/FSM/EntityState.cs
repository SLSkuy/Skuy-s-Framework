namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体状态
    /// </summary>
    public static class EntityState
    {
        public const uint IDLE = 0;
        public const uint WALK = 1;
        public const uint RUN = 2;
        public const uint SPRINT = 3;
        public const uint DASH = 4;
        public const uint AIRBORNE = 5;
    }
}
