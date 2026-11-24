using System;
using System.Collections.Generic;
using Framework;
using UnityEngine;

namespace GamePlay.BattleSystem
{
    /// <summary>保存节点 ID 和按配置顺序绑定的出边。</summary>
    [Serializable]
    public abstract class CombatGraphBaseNode : ICombatGraphNode
    {
        [Header("基础节点信息")]
        [SerializeField] private uint nodeId;

        private List<CombatGraphTransition> _transitions = new();

        #region 属性
        public uint NodeId => nodeId;
        
        // /// <summary>当前节点的所有转换条件边</summary>
        // public IReadOnlyList<CombatGraphTransition> Transitions => _transitions;
        #endregion

        #region 事件
        /// <summary>由 CombatModule 解析连招转换条件</summary>
        public event Func<ITransitionCondition[], bool> OnTransitionCheck;
        #endregion
        
        public void AddTransition(CombatGraphTransition transition)
        {
            if (transition.targetNode is not CombatGraphNode)
            {
                throw new ArgumentException("连招转换的目标必须是技能节点", nameof(transition));
            }

            _transitions.Add(transition);
        }
        
        public TransitionCheckResult CheckTransition(InputCommandFlags commandFlag, InputCommandState commandState)
        {
            foreach (CombatGraphTransition transition in _transitions)
            {
                // 输入命令不匹配，跳过
                if (transition.commandFlag != commandFlag || transition.commandState != commandState) continue;
                
                if (OnTransitionCheck == null) throw new InvalidOperationException("技能条件检测必须绑定 CombatModule");
                if (OnTransitionCheck(transition.conditions))
                {
                    // 当前Transition的所有条件满足，返回当前Transition
                    return new TransitionCheckResult(TransitionCheckCode.Success, transition.targetNode);
                }
            }
            
            // 所有的转换条件都不满足
            return new TransitionCheckResult(TransitionCheckCode.ConditionFailed);
        }

        public void Dispose()
        {
            _transitions.Clear();
            _transitions = null;
            
            OnTransitionCheck = null;
        }
    }
}
