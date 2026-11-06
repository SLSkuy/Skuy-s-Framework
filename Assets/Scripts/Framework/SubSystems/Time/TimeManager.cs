namespace Framework
{
    /// <summary>
    /// 时间管理器，管理游戏时间的运行
    /// </summary>
    public class TimeManager : SubSystemBase
    {
        #region 属性
        public override int Priority => (int)SubSystemPriority.TimeManager;

        /// <summary> 程序运行时间 </summary>
        public double AppTime { get; private set; }
        #endregion

        #region 子系统生命周期
        
        public override void Update(float deltaTime)
        {
            AppTime += deltaTime;
        }

        #endregion
    }
}