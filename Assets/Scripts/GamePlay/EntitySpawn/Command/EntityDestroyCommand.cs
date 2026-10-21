namespace GamePlay.EntitySpawn
{
    /// <summary>
    /// 销毁实体指令
    /// </summary>
    public struct EntityDestroyCommand : IEntityFactoryCommand
    {
        public uint entityId;

        public EntityDestroyCommand(uint id)
        {
            entityId = id;
        }
    }
}
