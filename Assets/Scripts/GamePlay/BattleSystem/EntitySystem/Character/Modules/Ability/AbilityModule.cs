using System;
using System.Collections.Generic;
using System.Linq;
using Framework;
using GamePlay.DataProxy;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 角色能力组件，管理施法实例和效果派发
    /// 外部通过传入技能ID进行能力释放
    /// </summary>
    public class AbilityModule : EntitySnapshotModuleBase<AbilitySnapshot>
    {
        private readonly AbilityConfigProxy _abilityConfig = Global.GetDataProxy<AbilityConfigProxy>();
        private readonly SimulationDataProxy _simulation = Global.GetDataProxy<SimulationDataProxy>();

        private MovementModule _movementModule;
        private ViewModule _viewModule;
        private CombatConfig _config;
        
        /// <summary> 条件解释器，按条件配置的实际类型注册 </summary>
        private Dictionary<Type, IConditionInterpreter> _conditionInterpreters;
        
        /// <summary> 所有技能效果执行器，按配置实际类型注册 </summary>
        private Dictionary<Type, ISkillEffectExecutor> _executors;
        
        /// <summary> 实体技能示例，创建时进行注册 </summary>
        private Dictionary<uint, SkillInstance> _skillInstances;
        private SkillInstance _currentSkill;

        #region 属性
        public override ModuleType Type => ModuleType.Ability;

        /// <summary> 是否正在释放技能 </summary>
        public bool IsCasting => _currentSkill is { IsCasting : true };
        #endregion

        /// <summary>
        /// 单主动技能释放；返回状态、冷却和条件检查的结果
        /// </summary>
        public SkillExecutionResult TryCastSkill(uint skillId)
        {
            // TODO: 后续实现技能中断，当前保持必须释放完一个能力时候才能释放下一个能力
            if (IsCasting) return new SkillExecutionResult(skillId, SkillExecutionCode.Casting);
            
            // 不存在技能配置
            if (!_skillInstances.TryGetValue(skillId, out SkillInstance instance))
            {
                // TODO: 添加日志处理类，自定义DEBUG信息
                return new SkillExecutionResult(skillId, SkillExecutionCode.SkillNotFound);
            }
            
            SkillExecutionResult result = instance.TryCastSkill();
            if (!result.IsSuccess) return result;
            
            _currentSkill = instance;
            return result;
        }
        
        /// <summary>
        /// 尝试中断技能，只有在中断窗口中才能进行中断
        /// </summary>
        public bool TryInterruptSkill(SkillInterruptReason reason)
        {
            return _currentSkill?.TryInterrupt(reason) ?? false;
        }

        public void Tick(float deltaTime)
        {
            // 视角旋转
            _viewModule.Look(Context.LastAimInput, deltaTime);
            
            // 模型旋转
            _movementModule.Rotate(Context.LastMoveInput, _viewModule.Yaw, deltaTime);
            
            // 技能效果
            _currentSkill?.Tick(deltaTime);
        }
        
        /// <summary>
        /// 初始化所有技能效果执行器
        /// </summary>
        private void InitEffectExecutors()
        {
            _executors = new Dictionary<Type, ISkillEffectExecutor>
            {
                { typeof(AnimationSkillEffect), new AnimationEffectExecutor(Context, _simulation.TickRate) },
                { typeof(MoveSkillEffect), new LocomotionEffectExecutor(Context) }
            };
        }
        
        private void InitConditionInterpreters()
        {
            _conditionInterpreters = new Dictionary<Type, IConditionInterpreter>
            {
                { typeof(EntityStateCondition), new EntityStateConditionInterpreter(Context) }
            };
        }

        /// <summary>
        /// 初始化当前实体能够执行的所有技能
        /// </summary>
        private void InitSkills()
        {
            _skillInstances = new Dictionary<uint, SkillInstance>();
            int tickRate = _simulation.TickRate;
            
            // 每个技能示例只Tick它所需的技能效果
            List<ISkillEffectExecutor> executors = new List<ISkillEffectExecutor>();
            foreach (uint id in _config.skillIds)
            {
                if (_abilityConfig.TryGetSkillConfig(id, out SkillConfig config))
                {
                    executors.AddRange(config.effects.Select(effect => _executors[effect.GetType()]).Distinct());

                    SkillInstance instance = new SkillInstance(config, executors.ToArray(), tickRate);
                    instance.OnCastCheck += OnCastCheck;
                    instance.OnSkillEffectStart += ExecuteSkillEffect;
                    instance.OnSkillEffectEnd += StopSkillEffect;
                    _skillInstances.Add(id, instance);
                    
                    executors.Clear();
                }
            }
        }

        /// <summary>
        /// 检测执行条件
        /// </summary>
        private bool OnCastCheck(ICondition condition)
        {
            return _conditionInterpreters[condition.GetType()].Evaluate(condition);
        }
        
        private void ExecuteSkillEffect(SkillEffect effect)
        {
            _executors[effect.GetType()].ExecuteSkillEffect(effect);
        }

        private void StopSkillEffect(SkillEffect effect)
        {
            _executors[effect.GetType()].StopExecution();
        }

        #region 快照逻辑

        public override AbilitySnapshot CaptureSnapshot()
        {
            // TODO: 完善网络回滚逻辑
            return default;
        }

        public override void RestoreSnapshot(in AbilitySnapshot state)
        {
            // TODO: 完善网络回滚逻辑
        }

        #endregion
        
        protected override void OnBound(EntityCharacter target)
        {
            _movementModule = Context.Get<MovementModule>();
            _viewModule = Context.Get<ViewModule>();
            
            _config = Config.combatConfig;
            
            Reset();
            InitEffectExecutors();
            InitConditionInterpreters();
            InitSkills();
        }

        public override void Reset()
        {
            if (_executors != null)
            {
                foreach (ISkillEffectExecutor executor in _executors.Values) executor.StopExecution();
            }

            if (_skillInstances != null)
            {
                foreach (SkillInstance instance in _skillInstances.Values) instance.Reset();
            }
        }
    }
}