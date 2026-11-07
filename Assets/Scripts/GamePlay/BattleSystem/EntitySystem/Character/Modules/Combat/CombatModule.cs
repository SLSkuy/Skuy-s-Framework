using Framework;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 实体战斗模块
    /// 处理连招系统与输入映射，将操作命令映射到最匹配的技能ID上
    /// </summary>
    public class CombatModule : EntityModuleBase
    {
        private EntityStateMachineModule _stateMachine;
        private AbilityModule _ability;
        private CombatConfig _config;
        
        #region 属性
        public override ModuleType Type => ModuleType.Combat;
        #endregion

        public void CheckAction(in EntityControlCommand command)
        {
            // 提取战斗输入命令，先只管单次点击的技能，不处理长按式技能
            // EntityCommandFlags flagsHeld = GetCombatCommand(command.flagsHeld);
            EntityCommandFlags flagsPressed = GetCombatCommand(command.flagsPressed);
            // EntityCommandFlags flagsReleased = GetCombatCommand(command.flagsReleased);
            
            uint skill2Action = ResolveCombatCommand(flagsPressed);

            if (_ability.TryCastSkill(skill2Action))
            {
                _stateMachine.ChangeState(EntityState.ACTION);
            }
        }

        /// <summary>
        /// 解析当前输入命令，并按照优先级，返回当前输入控制对应的技能ID
        /// </summary>
        private uint ResolveCombatCommand(in EntityCommandFlags flags)
        {
            uint skillId = 0;
            if ((flags & EntityCommandFlags.Roll) != 0) skillId = _config.rollSkillId;

            return skillId;
        }

        /// <summary>
        /// 提取战斗输入命令
        /// </summary>
        private EntityCommandFlags GetCombatCommand(in EntityCommandFlags flags)
        {
            EntityCommandFlags exclusion = EntityCommandFlags.Run | EntityCommandFlags.Jump | EntityCommandFlags.Interact;
            return flags & ~exclusion;
        }
        
        #region 生命周期

        protected override void OnBound(EntityCharacter target)
        {
            _stateMachine = Context.Get<EntityStateMachineModule>();
            _ability = Context.Get<AbilityModule>();

            _config = Config.combatConfig;
        }

        #endregion
    }
}