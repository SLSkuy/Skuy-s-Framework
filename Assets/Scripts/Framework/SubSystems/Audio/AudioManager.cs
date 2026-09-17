using UnityEngine;

namespace Framework.SubSystems
{
    /// <summary>
    /// 全局音频管理器，提供音频控制功能
    /// </summary>
    public class AudioManager : SubSystemBase
    {
        private Transform _container;

        #region 属性
        public override int Priority => (int)SubSystemPriority.AudioManager;
        #endregion

        #region 音量管理

        /// <summary>
        /// 调整主音量大小
        /// </summary>
        public void ChangeMasterVolume(float volume)
        {
            
        }

        /// <summary>
        /// 调整音乐大小
        /// </summary>
        public void ChangeMusicVolume(float volume)
        {
            
        }

        /// <summary>
        /// 调整音效大小
        /// </summary>
        public void ChangeSfxVolume(float volume)
        {
            
        }

        #endregion

        #region 生命周期

        public override void Init()
        {
            _container = new GameObject("[AudioManager]").transform;
            Object.DontDestroyOnLoad(_container);
        }

        #endregion
    }
}