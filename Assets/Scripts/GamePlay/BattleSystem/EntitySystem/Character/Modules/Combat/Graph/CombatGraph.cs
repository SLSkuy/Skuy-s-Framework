using System;
using System.Collections.Generic;
using System.Linq;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 只读使用的有向连招图，允许循环及多个节点复用同一技能
    /// </summary>
    [Serializable]
    public class CombatGraph
    {
        // TODO: 去除（或特殊化）根节点的设计，实现根据初次按键决定对应的派生树
        public uint rootNodeId;
        public CombatGraphNode[] nodes = Array.Empty<CombatGraphNode>();
        public CombatGraphEdge[] edges = Array.Empty<CombatGraphEdge>();

        /// <summary>
        /// 获取技能图中的所有技能ID，便于初始化
        /// </summary>
        public uint[] GetAllSkillId()
        {
            HashSet<uint> set = new HashSet<uint>();
            foreach (CombatGraphNode config in nodes)
            {
                // TODO: 去除（或特殊化）根节点的设计，实现根据初次按键决定对应的派生树
                if(config.nodeId == rootNodeId) continue;
                
                set.Add(config.skillId);
            }

            return set.ToArray();
        }
    }
}