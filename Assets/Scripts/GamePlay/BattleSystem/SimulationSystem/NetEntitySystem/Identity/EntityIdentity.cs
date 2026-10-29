using System;
using Framework;
using UnityEngine;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 网络同步对象身份识别器，决定如何进行模拟
    /// </summary>
    [DisallowMultipleComponent]
    public class EntityIdentity : MonoBehaviour, IPoolable
    {
        [SerializeField] private uint entityId;
        [SerializeField] private EntityRole role = EntityRole.AI;
        [SerializeField] private EntityType entityType = EntityType.None;

        #region 属性
        public uint EntityId => entityId;
        public bool IsInitialized => entityId != 0;
        
        public EntityRole Role => role;
        public EntityType EntityType => entityType;
        public bool IsAuthority => role == EntityRole.Authority;
        public bool IsPredict => role == EntityRole.Predict;
        public bool IsReplica => role == EntityRole.Replica;
        public bool IsLocalPlay => role == EntityRole.LocalPlay;
        public bool IsAI => role == EntityRole.AI;
        #endregion

        /// <summary>
        /// 分配实体ID
        /// </summary>
        public void BindEntityID(uint newEntityId)
        {
            if (newEntityId == 0) throw new ArgumentOutOfRangeException(nameof(newEntityId), "NetworkObjectId 不能为 0。");
            if (entityId != 0 && entityId != newEntityId)
            {
                throw new InvalidOperationException($"网络对象 {name} 已绑定 ID {entityId}，不能在激活时改为 {newEntityId}。");
            }

            entityId = newEntityId;
        }

        public void BindEntityType(EntityType newEntityType)
        {
            entityType = newEntityType;
        }

        /// <summary>
        /// 分配实体角色
        /// </summary>
        /// <param name="newRole"></param>
        public void BindEntityRole(EntityRole newRole = EntityRole.AI)
        {
            role = newRole;
        }

        /// <summary>
        /// 还池时解开本次实体号，下一次借出可以重新绑定。
        /// </summary>
        public virtual void Reset()
        {
            entityId = 0;
            role = EntityRole.AI;
            entityType = EntityType.None;
        }
    }
}
