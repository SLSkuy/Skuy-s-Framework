namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体控制驱动模式。
    /// </summary>
    public enum EntityDriveMode
    {
        None,
        LocalInput,
        Prediction,
        Authority,
        Replica,
        AI
    }
}

