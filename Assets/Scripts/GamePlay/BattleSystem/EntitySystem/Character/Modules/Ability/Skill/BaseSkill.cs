using System;
using UnityEngine;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 基础技能基类，定义技能通用数据结构
    /// </summary>
    [Serializable]
    public class BaseSkill
    {
        // TODO: 创建技能能力组件，由组合的形式配置各个技能
        public uint skillId;
        public float duration;
        public AnimationClip clip;
    }
}