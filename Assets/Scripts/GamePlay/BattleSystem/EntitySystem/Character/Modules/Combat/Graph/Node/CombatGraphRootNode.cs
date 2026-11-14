using System;
using System.Collections.Generic;
using Framework;
using UnityEngine;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 连招根节点
    /// </summary>
    [Serializable]
    public class CombatGraphRootNode : ICombatGraphNode
    {
        [Header("根节点信息")]
        [SerializeField]private uint nodeId;
        
        /// <summary>与当前节点连接的转换节点</summary>
        private readonly Dictionary<EntityCommandFlags, ICombatGraphNode> _nextNodes = new();
        
        #region 属性
        public uint NodeId => nodeId;
        #endregion
        
        #region 可视化编辑入口
        
        public void AddTransition(EntityCommandFlags command, ICombatGraphNode node)
        {
            _nextNodes.Add(command, node);
        }

        public void RemoveTransition(EntityCommandFlags command)
        {
            _nextNodes.Remove(command);
        }

        #endregion

        #region 节点查询

        public bool CheckTransition(EntityCommandFlags command, out CombatGraphNode targetNode)
        {
            if (_nextNodes.TryGetValue(command, out ICombatGraphNode node))
            {
                // 防止连招节点连接到根节点
                if (node is CombatGraphNode combatGraphNode)
                {
                    targetNode = combatGraphNode;
                    return true;    
                }
            }

            targetNode = null;
            return false;
        }

        #endregion
    }
}