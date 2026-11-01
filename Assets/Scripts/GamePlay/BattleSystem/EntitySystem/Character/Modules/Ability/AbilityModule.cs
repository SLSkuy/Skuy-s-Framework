namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 角色能力组件，控制角色的各种战斗能力
    /// </summary>
    public class AbilityModule : EntityModuleBase
    {
        #region 属性
        public override ModuleType ModuleType => ModuleType.Ability;
        #endregion
    }
}