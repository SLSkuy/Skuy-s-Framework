using System;
using UnityEngine;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 当前角色状态必须匹配任一允许状态；空列表不允许释放
    /// </summary>
    [Serializable]
    public sealed class EntityStateCondition : ICondition
    {
        [Tooltip("不允许释放技能的角色状态，匹配任意一个即可；空列表表示任意状态都可以释放")]
        public EntityState[] exclusiveStates = Array.Empty<EntityState>();
    }
}