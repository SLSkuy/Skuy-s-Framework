using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// AI 控制器入口，后续承载 AI 意图路由。
    /// </summary>
    [DisallowMultipleComponent]
    public class AIController : EntityControllerBase
    {
        #region 属性
        public override EntityDriveMode DriveMode => EntityDriveMode.AI;
        #endregion
    }
}

