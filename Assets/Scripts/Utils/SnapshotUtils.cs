using Framework;
using GamePlay.BattleSystem;

namespace Utils
{
    /// <summary>
    /// 控制命令工具
    /// </summary>
    public static class SnapshotUtils
    {
        /// <summary>
        /// 获取当前Tick按住的按键集合
        /// </summary>
        public static EntityCommandFlags GetHeldFlags(in InputCommand input)
        {
            EntityCommandFlags flags = EntityCommandFlags.None;
            if (input.IsSprintPressed) flags |= EntityCommandFlags.Sprint;
            if (input.IsJumpPressed) flags |= EntityCommandFlags.Jump;
            if (input.IsSwitchModePressed) flags |= EntityCommandFlags.ToggleRun;
            if (input.IsPrimaryAttackPressed) flags |= EntityCommandFlags.PrimaryAttack;
            if (input.IsSpecialAttackPressed) flags |= EntityCommandFlags.SpecialAttack;
            if (input.IsSpecialActionPressed) flags |= EntityCommandFlags.SpecialAction;
            if (input.IsInteractPressed) flags |= EntityCommandFlags.Interact;
            return flags;
        }
    }
}