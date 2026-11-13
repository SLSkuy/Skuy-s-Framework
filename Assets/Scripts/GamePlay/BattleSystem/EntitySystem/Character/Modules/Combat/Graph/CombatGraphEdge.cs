using System;
using Framework;

namespace GamePlay.BattleSystem
{
    /// <summary>从当前阶段按单个战斗命令派生至目标阶段。</summary>
    [Serializable]
    public struct CombatGraphEdge
    {
        public uint sourceNodeId;
        public uint targetNodeId;
        public EntityCommandFlags command;
    }
}