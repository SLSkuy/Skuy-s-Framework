using System.Collections.Generic;
using NetSync;
using UnityEngine;
using Utils;

namespace GamePlay.NetSync
{
    /// <summary>
    /// 本地客户端模拟，发送本地玩家输入，同步远程玩家模拟状态
    /// </summary>
    public class ClientSimulator
    {
        // ========== 玩家控制器 ==========
        private readonly Dictionary<uint, NetPlayerController> _remotePlayers;
        private LocalPlayerController _localPlayer;
        // ========== 玩家控制器 ==========

        /// <summary>
        /// 注册本地玩家
        /// </summary>
        /// <param name="player"></param>
        public void RegisterLocalPlayer(LocalPlayerController player)
        {
            _localPlayer = player;
        }

        /// <summary>
        /// 注册远程玩家
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="player"></param>
        public void RegisterRemotePlayer(uint clientId, NetPlayerController player)
        {
            if (clientId == 0 || player == null) return;

            if (!_remotePlayers.TryAdd(clientId, player))
            {
                Debug.LogWarning("Client " + clientId + " has already been registered.");
            }
        }

        public void UnregisterRemotePlayer(uint clientId)
        {
            _remotePlayers.Remove(clientId);
        }

        #region 快照控制

        /// <summary>
        /// 给本地模拟的远程玩家应用快照
        /// </summary>
        /// <param name="snapshot"></param>
        public void ApplySnapshot(Player_Snapshot snapshot)
        {
            if (snapshot == null) return;

            if (!_remotePlayers.TryGetValue(snapshot.CliendId, out NetPlayerController player)) return;
            
            player.Entity.SetSnapshot(NetSyncUtils.ToPlayerSnapshot(snapshot));
        }

        #endregion
    }
}