using System;
using System.Collections.Generic;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// Tick驱动轨道，单个技能实例运行控制器
    /// 可插入技能效果事件，播放到对应Tick自动触发
    /// </summary>
    public class SkillTrack : IDisposable
    {
        private readonly Dictionary<uint, SkillEffectEvent[]> _events = new();
        private readonly SkillConfig _config;

        /// <summary> 技能轨道长度 </summary>
        private readonly uint _trackLength;
        /// <summary> Tick频率 </summary>
        private readonly int _tickRate;

        #region 属性
        public SkillTrackState State { get; private set; }
        public bool IsRunning => State == SkillTrackState.Running;
        
        /// <summary> Tick执行进度 </summary>
        public uint ElapsedTicks { get; private set; }

        /// <summary>技能已用时间，由整数 Tick 推导 </summary>
        public float SkillElapsedTime => (float)ElapsedTicks / _tickRate;
        /// <summary>技能剩余时间，由总 Tick 减去已用 Tick 推导 </summary>
        public float SkillRemainingTime => (float)(_trackLength - ElapsedTicks) / _tickRate;
        #endregion

        #region 事件
        public event Action<SkillEndResult> OnTrackEnded;
        public event Action<SkillEffectEvent> OnSkillEffect;
        #endregion

        public SkillTrack(SkillConfig config, int tickRate)
        {
            State = SkillTrackState.Init;
            
            _tickRate = tickRate;
            _config = config;
            _trackLength = config.durationTicks;

            // 在轨道上添加技能效果事件
            for (int i = 0; i < config.effects.Length; i++)
            {
                SkillEffect effect = config.effects[i];
                AddEvent(effect.startTick, i, SkillEffectEventType.Start);
                AddEvent(effect.startTick + effect.durationTicks, i, SkillEffectEventType.Stop);
            }
        }

        /// <summary>
        /// 添加技能效果事件
        /// </summary>
        private void AddEvent(uint tick, int effectIndex, SkillEffectEventType eventType)
        {
            if (tick > _trackLength) tick = _trackLength; 
            
            var newEvent = new SkillEffectEvent
            {
                Tick = tick,
                EffectIndex = effectIndex,
                Type = eventType
            };
            
            if (_events.TryGetValue(tick, out var existing))
            {
                int oldLen = existing.Length;
                Array.Resize(ref existing, oldLen + 1);
                existing[oldLen] = newEvent;
                _events[tick] = existing;
            }
            else
            {
                _events[tick] = new[] { newEvent };
            }
        }

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// 判断当前Tick是否有技能事件并进行派发
        /// </summary>
        private void DispatchSkillEvent()
        {
            if (_events.TryGetValue(ElapsedTicks, out SkillEffectEvent[] events))
            {
                foreach (var e in events)
                {
                    OnSkillEffect?.Invoke(e);
                }
            }
        }

        #region 技能轨道控制方法

        public void Start()
        {
            if (IsRunning) return;
            
            State = SkillTrackState.Running;
            ElapsedTicks = 0;
            
            DispatchSkillEvent();
        }

        /// <summary>
        /// 暂停技能释放
        /// </summary>
        public void Pause()
        {
            if (!IsRunning) return;
            
            State = SkillTrackState.Paused;
        }
        
        /// <summary>
        /// 查询当前轨道拍是否处于指定中断窗口，不改变轨道状态
        /// </summary>
        public bool CheckInterrupt(SkillInterruptReason reason)
        {
            if (!IsRunning) return false;
            
            foreach (SkillInterruptRule rule in _config.interruptRules)
            {
                if (rule.reason == reason && ElapsedTicks >= rule.startTick && ElapsedTicks < rule.endTick) return true;
            }
            
            return false;
        }

        /// <summary>
        /// 尝试打断当前技能的释放
        /// </summary>
        public bool TryInterrupt(SkillInterruptReason reason)
        {
            if (!CheckInterrupt(reason)) return false;
            
            Interrupt(reason);
            
            return true;
        }

        /// <summary>
        /// 强制打断当前技能的释放
        /// </summary>
        public void Interrupt(SkillInterruptReason reason)
        {
            State = SkillTrackState.Interrupt;

            SkillEndCode code = reason == SkillInterruptReason.Replace
                ? SkillEndCode.Replaced
                : SkillEndCode.Interrupted;
            
            OnTrackEnded?.Invoke(new SkillEndResult(_config.skillId, code));
        }
        
        /// <summary>清除运行进度，不派发技能结束结算。</summary>
        public void Reset()
        {
            State = SkillTrackState.Init;
            ElapsedTicks = 0;
        }
        
        public void Tick()
        {
            if(!IsRunning) return;
            
            // 技能窗口 [0, _trackLength)
            ElapsedTicks++;
            DispatchSkillEvent();
            
            if(ElapsedTicks >= _trackLength)
            {
                State = SkillTrackState.Complete;
                OnTrackEnded?.Invoke(new SkillEndResult(_config.skillId, SkillEndCode.Completed));
            }
        }

        #endregion

        #region 快照管理

        public void RestoreSkillTrackState()
        {
            
        }

        #endregion

        public void Dispose()
        {
            _events.Clear();
            
            OnTrackEnded = null;
            OnSkillEffect = null;
        }
    }
}