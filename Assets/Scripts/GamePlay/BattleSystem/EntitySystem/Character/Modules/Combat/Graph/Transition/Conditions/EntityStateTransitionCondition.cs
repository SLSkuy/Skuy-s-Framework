using System;
using UnityEngine;

namespace GamePlay.BattleSystem
{
    [Serializable]
    public sealed class EntityStateTransitionCondition : ITransitionCondition
    {
        [Tooltip("匹配任一状态即可转换；未选择状态时条件不成立")]
        public EntityState[] allowedStates = Array.Empty<EntityState>();
    }
}
