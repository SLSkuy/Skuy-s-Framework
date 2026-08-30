namespace GamePlay.GameSession
{
    /// <summary>
    /// 战局内关卡流程相位。仅在房间开战、持有 GameManager 期间有意义。
    /// </summary>
    public enum GameplayPhase
    {
        Idle,
        InLevel,
        ChangingLevel
    }
}
