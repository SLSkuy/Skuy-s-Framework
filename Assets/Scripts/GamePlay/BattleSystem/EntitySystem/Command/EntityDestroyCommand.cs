namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 销毁实体指令
    /// </summary>
    public struct EntityDestroyCommand
    {
        public uint entityId;

        public EntityDestroyCommand(uint id)
        {
            entityId = id;
        }
    }
}
