using System;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 将时间映射到Tick
    /// </summary>
    public static class AbilityUtils
    {
        /// <summary>
        /// 非负秒数中点向上量化；非法时间及溢出拒绝
        /// </summary>
        public static int SecondsToTicks(double seconds, int tickRate)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0 || tickRate <= 0) 
                throw new ArgumentOutOfRangeException(nameof(seconds), "时间须有限且非负，频率须为正。");
            return checked((int)Math.Round(seconds * tickRate, MidpointRounding.AwayFromZero));
        }

        /// <summary>
        /// 分别量化起终点，正时长不足一个 Tick 时拒绝
        /// </summary>
        public static int DurationToTicks(double start, double duration, int tickRate)
        {
            if (double.IsNaN(duration) || double.IsInfinity(duration) || duration < 0)
                throw new ArgumentOutOfRangeException(nameof(duration));
            int ticks = checked(SecondsToTicks(start + duration, tickRate) - SecondsToTicks(start, tickRate));
            if (duration > 0 && ticks == 0)
                throw new ArgumentException("正时长量化后不足一个 Tick。", nameof(duration));
            return ticks;
        }
    }
}