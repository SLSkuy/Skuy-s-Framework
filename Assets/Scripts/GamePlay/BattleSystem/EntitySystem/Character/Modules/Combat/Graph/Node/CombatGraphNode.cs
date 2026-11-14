using System;
using System.Collections.Generic;
using Framework;
using UnityEngine;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 连招节点
    /// </summary>
    [Serializable]
    public class CombatGraphNode : ICombatGraphNode
    {
        [Header("节点信息")]
        [SerializeField]private uint nodeId;
        [SerializeField]private uint skillId;
        
        /// <summary>与当前节点连接的转换节点</summary>
        private readonly Dictionary<EntityCommandFlags, ICombatGraphNode> _nextNodes = new();
        
        /// <summary>技能实例对象，用于读取技能实例信息</summary>
        private SkillInstance _instance;
        
        #region 属性
        public uint NodeId => nodeId;
        /// <summary>释放技能ID</summary>
        public uint SkillId => skillId;

        /// <summary>当前技能释放进度</summary>
        public float SkillExecutionPercent => _instance?.SkillElapsedPercent ?? 0;
        #endregion

        #region 初始化

        /// <summary>
        /// 绑定创建的技能实例，用于获取释放进度等信息，便于DEBUG节点查看
        /// </summary>
        public void BindInstance(SkillInstance instance)
        {
            _instance = instance;
        }

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