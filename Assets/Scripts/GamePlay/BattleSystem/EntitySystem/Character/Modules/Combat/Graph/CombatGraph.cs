using System;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 只读使用的有向连招图，允许循环及多个节点复用同一技能
    /// </summary>
    [Serializable]
    public class CombatGraph : IDisposable
    {
        /// <summary>
        /// 初始节点，不存储技能信息，提供初步的转换入口
        /// </summary>
        public CombatGraphRootNode rootNode;
        
        /// <summary>
        /// 任意如何节点，该节点下的转换可以在任意节点后检测
        /// </summary>
        public CombatGraphAnyNode anyNode;
        
        /// <summary>
        /// 连招节点
        /// </summary>
        public CombatGraphNode[] nodes = Array.Empty<CombatGraphNode>();

        public void Dispose()
        {
            foreach (var node in nodes) node.Dispose();
            
            rootNode.Dispose();
            anyNode.Dispose();
        }
    }
}