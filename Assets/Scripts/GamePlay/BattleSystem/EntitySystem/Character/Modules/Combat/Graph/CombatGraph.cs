using System;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 只读使用的有向连招图，允许循环及多个节点复用同一技能
    /// </summary>
    [Serializable]
    public class CombatGraph
    {
        /// <summary>
        /// 初始节点，不存储技能信息，提供初步的转换入口
        /// </summary>
        public CombatGraphRootNode rootNode;
        
        /// <summary>
        /// 连招节点
        /// </summary>
        public CombatGraphNode[] nodes = Array.Empty<CombatGraphNode>();
    }
}