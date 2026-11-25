using System;
using System.Collections.Generic;
using Framework;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 单个技能实例信息，存储释放进度，记录技能冷却
    /// </summary>
    public class SkillInstance : IDisposable, IPoolable
    {
        /// <summary>当前技能执行所需要的执行器，只 Tick 调度这些执行器</summary>
        private readonly IReadOnlyCollection<ISkillEffectExecutor> _requireExecutors;
        
        /// <summary>技能释放轨道，存储 Tick 级的技能事件信息</summary>
        private readonly SkillTrack _track;
        /// <summary>只读技能配置信息</summary>
        private readonly SkillConfig _config;
        private readonly bool[] _activeEffects;
        /// <summary>下一次技能冷却完毕时的时间。</summary>
        private double _nextExecutableTime;
        /// <summary> Tick频率 </summary>
        private readonly int _tickRate;

        #region 属性
        public uint SkillId => _config.skillId;
        public SkillState State { get; private set; }
        public ControlPolicy ControlPolicy => _config.controlPolicy;
        public bool IsCasting => State == SkillState.Casting;

        /// <summary>技能冷却剩余时间</summary>
        public double CooldownRemainTime => Math.Max(0, _nextExecutableTime - Global.CurrentTime);
        /// <summary>技能冷却进度；无冷却技能始终就绪</summary>
        public float CooldownPercent => _config.cooldown > 0 ? 1 - (float)CooldownRemainTime / _config.cooldown : 1;

        /// <summary>技能已用时间</summary>
        public float SkillElapsedTime => (float)_track.ElapsedTicks / _tickRate;
        /// <summary>技能释放进度百分比</summary>
        public float SkillElapsedPercent => (float)_track.ElapsedTicks / _config.durationTicks;
        
        /// <summary>技能释放剩余时间</summary>
        public float SkillRemainingTime => (float)_track.RemainingTicks / _tickRate;
        /// <summary>技能释放剩余百分比</summary>
        public float SkillRemainingPercent => (float)_track.RemainingTicks / _config.durationTicks;
        #endregion

        #region 事件
        /// <summary>由所属 AbilityModule 解释条件；多个订阅者必须全部通过</summary>
        public event Func<ICastCondition, bool> OnCastCheck;
        
        public event Action<SkillEffect> OnSkillEffectStart;
        public event Action<SkillEffect> OnSkillEffectEnd;
        
        /// <summary>技能结束事件</summary>
        public event Action<SkillEndResult> OnSkillEnded;
        #endregion

        public SkillInstance(SkillConfig skillConfig, IReadOnlyCollection<ISkillEffectExecutor> executors, int tickRate)
        {
            _tickRate = tickRate;   
            _config = skillConfig;
            _requireExecutors = executors;
            _activeEffects = new bool[skillConfig.effects.Length];
            
            _track = new SkillTrack(skillConfig);
            _track.OnTrackEnded += OnTrackEnded;
            _track.OnSkillEffect += OnDispatchEffect;
        }

        /// <summary>
        /// 检测当前技能当前状态
        /// </summary>
        public SkillStateCheckResult CheckCast()
        {
            if (State == SkillState.Casting) return SkillStateCheckResult.Casting;
            if (State == SkillState.Locked) return SkillStateCheckResult.SkillLocked;

            // 自身结束后会开始冷却；有冷却的技能不能在同一次提交中重启。
            if (Global.CurrentTime < _nextExecutableTime) return SkillStateCheckResult.Cooldown;

            // 执行条件判断
            foreach (ICastCondition condition in _config.castConditions)
            {
                if (OnCastCheck == null) throw new InvalidOperationException("技能条件检测必须绑定 AbilityModule");
                if (!OnCastCheck(condition))
                {
                    // 执行条件判断失败
                    return SkillStateCheckResult.ConditionFailed;
                }
            }
            
            // 返回技能执行检测结果
            return SkillStateCheckResult.Ready;
        }

        /// <summary>
        /// 资格检查成功后启动，由 AbilityModule 先登记当前技能
        /// </summary>
        public void StartCast()
        {
            State = SkillState.Casting;
            _track.Start();
        }

        /// <summary>
        /// 查询当前技能是否处于指定中断窗口
        /// </summary>
        public bool CheckInterrupt(SkillInterruptReason reason)
        {
            return IsCasting && _track.CheckInterrupt(reason);
        }
        
        /// <summary>
        /// 强制打断当前技能的释放
        /// </summary>
        public void Interrupt(SkillInterruptReason reason)
        {
            _track.Interrupt(reason);
        }

        /// <summary>
        /// 尝试以对应的中断原因进行技能中夺冠
        /// </summary>
        public bool TryInterrupt(SkillInterruptReason reason)
        {
            return IsCasting && _track.TryInterrupt(reason);
        }

        private void StopActiveEffects()
        {
            for (int i = 0; i < _activeEffects.Length; i++)
            {
                if (!_activeEffects[i]) continue;
                _activeEffects[i] = false;
                OnSkillEffectEnd?.Invoke(_config.effects[i]);
            }
        }

        #region 事件回调
        
        /// <summary>
        /// 轨道执行结束（中断/执行完毕）
        /// </summary>
        private void OnTrackEnded(SkillEndResult result)
        {
            StopActiveEffects();
            
            // 更新下次可释放技能时间
            State = SkillState.Idle;
            _nextExecutableTime = Global.CurrentTime + _config.cooldown;
            
            OnSkillEnded?.Invoke(result);
        }
        
        /// <summary>
        /// 派发技能效果，并记录需要在中断时清理的活动效果
        /// </summary>
        private void OnDispatchEffect(SkillEffectEvent evt)
        {
            SkillEffect effect = _config.effects[evt.EffectIndex];
            if (evt.Type == SkillEffectEventType.Start)
            {
                _activeEffects[evt.EffectIndex] = true;
                OnSkillEffectStart?.Invoke(effect);
            }
            else if (evt.Type == SkillEffectEventType.Stop && _activeEffects[evt.EffectIndex])
            {
                _activeEffects[evt.EffectIndex] = false;
                OnSkillEffectEnd?.Invoke(effect);
            }
        }

        #endregion
        
        #region 快照处理

        /// <summary>
        /// 恢复技能释放进度
        /// </summary>
        public void RestoreSkillState(int elapsedTicks)
        {
            // TODO: 恢复技能状态
        }

        #endregion
        
        #region 生命周期
        
        // ReSharper disable Unity.PerformanceAnalysis
        public void Tick(float deltaTime)
        {
            if (!IsCasting) return;

            // 执行当前区间 [ElapsedTicks, ElapsedTicks + 1) 的技能效果。
            foreach (ISkillEffectExecutor executor in _requireExecutors) executor.Tick(deltaTime);

            // 区间执行完成后推进轨道，处理下一边界的开始与结束事件。
            _track.Tick();
        }

        public void Reset()
        {
            StopActiveEffects();
            _track.Reset();
            
            _nextExecutableTime = 0;
            
            bool wasCasting = IsCasting;
            State = SkillState.Idle;
            if(wasCasting) OnSkillEnded?.Invoke(new SkillEndResult(SkillId, SkillEndCode.Reset));
        }

        public void Dispose()
        {
            Reset();
            
            _track.OnTrackEnded -= OnTrackEnded;
            _track.OnSkillEffect -= OnDispatchEffect;
            _track.Dispose();
            
            OnCastCheck = null;
            OnSkillEffectStart = null;
            OnSkillEffectEnd = null;
            OnSkillEnded = null;
        }

        #endregion
    }
}