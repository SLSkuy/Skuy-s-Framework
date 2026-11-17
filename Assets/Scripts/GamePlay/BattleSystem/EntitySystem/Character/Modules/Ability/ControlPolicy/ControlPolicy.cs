using System;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 技能释放期间的控制策略
    /// </summary>
    [Flags]
    public enum ControlPolicy
    {
        None = 0,
        LockMeshRotate = 1 << 0,
        LockViewRotate = 1 << 1,
    }
}