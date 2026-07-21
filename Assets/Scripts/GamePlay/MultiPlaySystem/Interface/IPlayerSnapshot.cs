namespace GamePlay.EntitySystem
{
    public interface IPlayerSnapshot : IEntitySnapshot
    {
        /// <summary>
        /// 生成该状态时服务端已处理的最后一个输入 Tick。
        /// </summary>
        uint LastProcessedInputTick { get; set; }
    }
}
