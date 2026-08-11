namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 技能入口模块。
    /// </summary>
    public class SkillModule : EntityModuleBase, IEntitySkillEntry
    {
        #region 属性
        public IEntityControlTarget Owner => Target;
        #endregion

        #region 技能入口
        /// <summary>
        /// 绑定技能所属实体。
        /// </summary>
        public override void Bind(IEntityControlTarget owner)
        {
            base.Bind(owner);
        }

        /// <summary>
        /// 请求释放技能。
        /// </summary>
        public bool TryCast(uint skillId)
        {
            return IsEnabled && Owner != null;
        }

        /// <summary>
        /// 取消当前技能。
        /// </summary>
        public void Cancel()
        {
        }
        #endregion
    }
}
