using Framework;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 管理连招图和当前 Tick 的输入裁决，将合法派生交给技能系统执行
    /// </summary>
    public class CombatModule : EntityModuleBase
    {
        private EntityStateMachineModule _stateMachine;
        private AbilityModule _ability;
        private CombatConfig _config;

        #region 连招图管理
        /// <summary>连招图实例</summary>
        public CombatGraph Graph { get; private set; }
        /// <summary>当前连招节点</summary>
        public ICombatGraphNode CurrentNode { get; private set; }
        
        /// <summary>当前是否处于根节点，即没有进行任何连招</summary>
        public bool IsRootNode => CurrentNode == Graph.rootNode;
        #endregion

        #region 属性
        public override ModuleType Type => ModuleType.Combat;
        #endregion

        /// <summary>
        /// 仅尝试本 Tick 按下的最高优先级命令，不保留或重试输入
        /// </summary>
        public void CheckAction(in EntityControlCommand command)
        {
            // 按照优先级处理按键
            // TODO: 按照动作优先级处理输入命令
            foreach (EntityCommandFlags input in _config.commandPriorities)
            {
                // TODO: 添加按下/按住/松开的逻辑处理
                if (!command.IsPressed(input)) continue;
                
                // 不存在对应派生操作，退出检测
                if (!CurrentNode.CheckTransition(input, out CombatGraphNode targetNode)) return;
                
                // 判断是否为派生释放
                bool replacing = _ability.IsCasting;
                
                // 释放技能
                uint skillId = targetNode.SkillId;
                SkillExecutionCheckResult checkResult = _ability.TryCastSkill(skillId);
                if (!checkResult.IsSuccess) return;

                CurrentNode = targetNode;  // 若非替换，则需要进入Action状态
                if (!replacing) _stateMachine.ChangeState(EntityState.ACTION);
                return;
            }
        }
        
        #region 事件回调

        private void OnSkillEnded(SkillEndResult result)
        {
            if (result.Code != SkillEndCode.Replaced) Reset();
        }

        #endregion

        #region 生命周期
        
        protected override void OnBound(EntityCharacter target)
        {
            _stateMachine = Context.Get<EntityStateMachineModule>();
            _ability = Context.Get<AbilityModule>();
            
            _config = Config.combatConfig;
            _ability.OnSkillEnded += OnSkillEnded;

            Reset();
        }

        protected override void OnUnbound(EntityCharacter oldTarget)
        {
            Reset();
            
            _ability.OnSkillEnded -= OnSkillEnded;
        }
        
        public override void Reset()
        {
            CurrentNode = _config.combatGraph.rootNode;
        }
        
        #endregion
    }
}