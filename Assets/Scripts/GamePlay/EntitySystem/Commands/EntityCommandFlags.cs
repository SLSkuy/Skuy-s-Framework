using System;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体控制命令类型枚举定义
    /// </summary>
    [Flags]
    public enum EntityCommandFlags : ushort
    {
        None = 0,
        Sprint = 1 << 0,
        Jump = 1 << 1,
        ToggleRun = 1 << 2,
        PrimaryAttack = 1 << 3,
        SpecialAttack = 1 << 4,
        SpecialAction = 1 << 5,
        Interact = 1 << 6
    }
}
