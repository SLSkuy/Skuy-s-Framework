using System;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 实体角色属性配置
    /// </summary>
    [Serializable]
    public class AttributeConfig
    {
        /// <summary>生命值</summary>
        public float hp;
        
        /// <summary>法力值</summary>
        public float mp;
        
        /// <summary>防御力</summary>
        public float def;
        
        /// <summary>攻击力</summary>
        public float atk;
    }
}