using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 动画同步模块入口，负责捕获和接收动画状态。
    /// </summary>
    public class NetAnimationSync : MonoBehaviour, INetSyncComponent,
        INetSyncSnapSource<NetAnimationSnapshot>,
        INetSyncSnapshotReceiver<NetAnimationSnapshot>
    {
        private NetEntityIdentity _identity;
        private Animator _animator;
        private IEntityControlTarget _target;

        #region 属性
        public SyncModuleID ModuleId => SyncModuleID.Animation;
        public NetAnimationSnapshot CurrentSnapshot { get; private set; }
        #endregion

        #region 同步接口
        /// <summary>
        /// 根据网络角色配置动画同步行为。
        /// </summary>
        public void ConfigureRole(NetEntityRole role)
        {
        }

        /// <summary>
        /// 捕获动画快照。
        /// </summary>
        public NetAnimationSnapshot CaptureSnapshot(uint snapshotTick, uint lastProcessedInputTick = 0)
        {
            AnimatorStateInfo stateInfo = _animator ? _animator.GetCurrentAnimatorStateInfo(0) : default;
            return new NetAnimationSnapshot
            {
                EntityId = _identity ? _identity.EntityId : 0,
                SnapshotTick = snapshotTick,
                StateHash = stateInfo.shortNameHash,
                NormalizedTime = stateInfo.normalizedTime,
                LocomotionSpeed = _target?.LocomotionSpeed ?? 0f
            };
        }

        /// <summary>
        /// 应用动画快照。
        /// </summary>
        public void ApplySnapshot(in NetAnimationSnapshot snapshot)
        {
            CurrentSnapshot = snapshot;
        }
        #endregion

        #region 生命周期
        private void Awake()
        {
            _identity = GetComponent<NetEntityIdentity>();
            _animator = GetComponentInChildren<Animator>();
            _target = GetComponent<BaseEntity>();
        }
        #endregion
    }
}
