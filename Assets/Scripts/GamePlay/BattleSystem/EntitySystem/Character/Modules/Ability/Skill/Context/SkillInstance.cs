using System;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 当前释放技能实例信息
    /// </summary>
    public class SkillInstance
    {
        private readonly Action<SkillEffect> _skillExecutor;

        /// <summary>
        /// 下一个技能效果
        /// </summary>
        private int _nextEffectIndex;
        
        #region 属性
        /// <summary> 当前释放中的技能配置信息 </summary>
        public EntitySkill SkillConfig { get; }

        /// <summary> 技能状态 </summary>
        public SkillState State { get; private set; } = SkillState.Created;
        public bool IsRunning => State == SkillState.Running;
        
        /// <summary> Tick频率 </summary>
        public int TickRate { get; }
        /// <summary> Tick执行进度 </summary>
        public int ElapsedTicks { get; private set; }
        
        /// <summary> 持续时间 </summary>
        public float ElapsedTime => (float)ElapsedTicks / TickRate;
        /// <summary> 剩余时间 </summary>
        public float RemainingTime => (float)(SkillConfig.durationTicks - ElapsedTicks) / TickRate;
        #endregion

        public SkillInstance(EntitySkill skillConfig, int tickRate, Action<SkillEffect> executor)
        {
            SkillConfig = skillConfig;
            TickRate = tickRate;
            _skillExecutor = executor;
        }

        public void Cast()
        {
            if(State != SkillState.Created) throw new InvalidOperationException("施法实例只能启动一次");
            
            State = SkillState.Running;
            
            // 触发0Tick时的效果，不然由Tick驱动会慢一Tick
            DispatchEffect();
        }
        
        /// <summary>
        /// 中断当前的技能释放
        /// </summary>
        public void Interrupt()
        {
            if(State == SkillState.Created || IsRunning)
                State = SkillState.Interrupted;
        }

        public void Tick()
        {
            if(!IsRunning) return;
            
            ElapsedTicks++;
            DispatchEffect();
            
            // 执行窗口[startTick, startTick + durationTick]
            // durationTick == 0/1 效果一致，至少会经过一个Tick
            if (ElapsedTicks >= SkillConfig.durationTicks) State = SkillState.Completed;
        }

        /// <summary>
        /// 派发技能效果，检测是否有技能效果达到了播放窗口
        /// </summary>
        private void DispatchEffect()
        {
            SkillEffect[] effects = SkillConfig.effects;
            
            while (IsRunning && _nextEffectIndex < effects.Length)
            {
                // TODO: SkillEffect需要按照 startTick -> order 排序
                SkillEffect effect = effects[_nextEffectIndex];
                if (effect.startTick > ElapsedTicks) break;

                // 先推进游标，避免效果回调导致重复派发
                _nextEffectIndex++;
                _skillExecutor(effect);
            }
        }

        /// <summary>
        /// 恢复技能技能释放进度
        /// </summary>
        public void RestoreProgress(int elapsedTicks)
        {
            if (State != SkillState.Created) throw new InvalidOperationException("恢复进度只能用于新实例。");
            if (elapsedTicks < 0 || elapsedTicks >= SkillConfig.durationTicks) throw new ArgumentOutOfRangeException(nameof(elapsedTicks));
            
            // TODO: 恢复技能释放进度
        }
    }
}