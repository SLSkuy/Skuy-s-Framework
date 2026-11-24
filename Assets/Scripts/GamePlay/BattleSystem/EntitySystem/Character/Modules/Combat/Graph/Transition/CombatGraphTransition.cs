using System;
using Framework;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 连招图节点转换条件
    /// </summary>
    [Serializable]
    public class CombatGraphTransition
    {
        /// <summary>转换目标节点</summary>
        public ICombatGraphNode targetNode;
        
        /// <summary>转换目标输入命令标志</summary>
        public InputCommandFlags commandFlag;
        
        /// <summary>转换目标输入命令状态</summary>
        public InputCommandState commandState;
        
        /// <summary>单个节点转换所需满足条件</summary>
        public ITransitionCondition[] conditions;
    }
}
