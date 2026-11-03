using System;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 当前释放技能实例信息
    /// </summary>
    public class SkillInstance
    {
        private Action<SkillEffect> _skillExecutor;
        
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
        }

        /// <summary>
        /// 派发技能效果
        /// </summary>
        private void DispatchEffect(SkillEffect effect)
        {
            _skillExecutor(effect);
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