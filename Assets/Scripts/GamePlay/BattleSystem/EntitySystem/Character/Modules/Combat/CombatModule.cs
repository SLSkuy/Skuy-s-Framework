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
        
        #region 属性
        public override ModuleType Type => ModuleType.Combat;
        #endregion

        public void CheckAction(in EntityControlCommand command)
        {
            // 提取战斗输入命令
            EntityCommandFlags flagsHeld = GetCombatCommand(command.flagsHeld);
            EntityCommandFlags flagsPressed = GetCombatCommand(command.flagsPressed);
            EntityCommandFlags flagsReleased = GetCombatCommand(command.flagsReleased);
            
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
        }

        #endregion
    }
}