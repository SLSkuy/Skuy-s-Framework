using System;
using Core;
using Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamePlay.LevelControl
{
    /// <summary>
    /// 对局里负责何时加载与切换关卡。订阅 SceneLoader 的 IO 回调，不接管场景 IO。
    /// </summary>
    public class LevelManager : SubSystemBase
    {
        private SceneLoader _sceneLoader;

        #region 属性
        public override int Priority => 180;

        /// <summary>
        /// 对局正在请求的关卡场景。尚未请求时为空。
        /// </summary>
        public string RequestedLevelScene { get; private set; }

        /// <summary>
        /// 最近一次 Completed 的关卡场景。切关进行中仍是上一张已就绪的图。
        /// </summary>
        public string ActiveLevelScene { get; private set; }

        /// <summary>
        /// 已发出切关请求、尚未 Completed 或 Failed。
        /// </summary>
        public bool IsChanging { get; private set; }

        /// <summary>
        /// 请求过的关卡已经 Completed。菜单场景完成不会把它打成 true。
        /// </summary>
        public bool IsReady { get; private set; }
        #endregion

        #region 事件
        public event Action<string> Completed;
        public event Action<string, string> Failed;
        #endregion

        /// <summary>
        /// 请求加载关卡场景。同一张图的重复请求忽略；已在目标场景则立刻就绪。
        /// </summary>
        public void LoadLevel(string sceneName)
        {
            if (IsChanging && RequestedLevelScene == sceneName) return;
            if (IsReady && ActiveLevelScene == sceneName) return;

            if (IsChanging)
            {
                Debug.LogWarning($"[LevelManager] 正在加载 '{RequestedLevelScene}'，忽略 '{sceneName}'");
                return;
            }

            RequestedLevelScene = sceneName;
            IsChanging = true;
            IsReady = false;

            if (SceneManager.GetActiveScene().name == sceneName)
            {
                OnLoadCompleted(sceneName);
                return;
            }

            Global.LoadScene(sceneName);
        }

        /// <summary>
        /// 加载对局默认关卡。
        /// </summary>
        public void LoadMatchLevel()
        {
            LoadLevel(GlobalConstants.LEVEL_SCENE_NAME);
        }

        #region 子系统生命周期

        public override void BindEvents()
        {
            _sceneLoader = Global.Get<SceneLoader>();
            _sceneLoader.Completed += HandleSceneLoadCompleted;
            _sceneLoader.Failed += HandleSceneLoadFailed;
        }

        public override void Destroy()
        {
            _sceneLoader.Completed -= HandleSceneLoadCompleted;
            _sceneLoader.Failed -= HandleSceneLoadFailed;
            _sceneLoader = null;
        }

        #endregion

        private void HandleSceneLoadCompleted(string sceneName)
        {
            if (!IsChanging || sceneName != RequestedLevelScene)
            {
                return;
            }

            OnLoadCompleted(sceneName);
        }

        private void HandleSceneLoadFailed(string sceneName, string errorMessage)
        {
            if (!IsChanging || sceneName != RequestedLevelScene)
            {
                return;
            }

            OnLoadFailed(sceneName, errorMessage);
        }

        private void OnLoadCompleted(string sceneName)
        {
            IsChanging = false;
            IsReady = true;
            ActiveLevelScene = sceneName;
            Completed?.Invoke(sceneName);
        }

        private void OnLoadFailed(string sceneName, string errorMessage)
        {
            IsChanging = false;
            IsReady = false;
            Failed?.Invoke(sceneName, errorMessage);
        }
    }
}
