namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体技能入口接口，用于让技能系统接入实体而不依赖控制器。
    /// </summary>
    public interface IEntitySkillEntry
    {
        /// <summary>
        /// 技能所属实体。
        /// </summary>
        IEntityControlTarget Owner { get; }

        /// <summary>
        /// 绑定技能所属实体。
        /// </summary>
        void Bind(IEntityControlTarget owner);

        /// <summary>
        /// 请求释放技能。
        /// </summary>
        bool TryCast(uint skillId);

        /// <summary>
        /// 取消当前技能。
        /// </summary>
        void Cancel();
    }
}

