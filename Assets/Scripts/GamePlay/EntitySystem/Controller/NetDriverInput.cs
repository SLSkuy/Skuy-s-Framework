using Framework;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 驱动器共享的输入映射工具，把 InputState 全字段写入 EntityCharacter。
    /// 持续型输入直接写；边沿型输入（Jump/ToggleRun）做按下边沿检测。
    /// </summary>
    internal static class NetDriverInput
    {
        /// <summary>
        /// 把 InputState 映射到 EntityCharacter，并更新边沿检测基线。
        /// </summary>
        public static void ApplyTo(EntityCharacter character, in InputState input, ref InputState previousInput)
        {
            // 持续型：移动、瞄准、疾跑
            character.Move(input.MoveInput);
            character.Aim(input.AimInput);
            if (input.IsSprintPressed) character.StartSprint();
            else character.StopSprint();

            // 边沿型：跳跃、切换模式（按下边沿触发）
            if (input.IsJumpPressed && !previousInput.IsJumpPressed)
                character.Jump();
            if (input.IsSwitchModePressed && !previousInput.IsSwitchModePressed)
                character.ToggleRun();

            // 攻击/特殊/交互暂不接（对应旧版 IPlayerCharacter 的 TODO 占位）

            previousInput = input;
        }
    }
}
