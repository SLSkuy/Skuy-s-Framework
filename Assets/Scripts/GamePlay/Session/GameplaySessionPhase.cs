namespace GamePlay
{
    /// <summary>
    /// 玩法会话相位，独立于应用壳层 <see cref="Framework.GameState"/>。
    /// </summary>
    public enum GameplaySessionPhase
    {
        Idle,
        InPlay,
        Ending
    }
}
