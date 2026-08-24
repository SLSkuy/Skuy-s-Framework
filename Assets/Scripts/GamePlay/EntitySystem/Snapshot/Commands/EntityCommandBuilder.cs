using Framework;
using Utils;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 将输入状态转换为每 Tick 完整命令，并在命令边界计算按钮边沿。
    /// </summary>
    public sealed class EntityCommandBuilder
    {
        private EntityCommandFlags _previousHeldFlags;

        /// <summary>
        /// 根据当前输入状态构建命令。
        /// </summary>
        public EntityCommand Build(uint tick, in InputState input)
        {
            EntityCommandFlags heldFlags = SnapshotUtils.GetHeldFlags(input);
            EntityCommand command = new()
            {
                tick = tick,
                move = input.MoveInput,
                aim = input.AimInput,
                flagsHeld = heldFlags,
                flagsPressed = heldFlags & ~_previousHeldFlags,
                flagsReleased = _previousHeldFlags & ~heldFlags
            };

            _previousHeldFlags = heldFlags;
            return command;
        }

        /// <summary>
        /// 清空边沿检测基线。
        /// </summary>
        public void Reset()
        {
            _previousHeldFlags = EntityCommandFlags.None;
        }
    }
}
