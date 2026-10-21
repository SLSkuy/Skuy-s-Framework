using System;
using UnityEngine;

namespace GamePlay.GameData
{
    /// <summary>
    /// 玩家实体数据信息，独立于PlayerInfoData
    /// 只存在于游戏存档中，记录玩家实体的各种数据（位置，旋转，血量等）
    /// 通过UUID索引PlayerInfoData
    /// </summary>
    public class PlayerEntityData
    {
        /// <summary>
        /// 实体唯一ID，与PlayerInfo.UUID对应
        /// </summary>
        public Guid uuid;

        public Vector3 position;
        
        public Quaternion rotation;
    }
}