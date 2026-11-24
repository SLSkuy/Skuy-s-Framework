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
        private AnimationModule _animationModule;
        
        /// <summary> 条件解释器，按条件配置的实际类型注册 </summary>
        private Dictionary<Type, ICastConditionInterpreter> _conditionInterpreters;
        
        /// <summary> 所有技能效果执行器，按配置实际类型注册 </summary>
        private Dictionary<Type, ISkillEffectExecutor> _executors;
        
        /// <summary> 实体技能示例，创建时进行注册 </summary>
        private Dictionary<uint, SkillInstance> _skillInstances = new();
        private SkillInstance _currentSkill;

        #region 属性
        public override ModuleType Type => ModuleType.Ability;

        /// <summary> 是否正在释放技能 </summary>
        public bool IsCasting => _currentSkill is { IsCasting : true };
        #endregion

        #region 事件
        public event Action<SkillEndResult> OnSkillEnded;
        #endregion
        
        #region 技能管理

        /// <summary>
        /// 检测单技能是否能够进行释放
        /// </summary>
        public SkillExecutionCheckResult CheckCast(uint skillId, SkillInterruptReason reason)
        {
            // 不存在技能配置
            if (!_skillInstances.TryGetValue(skillId, out SkillInstance instance))
            {
                // TODO: 添加日志处理类，自定义DEBUG信息
                return new SkillExecutionCheckResult(skillId, SkillExecutionCheckCode.SkillNotFound);
            }
            
            // 检测目标技能状态
            SkillStateCheckResult code = instance.CheckCast();
            if (code != SkillStateCheckResult.Ready)
            {
                return new SkillExecutionCheckResult(skillId, code);
            }
            
            // 目标技能状态通过，检测当前技能打断状态
            if (IsCasting)
            {
                if (!_currentSkill.CheckInterrupt(reason))
                {
                    // 不能进行转换打断，返回判断结果
                    return new SkillExecutionCheckResult(skillId, SkillExecutionCheckCode.ReplaceNotAllowed);
                }
                
                // 技能运行派生
                return new SkillExecutionCheckResult(skillId, SkillExecutionCheckCode.ReplaceAllowed);
            }

            // 技能运行释放（之前没有释放过任何技能）
            return new SkillExecutionCheckResult(skillId, SkillExecutionCheckCode.Success);
        }

        /// <summary>
        /// 强制执行技能
        /// </summary>
        public void CastSkill(uint skillId, SkillInterruptReason reason)
        {
            if (IsCasting)
            {
                _currentSkill.Interrupt(reason);
            }

            _currentSkill = _skillInstances[skillId];
            _currentSkill.StartCast();
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
        /// 注册技能实例，未注册的技能无法进行释放
        /// </summary>
        public SkillInstance RegisterSkill(uint skillId)
        {
            if (!_abilityConfig.TryGetSkillConfig(skillId, out SkillConfig skillConfig))
            {
                throw new InvalidOperationException($"技能注册表引用了不存在的技能 {skillId}");        
            }
                
            // 指向同一个技能实例，防止错误的数据读取
            if (_skillInstances.TryGetValue(skillId, out SkillInstance sharedInstance))
            {
                return sharedInstance;
            }
         
            int tickRate = _simulation.TickRate;
            
            // 记录只有当前技能实例需要的效果执行器，避免多余的技能调度
            List<ISkillEffectExecutor> executors = new List<ISkillEffectExecutor>();
            executors.AddRange(skillConfig.effects.Select(effect => _executors[effect.GetType()]).Distinct());
                
            SkillInstance instance = new SkillInstance(skillConfig, executors.ToArray(), tickRate);
            instance.OnCastCheck += OnCastCheck;
            instance.OnSkillEffectStart += OnExecuteSkillEffect;
            instance.OnSkillEffectEnd += OnStopSkillEffect;
            instance.OnSkillEnded += OnSkillInstanceEnded;
                
            // 管理所有技能实例
            _skillInstances.Add(skillId, instance);
            return instance;
        }

        #endregion
        
        /// <summary>
        /// 初始化所有技能效果执行器
        /// </summary>
        private void InitEffectExecutors()
        {
            _executors = new Dictionary<Type, ISkillEffectExecutor>
            {
                { typeof(AnimationSkillEffect), new AnimationEffectExecutor(Context, _simulation.TickRate) },
                { typeof(MoveSkillEffect), new MoveEffectExecutor(Context) },
                { typeof(RootMotionSkillEffect), new RootMotionEffectExecutor(Context) },
                { typeof(HitBoxSkillEffect), new HitBoxEffectExecutor(Context) },
                { typeof(WeaponCapsuleSkillEffect), new WeaponCapsuleEffectExecutor(Context) },
            };
        }
        
        private void InitConditionInterpreters()
        {
            _conditionInterpreters = new Dictionary<Type, ICastConditionInterpreter>
            {
                { typeof(EntityStateCastCondition), new EntityStateCastConditionInterpreter(Context) }
            };
        }
        
        #region 事件回调
        
        /// <summary>
        /// 检测执行条件
        /// </summary>
        private bool OnCastCheck(ICastCondition castCondition)
        {
            return _conditionInterpreters[castCondition.GetType()].Evaluate(castCondition);
        }
        
        private void OnExecuteSkillEffect(SkillEffect effect)
        {
            _executors[effect.GetType()].ExecuteSkillEffect(effect);
        }

        private void OnStopSkillEffect(SkillEffect effect)
        {
            _executors[effect.GetType()].StopExecution();
        }

        private void OnSkillInstanceEnded(SkillEndResult result)
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

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_executors == null) return;

            ((HitBoxEffectExecutor)_executors[typeof(HitBoxSkillEffect)]).DrawGizmos();
            ((WeaponCapsuleEffectExecutor)_executors[typeof(WeaponCapsuleSkillEffect)]).DrawGizmos();
        }
#endif

        public void Tick(float deltaTime)
        {
            if (_currentSkill != null)
            {
                ControlPolicy policy = _currentSkill.ControlPolicy;
                if ((policy & ControlPolicy.LockViewRotate) == ControlPolicy.None)
                {
                    // 视角旋转
                    _viewModule.Look(Context.LastAimInput, deltaTime);                    
                }
                
                if ((policy & ControlPolicy.LockMeshRotate) == ControlPolicy.None)
                {
                    // 模型旋转
                    _movementModule.Rotate(Context.LastMoveInput, _viewModule.Yaw, deltaTime);
                }
                
                // 动画更新
                _animationModule.Tick(deltaTime);
                
                // 技能效果
                _currentSkill?.Tick(deltaTime);
            }
        }
        
        protected override void OnBound(EntityCharacter target)
        {
            _movementModule = Context.Get<MovementModule>();
            _viewModule = Context.Get<ViewModule>();
            _animationModule = Context.Get<AnimationModule>();
            
            DisposeSkills();
            InitEffectExecutors();
            InitConditionInterpreters();
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
