namespace GamePlay.EntitySystem
{
    public interface IPlayerSnapshot : IEntitySnapshot
    {
        /// <summary>
        /// 玩家对应的客户端ID索引
        /// </summary>
        uint ClientId { get; set; }
    }
}