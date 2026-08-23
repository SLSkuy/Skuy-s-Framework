using System;
using System.Collections.Generic;
using GamePlay.EntitySystem;

namespace GamePlay.MultiPlaySystem
{
    /// <summary>
    /// 本地预测命令历史、确认与重放策略。
    /// </summary>
    public sealed class CharacterPredictionController
    {
        private readonly EntityPredictionHistory _history;
        private readonly List<EntityPredictionFrame> _replayFrames = new();

        #region Properties
        public EntityPredictionHistory History => _history;
        public List<EntityPredictionFrame> ReplayFrames => _replayFrames;
        public uint LastConfirmedInputTick { get; private set; }
        public uint NextInputTick { get; private set; }
        #endregion

        public CharacterPredictionController(int historySize)
        {
            _history = new EntityPredictionHistory(Math.Max(2, historySize));
        }

        /// <summary>
        /// 分配下一个预测输入 Tick。序号从 1 起连续增加，不与快照 Tick 对齐。
        /// </summary>
        public uint AllocateInputTick()
        {
            uint inputTick = NextInputTick;
            if (inputTick == 0)
            {
                inputTick = 1;
            }

            NextInputTick = inputTick + 1;
            return inputTick;
        }

        /// <summary>
        /// 更新最近确认输入 Tick。
        /// </summary>
        public void Confirm(uint confirmedTick)
        {
            LastConfirmedInputTick = Math.Max(LastConfirmedInputTick, confirmedTick);
        }

        /// <summary>
        /// 在历史缺失后保证后续输入 Tick 位于确认点之后。
        /// </summary>
        public void AdvanceAfter(uint confirmedTick)
        {
            NextInputTick = Math.Max(NextInputTick, confirmedTick + 1);
        }

        /// <summary>
        /// 清空预测历史与 Tick 锚点。
        /// </summary>
        public void Reset()
        {
            _history.Clear();
            _replayFrames.Clear();
            LastConfirmedInputTick = 0;
            NextInputTick = 0;
        }
    }
}
