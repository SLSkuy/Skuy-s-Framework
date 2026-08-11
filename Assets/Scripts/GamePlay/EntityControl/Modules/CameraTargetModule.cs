using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 相机目标能力模块入口。
    /// </summary>
    public class CameraTargetModule : EntityModuleBase
    {
        [SerializeField] private Transform target;

        #region 属性
        public Transform TargetTransform => target;
        #endregion
    }
}

