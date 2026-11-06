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
        public event Action OnInterrupt;
        public event Action OnCompleted;
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
        /// 尝试打断当前技能的释放
        /// </summary>
        public bool Interrupt(SkillInterruptReason reason)
        {
            if (State == SkillTrackState.Init || IsRunning )
            {
                foreach (SkillInterruptRule rule in _config.interruptRules)
                {
                    // 判断中断原因是否处于中断窗口中
                    if (rule.reason == reason && ElapsedTicks >= rule.startTick && ElapsedTicks < rule.endTick)
                    {
                        State = SkillTrackState.Interrupt;
                        OnInterrupt?.Invoke();
                        return true;
                    }
                }
            }

            return false;
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
                OnCompleted?.Invoke();
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

            OnInterrupt = null;
            OnCompleted = null;
            OnSkillEffect = null;
        }
    }
}