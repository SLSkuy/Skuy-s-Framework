using Framework;

namespace GamePlay.Simulator
{
    /// <summary>
    /// 全局模拟入口，收集输入命令或快照，驱动实体
    /// </summary>
    public class Simulator : SubSystemBase
    {
        public override int Priority => 500;
        
        // Tick模块
        private TickSystem _tickSystem;
        
        #region 实体管理

        public void RegisterEntity()
        {
            
        }

        public void UnregisterEntity()
        {
            
        }
        
        #endregion

        #region 生命周期

        public override void Init()
        {
            
        }

        public override void Update(float deltaTime)
        {
            _tickSystem.Update(deltaTime);
        }

        #endregion
    }
}