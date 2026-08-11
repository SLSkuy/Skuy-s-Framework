using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 技能同步模块入口，负责承接技能状态快照。
    /// </summary>
    public class NetSkillSync : MonoBehaviour, INetSyncComponent,
        INetSyncSnapSource<NetSkillSnapshot>,
        INetSyncSnapshotReceiver<NetSkillSnapshot>
    {
        private NetEntityIdentity _identity;

        #region 属性
        public SyncModuleID ModuleId => SyncModuleID.Skill;
        public NetSkillSnapshot CurrentSnapshot { get; private set; }
        #endregion

        #region 同步接口
        /// <summary>
        /// 根据网络角色配置技能同步行为。
        /// </summary>
        public void ConfigureRole(NetEntityRole role)
        {
        }

        /// <summary>
        /// 捕获技能快照。
        /// </summary>
        public NetSkillSnapshot CaptureSnapshot(uint snapshotTick, uint lastProcessedInputTick = 0)
        {
            return new NetSkillSnapshot
            {
                EntityId = _identity ? _identity.EntityId : 0,
                SnapshotTick = snapshotTick
            };
        }

        /// <summary>
        /// 应用技能快照。
        /// </summary>
        public void ApplySnapshot(in NetSkillSnapshot snapshot)
        {
            CurrentSnapshot = snapshot;
        }
        #endregion

        #region 生命周期
        private void Awake()
        {
            _identity = GetComponent<NetEntityIdentity>();
        }
        #endregion
    }
}
