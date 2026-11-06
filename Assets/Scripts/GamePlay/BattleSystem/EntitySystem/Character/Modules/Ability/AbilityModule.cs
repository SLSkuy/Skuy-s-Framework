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
        
        /// <summary> 所有技能效果执行器，按配置实际类型注册 </summary>
        private Dictionary<Type, ISkillEffectExecutor> _executors;
        
        /// <summary> 实体技能示例，创建时进行注册 </summary>
        private Dictionary<uint, SkillInstance> _skillInstances;
        private SkillInstance _currentSkill;

        #region 属性
        public override ModuleType ModuleType => ModuleType.Ability;

        /// <summary> 是否正在释放技能 </summary>
        public bool IsCasting => _currentSkill is { IsCasting : true };
        #endregion

        /// <summary>
        /// 单主动技能释放；校验全部效果后创建实例并派发零时刻效果
        /// </summary>
        public bool TryCastSkill(uint skillId)
        {
            // 不存在技能配置
            if (!_skillInstances.TryGetValue(skillId, out SkillInstance instance))
            {
                // TODO: 添加日志处理类，自定义DEBUG信息
                return false;
            }

            if (!instance.TryCastSkill()) return false;
            
            _currentSkill = instance;
            return true;
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
        private void InitExecutors()
        {
            _executors = new Dictionary<Type, ISkillEffectExecutor>
            {
                { typeof(AnimationSkillEffect), new AnimationEffectExecutor(Context, _simulation.TickRate) },
                { typeof(MoveSkillEffect), new LocomotionEffectExecutor(Context) }
            };
        }

        /// <summary>
        /// 初始化当前实体能够执行的所有技能
        /// </summary>
        private void InitSkills()
        {
            _skillInstances = new Dictionary<uint, SkillInstance>();
            int tickRate = _simulation.TickRate;
            
            foreach (uint id in Config.skillIds)
            {
                if (_abilityConfig.TryGetSkillConfig(id, out SkillConfig config))
                {
                    SkillInstance instance = new SkillInstance(config, tickRate);
                    instance.OnSkillEffectStart += ExecuteSkillEffect;
                    instance.OnSkillEffectEnd += StopExecuteSkillEffect;
                    _skillInstances.Add(id, instance);
                }
            }
        }
        
        private void ExecuteSkillEffect(SkillEffect effect)
        {
            _executors[effect.GetType()].ExecuteSkillEffect(effect);
        }

        private void StopExecuteSkillEffect(SkillEffect effect)
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
            Reset();
            InitExecutors();
            InitSkills();
            
            _movementModule = Context.Get<MovementModule>();
            _viewModule = Context.Get<ViewModule>();
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