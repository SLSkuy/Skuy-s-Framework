using System;

namespace Framework
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
        Roll = 1 << 3,
        PrimaryAttack = 1 << 4,
        SpecialAttack = 1 << 5,
        SpecialAction = 1 << 6,
        Interact = 1 << 7
    }
}