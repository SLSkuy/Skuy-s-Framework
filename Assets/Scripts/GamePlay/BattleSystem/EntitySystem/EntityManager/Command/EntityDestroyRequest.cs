namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 销毁实体指令
    /// </summary>
    public struct EntityDestroyRequest
    {
        public uint entityId;

        public EntityDestroyRequest(uint id)
        {
            entityId = id;
        }
    }
}
