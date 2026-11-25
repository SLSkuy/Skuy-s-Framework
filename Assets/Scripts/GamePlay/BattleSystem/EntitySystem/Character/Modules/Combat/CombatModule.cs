using System;
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
        
        /// <summary> 条件解释器，按条件配置的实际类型注册 </summary>
        private Dictionary<Type, ITransitionConditionInterpreter> _conditionInterpreters;

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
        public void ResolveCommand(in EntityControlCommand command)
        {
            // 按照优先级处理按键
            // 处理技能间的派生
            foreach (InputCommandFlags input in _config.commandPriorities)
            {
                InputCommandState state = command.GetCommandState(input);
                if(state == InputCommandState.None) continue;   // 没有对应输入命令
                
                // 全局节点入口处理
                CombatGraphAnyNode anyNode = Graph.anyNode;
                TransitionCheckResult checkTransition = anyNode.CheckTransition(input, state);
                if (!checkTransition.IsSuccess)
                {
                    // TODO: 处理某一条转换能够执行但是目标技能不能释放后，自动流转到其他的转换边，直到所有转换及目标技能都不能执行
                    checkTransition = CurrentNode.CheckTransition(input, state);
                }

                // 当前节点仍然没有可以执行的节点转换，直接退出
                if (!checkTransition.IsSuccess) continue;

                // 理论上一定能进入这条判断，转换都是进入普通连招节点
                if (checkTransition.TargetNode is CombatGraphNode targetNode)
                {
                    // 检测技能释放条件
                    uint skillId = targetNode.SkillId;
                    SkillInterruptReason reason = SkillInterruptReason.Replace;
                    if (input is InputCommandFlags.Run or InputCommandFlags.Roll)
                    {
                        // 闪避打断
                        reason = SkillInterruptReason.Dodge;
                    }
                    
                    SkillExecutionCheckResult checkCast = _ability.CheckCast(skillId, reason);
                    if (!checkCast.IsSuccess) continue;
                    
                    // 释放技能
                    _ability.CastSkill(skillId, reason);

                    // 若非技能派生，则需要进入Action状态
                    if (checkCast.Code == SkillExecutionCheckCode.Success) _stateMachine.ChangeState(EntityState.ACTION);
                    CurrentNode = targetNode;  
                    break;
                }
            }

            // 技能状态的打断处理
            if (!_ability.IsCasting) return;

            // 处理跳跃输入的打断操作
            if (command.GetCommandState(InputCommandFlags.Jump) == InputCommandState.Pressed)
            {
                if (_ability.CheckInterrupt(SkillInterruptReason.Jump))
                {
                    _ability.InterruptSkill(SkillInterruptReason.Jump);
                    return;
                }
            }
            
            // 处理移动输入的打断操作
            InputCommandState locomotionState = command.GetCommandState(InputCommandFlags.Locomotion);
            if (locomotionState is InputCommandState.Pressed or InputCommandState.Held)
            {
                if (_ability.CheckInterrupt(SkillInterruptReason.Locomotion))
                {
                    _ability.InterruptSkill(SkillInterruptReason.Locomotion);
                    return;
                }
            }
        }

        private void InitCombatGraph()
        {
            // TODO: 初始化连招图节点信息
            Graph = _config.CreateRuntimeCombatGraph();

            foreach (CombatGraphNode node in Graph.nodes)
            {
                SkillInstance instance = _ability.RegisterSkill(node.SkillId);
                node.BindSkillInstance(instance);     
                node.OnTransitionCheck += OnTransitionCheck;
            }   
            
            Graph.rootNode.OnTransitionCheck += OnTransitionCheck;
            Graph.anyNode.OnTransitionCheck += OnTransitionCheck;
        }

        /// <summary>
        /// 初始化条件执行器
        /// </summary>
        private void InitConditionInterpreters()
        {
            _conditionInterpreters = new Dictionary<Type, ITransitionConditionInterpreter>
            {
                [typeof(EntityStateTransitionCondition)] = new EntityStateTransitionConditionInterpreter(_stateMachine)
            };
        }
        
        #region 事件回调

        private bool OnTransitionCheck(ITransitionCondition[] conditions)
        {
            foreach (ITransitionCondition condition in conditions)
            {
                if (!_conditionInterpreters[condition.GetType()].Evaluate(condition))
                {
                    // 某一个条件不满足，节点不能进行该转换
                    return false;
                }
            }
            
            // 所有条件满足。可以进行转换
            return true;
        }

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
            
            InitCombatGraph();
            InitConditionInterpreters();

            Reset();
        }

        protected override void OnUnbound(EntityCharacter oldTarget)
        {
            DisposeGraph();
            
            _ability.OnSkillEnded -= OnSkillEnded;
        }
        
        public override void Reset()
        {
            CurrentNode = Graph.rootNode;
        }

        private void DisposeGraph()
        {
            CurrentNode = null;
            Graph?.Dispose();
        }
        
        #endregion
    }
}
