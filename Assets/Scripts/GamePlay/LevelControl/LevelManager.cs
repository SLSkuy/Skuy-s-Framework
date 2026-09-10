using Core;
using Framework;
using UnityEngine.SceneManagement;

namespace GamePlay.LevelControl
{
    /// <summary>
    /// 对局里负责加载与切换关卡。不编排玩法，不管名册和连接。
    /// </summary>
    public class LevelManager : SubSystemBase
    {
        #region 属性
        public override int Priority => 180;
        public string ActiveLevelScene { get; private set; }
        #endregion

        /// <summary>
        /// 请求加载关卡场景。
        /// </summary>
        public void LoadLevel(string sceneName)
        {
            ActiveLevelScene = sceneName;
            if (SceneManager.GetActiveScene().name == sceneName)
            {
                return;
            }

            Global.LoadScene(sceneName);
        }

        /// <summary>
        /// 加载对局默认关卡。
        /// </summary>
        public void LoadMatchLevel()
        {
            LoadLevel(GameConstants.LEVEL_SCENE_NAME);
        }
    }
}
