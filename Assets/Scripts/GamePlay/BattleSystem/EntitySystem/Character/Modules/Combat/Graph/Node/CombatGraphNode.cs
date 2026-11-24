using System;
using UnityEngine;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 连招节点
    /// </summary>
    [Serializable]
    public class CombatGraphNode : CombatGraphBaseNode
    {
        [Header("连招节点信息")]
        [SerializeField]private uint skillId;
        [SerializeField]private int priority;
        
        /// <summary>技能实例对象，用于读取技能实例信息</summary>
        private SkillInstance _instance;
        
        #region 属性
        /// <summary>释放技能ID</summary>
        public uint SkillId => skillId;

        /// <summary>当前技能释放进度</summary>
        public float SkillExecutionPercent => _instance?.SkillElapsedPercent ?? 0;
        #endregion

        internal void InitializeSkill(uint id) => skillId = id;

        #region 初始化

        /// <summary>
        /// 绑定创建的技能实例，用于获取释放进度等信息，便于DEBUG节点查看
        /// </summary>
        public void BindSkillInstance(SkillInstance instance)
        {
            _instance = instance;
        }

        #endregion
    }
}
