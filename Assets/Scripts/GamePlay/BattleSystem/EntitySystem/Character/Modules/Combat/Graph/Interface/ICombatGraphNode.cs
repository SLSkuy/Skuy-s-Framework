using Framework;

namespace GamePlay.BattleSystem
{
    public interface ICombatGraphNode
    {
        /// <summary>
        /// 连招节点ID
        /// </summary>
        uint NodeId { get; }

        /// <summary>
        /// 添加节点转换
        /// </summary>
        void AddTransition(EntityCommandFlags command, ICombatGraphNode node);
        
        /// <summary>
        /// 移除节点转换路径
        /// </summary>
        void RemoveTransition(EntityCommandFlags command);
        
        /// <summary>
        /// 检测获取下一个连招节点信息
        /// </summary>
        bool CheckTransition(EntityCommandFlags command, out CombatGraphNode skillId);
    }
}