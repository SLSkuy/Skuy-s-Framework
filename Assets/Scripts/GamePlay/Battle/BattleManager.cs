using Framework;

namespace GamePlay.Battle
{
    /// <summary>
    /// 战局管理器：房间表、连接进房，以及开战/结束对局。
    /// </summary>
    public sealed class BattleManager : SubSystemBase
    {
        public const uint LOCAL_CONNECTION_ID = 1000;

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
        /// 本机假连接进房。
        /// </summary>
        public bool AdmitLocal(out uint playerId)
        {
            return Admit(LOCAL_CONNECTION_ID, out playerId);
        }

        /// <summary>
        /// 连接离开。未加入则为无操作。
        /// </summary>
        public void Leave(uint connectionId)
        {
            ActiveRoom?.Leave(connectionId);
        }

        /// <summary>
        /// 本机假连接离开。
        /// </summary>
        public void LeaveLocal()
        {
            Leave(LOCAL_CONNECTION_ID);
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

        #region 子系统生命周期

        public override void Destroy()
        {
            Dissolve();
        }

        #endregion
    }
}
