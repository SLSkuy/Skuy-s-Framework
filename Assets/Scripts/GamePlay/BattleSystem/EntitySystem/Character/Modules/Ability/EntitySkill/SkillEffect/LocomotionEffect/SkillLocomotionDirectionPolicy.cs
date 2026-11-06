namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 位移技能效果目标方向策略
    /// </summary>
    public enum SkillLocomotionDirectionPolicy
    {
        /// <summary>
        /// 无指定朝向移动，无输入方向时不会进行移动
        /// </summary>
        None,
        
        /// <summary>
        /// 当前模型朝向
        /// </summary>
        MeshForward,
        
        /// <summary>
        /// 当前摄像机朝向
        /// </summary>
        CameraForward,
    }
}