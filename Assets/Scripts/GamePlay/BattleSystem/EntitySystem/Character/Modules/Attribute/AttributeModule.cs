using System;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 实体属性组件，记录实体属性（攻击/防御/生命等），提供伤害/治疗等接口
    /// </summary>
    public class AttributeModule : EntitySnapshotModuleBase<AttributeSnapshot>
    {
        private float _currentHp;
        private float _currentMp;
        private float _currentDef;
        
        #region 属性
        public override ModuleType Type => ModuleType.Heath;

        /// <summary>最大生命值</summary>
        public float MaxHp => Config.attributeConfig.hp;
        public float HpPercent => _currentHp / MaxHp;
        
        /// <summary>最大法力值</summary>
        public float MaxMp => Config.attributeConfig.mp;
        public float MpPercent => _currentHp / MaxMp;
        #endregion

        #region 事件
        /// <summary>实体死亡事件</summary>
        public event Action OnDeath;
        #endregion

        #region 生命管理

        public void Damage(float damage)
        {
            
        }

        public void Heal(float heal)
        {
            
        }

        #endregion

        #region 快照处理

        public override AttributeSnapshot CaptureSnapshot()
        {
            throw new System.NotImplementedException();
        }

        public override void RestoreSnapshot(in AttributeSnapshot state)
        {
            throw new System.NotImplementedException();
        }

        #endregion

        #region 生命周期

        protected override void OnBound(EntityCharacter target)
        {
            Reset();
        }

        public override void Reset()
        {
            _currentHp = Config.attributeConfig.hp;
            _currentMp = Config.attributeConfig.mp;
            _currentDef = Config.attributeConfig.def;
        }
        
        #endregion
    }
}