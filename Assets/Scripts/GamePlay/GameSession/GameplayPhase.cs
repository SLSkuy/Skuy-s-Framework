namespace GamePlay.GameSession
{
    /// <summary>
    /// 对局内关卡流程相位。仅在玩法粘合点持有期间有意义。
    /// </summary>
    public enum GameplayPhase
    {
        Idle,
        InLevel,
        ChangingLevel
    }
}
