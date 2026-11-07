using System;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 实体属性组件，记录实体属性（攻击/防御/生命等），提供伤害/治疗等接口
    /// </summary>
    public class AttributeModule : EntitySnapshotModuleBase<AttributeSnapshot>
    {
        #region 属性
        public override ModuleType Type => ModuleType.Heath;
        #endregion

        #region 事件
        public event Action OnDeath;
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

        public override void Reset()
        {
            
        }
    }
}