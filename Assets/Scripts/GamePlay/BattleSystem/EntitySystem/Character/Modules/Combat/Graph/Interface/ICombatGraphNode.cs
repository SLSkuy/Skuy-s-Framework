using System;
using Framework;

namespace GamePlay.BattleSystem
{
    public interface ICombatGraphNode : IDisposable
    {
        /// <summary>
        /// 连招节点ID
        /// </summary>
        uint NodeId { get; }

        event Func<ITransitionCondition[], bool> OnTransitionCheck; 

        /// <summary>
        /// 添加节点转换
        /// </summary>
        void AddTransition(CombatGraphTransition transition);
        
        /// <summary>
        /// 检测当前输入条件按照优先级检测是否有满足条件的连招转换，返回满足条件的连招节点
        /// </summary>
        TransitionCheckResult CheckTransition(InputCommandFlags commandFlag, InputCommandState commandState);
    }
}