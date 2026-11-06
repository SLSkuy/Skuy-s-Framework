using System;
using Framework;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 单个技能实例信息
    /// 存储释放进度，记录技能冷却
    /// </summary>
    public class SkillInstance : IDisposable, IPoolable
    {
        /// <summary> 技能释放轨道，存储Tick级的技能事件信息 </summary>
        private readonly SkillTrack _track;
        /// <summary> 只读技能配置信息 </summary>
        private readonly SkillConfig _config;
        /// <summary> 下一次技能冷却完毕时的时间 </summary>
        private double _nextExecutableTime;
        
        #region 属性
        /// <summary> 技能状态 </summary>
        public SkillState State { get; private set; }
        public bool IsCasting => State == SkillState.Casting;

        /// <summary>
        /// 技能冷却剩余时间
        /// </summary>
        public double CooldownRemainTime
        {
            get
            {
                double remainTime = _config.cooldown - Global.CurrentTime;
                return remainTime > 0 ? remainTime : 0;
            }
        }

        /// <summary>
        /// 技能冷却百分比
        /// </summary>
        public float CooldownPercent
        {
            get
            {
                double remainTime = CooldownRemainTime;
                return 1 - (float)remainTime / _config.cooldown;
            }
        }
        #endregion

        #region 事件
        public event Action<SkillEffect> OnSkillEffectStart;
        public event Action<SkillEffect> OnSkillEffectEnd;
        #endregion

        public SkillInstance(SkillConfig skillConfig, int tickRate)
        {
            _config = skillConfig;
            
            _track = new SkillTrack(skillConfig, tickRate);
            _track.OnInterrupt += SkillFinished;
            _track.OnCompleted += SkillFinished;
            _track.OnSkillEffect += DispatchEffect;
        }

        /// <summary>
        /// 尝试释放技能
        /// </summary>
        public bool TryCastSkill()
        {
            if(State != SkillState.Idle) return false;
            
            // 技能冷却中
            if(Global.CurrentTime < _nextExecutableTime) return false;
            
            _track.Start();
            State = SkillState.Casting;
            
            return true;
        }
        
        /// <summary>
        /// 中断当前的技能释放
        /// </summary>
        public bool TryInterrupt(SkillInterruptReason reason)
        {
            if(State != SkillState.Casting) return false;
            
            return _track.Interrupt(reason);
        }

        public void Tick(float deltaTime)
        {
            // 更新轨道
            _track.Tick();
        }

        /// <summary>
        /// 派发技能效果，检测是否有技能效果达到了播放窗口
        /// </summary>
        private void DispatchEffect(SkillEffectEvent evt)
        {
            SkillEffect effect = _config.effects[evt.EffectIndex];
            if (evt.Type == SkillEffectEventType.Start)
            {
                OnSkillEffectStart?.Invoke(effect);
            }else if (evt.Type == SkillEffectEventType.Stop)
            {
                OnSkillEffectEnd?.Invoke(effect);
            }
        }

        private void SkillFinished()
        {
            // 更新下次可释放技能时间
            State = SkillState.Idle;
            _nextExecutableTime = Global.CurrentTime + _config.cooldown;
        }
        
        #region 快照逻辑
        
        /// <summary>
        /// 恢复技能技能释放进度
        /// </summary>
        public void RestoreSkillState(int elapsedTicks)
        {
            // TODO: 恢复技能状态
        }
        
        #endregion

        public void Reset()
        {
            State = SkillState.Idle;
            _nextExecutableTime = 0;
        }
        
        public void Dispose()
        {
            _track.OnInterrupt -= SkillFinished;
            _track.OnCompleted -= SkillFinished;
            _track.OnSkillEffect -= DispatchEffect;
            _track?.Dispose();

            OnSkillEffectStart = null;
            OnSkillEffectEnd = null;
        }
    }
}