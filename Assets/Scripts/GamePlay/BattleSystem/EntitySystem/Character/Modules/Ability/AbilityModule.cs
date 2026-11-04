using System;
using System.Collections.Generic;
using Framework;
using GamePlay.DataProxy;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 角色能力组件，管理施法实例和效果派发。
    /// 外部通过技能 ID 查询配置并发起释放。
    /// </summary>
    public class AbilityModule : EntitySnapshotModuleBase<AbilitySnapshot>
    {
        private readonly AbilityConfigProxy _abilityConfig = Global.GetDataProxy<AbilityConfigProxy>();
        private readonly SimulationDataProxy _simulation = Global.GetDataProxy<SimulationDataProxy>();

        private MovementModule _movementModule;
        private ViewModule _viewModule;
        
        /// <summary>所有技能效果执行器，按配置实际类型注册。</summary>
        private Dictionary<Type, ISkillEffectExecutor> _executors;
        private SkillInstance _castingSkill;

        #region 属性
        public override ModuleType ModuleType => ModuleType.Ability;
        public SkillInstance CastingSkill => _castingSkill;
        public bool IsCasting => _castingSkill is { IsRunning: true };

        /// <summary>技能已用时间，由整数 Tick 推导 </summary>
        public float SkillElapsedTime => _castingSkill?.ElapsedTime ?? 0f;
        /// <summary>技能剩余时间，由总 Tick 减去已用 Tick 推导 </summary>
        public float SkillRemainingTime => _castingSkill?.RemainingTime ?? 0f;
        #endregion

        /// <summary>
        /// 单主动技能释放；校验全部效果后创建实例并派发零时刻效果
        /// </summary>
        public bool TryCastSkill(uint skillId)
        {
            if (IsCasting || !_abilityConfig.TryGetSkillConfig(skillId, out EntitySkill skillConfig)) return false;
            
            SkillInstance instance = CreateInstance(skillConfig);
            _castingSkill = instance;
            instance.Cast();
            return true;
        }
        
        /// <summary>
        /// 尝试中断技能，只有在中断窗口中才能进行中断
        /// </summary>
        public bool TryInterruptSkill(SkillInterruptReason reason)
        {
            if (!IsCasting) return false;
            
            foreach (SkillInterruptRule rule in _castingSkill.SkillConfig.interruptRules)
            {
                // 判断中断原因是否处于中断窗口中
                if (rule.reason == reason && _castingSkill.ElapsedTicks >= rule.startTick && _castingSkill.ElapsedTicks < rule.endTick)
                {
                    Reset();
                    return true;
                }
            }
            return false;
        }

        public void Tick(float deltaTime)
        {
            // 视角旋转
            _viewModule.Look(Context.LastAimInput, deltaTime);
            
            // 模型旋转
            _movementModule.Rotate(Context.LastMoveInput, _viewModule.Yaw, deltaTime);
            
            // 技能效果
            _castingSkill?.Tick();
        }

        private SkillInstance CreateInstance(EntitySkill skill)
        {
            // TODO: 优化技能上下文逻辑，不再每次都创建一个新的动画实例对象
            SkillInstance instance = new SkillInstance(skill, _simulation.TickRate, DispatchSkillEffect);
            return instance;
        }

        private void DispatchSkillEffect(SkillEffect effect)
        {
            _executors[effect.GetType()].ExecuteSkillEffect(effect);
        }

        /// <summary>
        /// 初始化所有技能效果执行器
        /// </summary>
        private void InitExecutors()
        {
            _executors = new Dictionary<Type, ISkillEffectExecutor>
            {
                { typeof(AnimationSkillEffect), new AnimationEffectExecutor(Context, _simulation.TickRate) },
                { typeof(MoveSkillEffect), new LocomotionEffectExecutor(Context) }
            };
        }

        #region 快照处理

        public override AbilitySnapshot CaptureSnapshot()
        {
            // 没有释放的技能信息，传递空数据
            if (!IsCasting) return default;
            
            return new AbilitySnapshot
            {
                skillId = _castingSkill.SkillConfig.skillId,
                elapsedTime = SkillElapsedTime
            };
        }

        public override void RestoreSnapshot(in AbilitySnapshot state)
        {
            if (state.skillId == 0)
            {
                Reset();
                return;
            }

            if (!_abilityConfig.TryGetSkillConfig(state.skillId, out var skillConfig))
            {
                throw new InvalidOperationException($"技能快照引用未知技能：{state.skillId}");
            }
            
            // 恢复技能状态
            SkillInstance instance = CreateInstance(skillConfig);
            int elapsedTicks = AbilityUtils.SecondsToTicks(state.elapsedTime, _simulation.TickRate);
            instance.RestoreProgress(elapsedTicks);
            
            // 中断当前技能
            Reset();
            _castingSkill = instance;
        }

        #endregion
        
        protected override void OnBound(EntityCharacter target)
        {
            Reset();
            InitExecutors();
            
            _movementModule = Context.Get<MovementModule>();
            _viewModule = Context.Get<ViewModule>();
        }

        protected override void OnUnbound(EntityCharacter oldTarget)
        {
            Reset();
            _executors.Clear();
        }

        protected override void OnEnabledChanged(bool isEnabled)
        {
            if (!isEnabled) Reset();
        }
        
        public override void Reset()
        {
            _castingSkill?.Interrupt();
            _castingSkill = null;

            if (_executors != null)
            {
                foreach (ISkillEffectExecutor executor in _executors.Values) executor.StopExecution();
            }
        }
    }
}