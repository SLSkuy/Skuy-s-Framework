using System;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 连招阶段；根节点不关联技能，其余节点引用已注册的技能
    /// </summary>
    [Serializable]
    public struct CombatGraphNode
    {
        public uint nodeId;
        public uint skillId;
    }
}