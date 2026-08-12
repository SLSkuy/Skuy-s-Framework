using Framework;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 将输入状态转换为每 Tick 完整命令，并在命令边界计算按钮边沿。
    /// </summary>
    public sealed class EntityInputCommandBuilder
    {
        private EntityCommandButtons _previousHeldButtons;

        /// <summary>
        /// 根据当前输入状态构建命令。
        /// </summary>
        public EntityInputCommand Build(uint tick, in InputState input)
        {
            EntityCommandButtons heldButtons = GetHeldButtons(input);
            EntityInputCommand command = new()
            {
                Tick = tick,
                Move = input.MoveInput,
                Aim = input.AimInput,
                ButtonsHeld = heldButtons,
                ButtonsPressedThisTick = heldButtons & ~_previousHeldButtons
            };

            _previousHeldButtons = heldButtons;
            return command;
        }

        /// <summary>
        /// 将边沿检测基线恢复到指定输入状态。
        /// </summary>
        public void RestoreBaseline(in InputState input)
        {
            _previousHeldButtons = GetHeldButtons(input);
        }

        /// <summary>
        /// 清空边沿检测基线。
        /// </summary>
        public void Reset()
        {
            _previousHeldButtons = EntityCommandButtons.None;
        }

        private static EntityCommandButtons GetHeldButtons(in InputState input)
        {
            EntityCommandButtons buttons = EntityCommandButtons.None;
            if (input.IsSprintPressed) buttons |= EntityCommandButtons.Sprint;
            if (input.IsJumpPressed) buttons |= EntityCommandButtons.Jump;
            if (input.IsSwitchModePressed) buttons |= EntityCommandButtons.ToggleRun;
            if (input.IsPrimaryAttackPressed) buttons |= EntityCommandButtons.PrimaryAttack;
            if (input.IsSpecialAttackPressed) buttons |= EntityCommandButtons.SpecialAttack;
            if (input.IsSpecialActionPressed) buttons |= EntityCommandButtons.SpecialAction;
            if (input.IsInteractPressed) buttons |= EntityCommandButtons.Interact;
            return buttons;
        }
    }
}
