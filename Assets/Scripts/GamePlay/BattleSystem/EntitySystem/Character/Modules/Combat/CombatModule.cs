using System.Collections.Generic;
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
        /// <summary>连招节点</summary>
        private readonly Dictionary<uint, CombatGraphNode> _nodes = new();
        /// <summary>连招节点转换路径</summary>
        private readonly Dictionary<(uint, EntityCommandFlags), uint> _transitions = new();
        /// <summary>当前节点</summary>
        private uint _currentNodeId;
        #endregion

        #region 属性
        public override ModuleType Type => ModuleType.Combat;
        #endregion

        /// <summary>
        /// 仅尝试本 Tick 按下的最高优先级命令，不保留或重试输入
        /// TODO: 添加按下/按住/松开的逻辑处理
        /// </summary>
        public void CheckAction(in EntityControlCommand command)
        {
            // 按照优先级处理按键
            // TODO: 按照动作优先级处理输入命令
            foreach (EntityCommandFlags input in _config.commandPriorities)
            {
                if (!command.IsPressed(input)) continue;
                if (!_transitions.TryGetValue((_currentNodeId, input), out uint targetNodeId)) return;

                // 获取下一个节点信息
                uint skillId = _nodes[targetNodeId].skillId;
                bool replacing = _ability.IsCasting;
                
                // 释放技能
                SkillExecutionCheckResult checkResult = _ability.TryCastSkill(skillId);
                if (!checkResult.IsSuccess) return;

                _currentNodeId = targetNodeId;  // 若非替换，则需要进入Action状态
                if (!replacing) _stateMachine.ChangeState(EntityState.ACTION);
                return;
            }
        }

        private void BuildGraph()
        {
            _nodes.Clear();
            _transitions.Clear();
            
            CombatGraph graph = _config.combatGraph;
            foreach (CombatGraphNode node in graph.nodes)
            {
                _nodes.Add(node.nodeId, node);
            }

            foreach (CombatGraphEdge edge in graph.edges)
            {
                _transitions.Add(new(edge.sourceNodeId, edge.command), edge.targetNodeId);
            }
            
            Reset();
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
            
            BuildGraph();
            Reset();
        }

        protected override void OnUnbound(EntityCharacter oldTarget)
        {
            Reset();
            
            _ability.OnSkillEnded -= OnSkillEnded;
        }
        
        public override void Reset()
        {
            _currentNodeId = _config.combatGraph.rootNodeId;
        }
        
        #endregion
    }
}