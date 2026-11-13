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

        #region 事件
        public event Action<SkillEndResult> OnSkillEnded;
        #endregion

        /// <summary>
        /// 单技能释放；返回状态、冷却和条件检查的结果
        /// </summary>
        public SkillExecutionCheckResult TryCastSkill(uint skillId)
        {
            // 不存在技能配置
            if (!_skillInstances.TryGetValue(skillId, out SkillInstance instance))
            {
                // TODO: 添加日志处理类，自定义DEBUG信息
                return new SkillExecutionCheckResult(skillId, SkillExecutionCheckCode.SkillNotFound);
            }
            
            SkillExecutionCheckResult checkResult = instance.CheckCast();
            if (checkResult.IsSuccess)
            {
                // 目标技能可用，检测是否需要打断当前释放的技能
                if (IsCasting)
                {
                    SkillInterruptReason reason = SkillInterruptReason.Replace;
                    if (!_currentSkill.CheckInterrupt(reason))
                    {
                        // 不能进行转换打断，返回判断结果
                        return new SkillExecutionCheckResult(skillId, SkillExecutionCheckCode.ReplaceNotAllowed);
                    }
                    
                    // 打断当前技能
                    _currentSkill.Interrupt(reason);
                }
                
                // 执行新的技能
                _currentSkill = instance;
                _currentSkill.StartCast();
            }

            // 返回执行结果
            return checkResult;
        }
        
        /// <summary>
        /// 尝试在对应窗口中断技能；Replace 必须通过 TryReplaceSkill 指定目标技能。
        /// </summary>
        public bool TryInterruptSkill(SkillInterruptReason reason)
        {
            if (reason == SkillInterruptReason.Replace) 
                throw new ArgumentException("技能派生必须通过 TryReplaceSkill 提交目标技能", nameof(reason));
            
            return _currentSkill?.TryInterrupt(reason) ?? false;
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
            foreach (uint id in _config.combatGraph.GetAllSkillId())
            {
                if (!_abilityConfig.TryGetSkillConfig(id, out SkillConfig config))
                    throw new InvalidOperationException($"技能注册表引用了不存在的技能 {id}");

                executors.AddRange(config.effects.Select(effect => _executors[effect.GetType()]).Distinct());

                SkillInstance instance = new SkillInstance(config, executors.ToArray(), tickRate);
                instance.OnCastCheck += OnCastCheck;
                instance.OnSkillEffectStart += OnExecuteSkillEffect;
                instance.OnSkillEffectEnd += OnStopSkillEffect;
                instance.OnSkillEnded += OnInstanceEnded;
                _skillInstances.Add(id, instance);
                    
                executors.Clear();
            }
        }

        #region 事件回调
        
        /// <summary>
        /// 检测执行条件
        /// </summary>
        private bool OnCastCheck(ICondition condition)
        {
            return _conditionInterpreters[condition.GetType()].Evaluate(condition);
        }
        
        private void OnExecuteSkillEffect(SkillEffect effect)
        {
            _executors[effect.GetType()].ExecuteSkillEffect(effect);
        }

        private void OnStopSkillEffect(SkillEffect effect)
        {
            _executors[effect.GetType()].StopExecution();
        }

        private void OnInstanceEnded(SkillEndResult result)
        {
            _currentSkill = null;
            OnSkillEnded?.Invoke(result);
        }

        #endregion

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

        #region 生命周期
        
        public void Tick(float deltaTime)
        {
            // 视角旋转
            _viewModule.Look(Context.LastAimInput, deltaTime);
            
            // 模型旋转
            _movementModule.Rotate(Context.LastMoveInput, _viewModule.Yaw, deltaTime);
            
            // 技能效果
            _currentSkill?.Tick(deltaTime);
        }
        
        protected override void OnBound(EntityCharacter target)
        {
            _movementModule = Context.Get<MovementModule>();
            _viewModule = Context.Get<ViewModule>();
            
            _config = Config.combatConfig;
            
            DisposeSkills();
            InitEffectExecutors();
            InitConditionInterpreters();
            InitSkills();
        }

        protected override void OnUnbound(EntityCharacter oldTarget)
        {
            DisposeSkills();
        }
        
        public override void Reset()
        {
            if (_skillInstances != null)
            {
                foreach (SkillInstance instance in _skillInstances.Values) instance.Reset();
            }
            
            _currentSkill = null;
        }
        
        private void DisposeSkills()
        {
            if (_skillInstances == null) return;
            
            foreach (SkillInstance instance in _skillInstances.Values) instance.Dispose();
            
            _skillInstances.Clear();
            _currentSkill = null;
        }
        
        #endregion
    }
}