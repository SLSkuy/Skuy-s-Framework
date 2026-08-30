using Framework;
using Network;

namespace GamePlay.Battle
{
    /// <summary>
    /// 战局管理器：房间表、连接进房，以及开战/结束对局。
    /// </summary>
    public sealed class BattleManager : SubSystemBase
    {
        public const uint LOCAL_CONNECTION_ID = 1000;

        private BattleServerHandler _serverHandler;
        private NetServer _netServer;
        private uint _nextRoomId = 1;

        #region 属性
        public override int Priority => 150;
        public BattleRoom ActiveRoom { get; private set; }
        public bool HasActiveRoom => ActiveRoom != null;
        #endregion

        /// <summary>
        /// 创建本机可玩房间。已有活动房间时失败。
        /// </summary>
        public bool CreateLocalRoom()
        {
            if (HasActiveRoom) return false;
            ActiveRoom = new BattleRoom(_nextRoomId++);
            return true;
        }

        /// <summary>
        /// 将连接加入活动房间。
        /// </summary>
        public bool Admit(uint connectionId, out uint playerId)
        {
            playerId = 0;
            if (!HasActiveRoom) return false;
            return ActiveRoom.TryAdmit(connectionId, out playerId);
        }

        /// <summary>
        /// 连接离开。未加入则为无操作。
        /// </summary>
        public void Leave(uint connectionId)
        {
            if (!HasActiveRoom) return;

            bool wasHost = false;
            if (ActiveRoom.TryGetPlayerByConnection(connectionId, out BattlePlayer player))
            {
                wasHost = player.PlayerId == ActiveRoom.HostPlayerId;
            }

            ActiveRoom.Leave(connectionId);
            if (wasHost)
            {
                Dissolve();
            }
        }

        /// <summary>
        /// 开战。须已有活动房间且已有成员。
        /// </summary>
        public bool StartMatch()
        {
            return ActiveRoom != null && ActiveRoom.StartMatch();
        }

        /// <summary>
        /// 结束对局，房间名册保留。
        /// </summary>
        public void EndMatch()
        {
            ActiveRoom?.EndMatch();
        }

        /// <summary>
        /// 解散活动房间。
        /// </summary>
        public void Dissolve()
        {
            if (!HasActiveRoom) return;
            ActiveRoom.EndMatch();
            ActiveRoom.ClearMembers();
            ActiveRoom = null;
        }

        private void HandleClientRemoved(uint connectionId)
        {
            Leave(connectionId);
        }

        #region 子系统生命周期

        public override void Init()
        {
            _serverHandler = new BattleServerHandler(this);
        }

        public override void BindEvents()
        {
            _netServer = Global.Get<NetServer>();
            _netServer.OnClientRemoved += HandleClientRemoved;
            _serverHandler.Bind();
        }

        public override void Destroy()
        {
            _serverHandler.Unbind();
            if (_netServer != null)
            {
                _netServer.OnClientRemoved -= HandleClientRemoved;
                _netServer = null;
            }

            Dissolve();
        }

        #endregion
    }
}
