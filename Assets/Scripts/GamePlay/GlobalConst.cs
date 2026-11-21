using UnityEngine;

namespace GamePlay
{
    /// <summary>
    /// 全局游戏常量
    /// </summary>
    public static class GlobalConst
    {
        #region 场景管理
        public const string MENU_SCENE_NAME = "MainScene";
        public const string LEVEL_SCENE_NAME = "GameScene";
        #endregion
        
        #region 网络联机
        /// <summary>本地玩家连接ID</summary>
        public const uint LOCAL_CONNECTION_ID = 0;
        #endregion

        #region 碰撞盒伤害检测
        public static readonly int EnemyLayerMask = LayerMask.GetMask("Enemy");
        #endregion

#if UNITY_EDITOR
        #region Gizmos数据
        public const float GIZMOS_VISIBLE_TIME = 0.5f;
        #endregion
#endif
    }
}