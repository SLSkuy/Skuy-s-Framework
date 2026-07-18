namespace GamePlay.EntitySystem
{
    public enum NetEntityRole
    {
        /// <summary>
        /// 消耗输入命令进行模拟
        /// </summary>
        Authority,
        
        /// <summary>
        /// 消耗状态快照进行模拟
        /// </summary>
        Replica
    }
}
