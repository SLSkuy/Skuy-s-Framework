using Framework;
using GamePlay.Battle;
using GamePlay.Simulator;
using NetSync;
using Network;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    /// <summary>
    /// 战局管理器会话 API：本机建房、开多人房、入座拒绝、开战提交。
    /// </summary>
    public sealed class BattleManagerSessionTests
    {
        private SystemManager _systems;

        [SetUp]
        public void SetUp()
        {
            Global.Clear();
            _systems = new SystemManager();
            _systems._Init();
        }

        [TearDown]
        public void TearDown()
        {
            if (Global.TryGet(out BattleManager battle))
            {
                Global.Unregister<BattleManager>();
            }

            _systems?.Destroy();
            Global.Clear();
        }

        [Test]
        public void CreateLocalRoom_SeatsOnlyLocalPlayerAndRejectsRemote()
        {
            BattleManager battle = Global.Register<BattleManager>();

            battle.CreateLocalRoom();

            Assert.IsNotNull(battle.ActiveRoom);
            Assert.AreEqual(1, battle.ActiveRoom.Capacity);
            Assert.IsFalse(battle.ActiveRoom.AcceptsRemoteJoin);
            Assert.AreEqual(1, battle.ActiveRoom.MemberCount);
            Assert.AreNotEqual(0u, battle.ActiveRoom.HostPlayerId);
            Assert.IsTrue(battle.ActiveRoom.ContainsPlayer(battle.ActiveRoom.HostPlayerId));
            Assert.IsTrue(battle.IsMatchSubmitted);

            Assert.IsFalse(battle.Admit(2));
            Assert.AreEqual(1, battle.ActiveRoom.MemberCount);
        }

        [Test]
        public void CreateHostRoom_SeatsLocalPlayerOpensListenAndSubmitsMatch()
        {
            BattleManager battle = Global.Register<BattleManager>();

            battle.CreateHostRoom();

            Assert.IsNotNull(battle.ActiveRoom);
            Assert.AreEqual(4, battle.ActiveRoom.Capacity);
            Assert.IsTrue(battle.ActiveRoom.AcceptsRemoteJoin);
            Assert.AreEqual(1, battle.ActiveRoom.MemberCount);
            Assert.IsTrue(battle.IsMatchSubmitted);
            Assert.IsTrue(battle.IsListening);
        }

        [Test]
        public void HandleGameJoinRequest_AssignsHostPlayerIdNotReportedClientId()
        {
            BattleManager battle = Global.Register<BattleManager>();
            battle.CreateHostRoom();
            uint hostPlayerId = battle.ActiveRoom.HostPlayerId;
            const uint connectionId = 7;
            const uint reportedClientId = 999;

            Game_Join_Response response = battle.HandleGameJoinRequest(
                connectionId,
                new Game_Join_Request { ClientId = reportedClientId });

            Assert.IsTrue(response.Accepted);
            Assert.AreNotEqual(0u, response.PlayerId);
            Assert.AreNotEqual(reportedClientId, response.PlayerId);
            Assert.AreEqual(hostPlayerId, response.HostPlayerId);
            Assert.IsTrue(response.InMatch);
            Assert.AreEqual(2, response.PlayerIds.Count);
            CollectionAssert.Contains(response.PlayerIds, hostPlayerId);
            CollectionAssert.Contains(response.PlayerIds, response.PlayerId);
            Assert.AreEqual(2, battle.ActiveRoom.MemberCount);
            Assert.IsTrue(battle.ActiveRoom.ContainsPlayer(response.PlayerId));
            Assert.IsFalse(battle.ActiveRoom.ContainsPlayer(reportedClientId));
            Assert.IsTrue(battle.IsMatchSubmitted);
        }

        [Test]
        public void HandleGameJoinResponse_AcceptedCreatesRoomWithMatchingRoster()
        {
            BattleManager joiner = Global.Register<BattleManager>();
            Assert.IsNull(joiner.ActiveRoom);

            Game_Join_Response response = new()
            {
                Accepted = true,
                PlayerId = 2,
                HostPlayerId = 1,
                InMatch = true,
            };
            response.PlayerIds.Add(1);
            response.PlayerIds.Add(2);

            joiner.HandleGameJoinResponse(response);

            Assert.IsNotNull(joiner.ActiveRoom);
            Assert.AreEqual(BattleSessionRole.Client, joiner.ActiveRoom.SessionRole);
            Assert.AreEqual(1u, joiner.ActiveRoom.HostPlayerId);
            Assert.AreEqual(2, joiner.ActiveRoom.MemberCount);
            Assert.IsTrue(joiner.ActiveRoom.ContainsPlayer(1));
            Assert.IsTrue(joiner.ActiveRoom.ContainsPlayer(2));
            Assert.IsTrue(joiner.IsMatchSubmitted);
        }

        [Test]
        public void HandleGameJoinResponse_RejectedLeavesNoRoom()
        {
            BattleManager joiner = Global.Register<BattleManager>();

            joiner.HandleGameJoinResponse(new Game_Join_Response { Accepted = false });

            Assert.IsNull(joiner.ActiveRoom);
            Assert.IsFalse(joiner.IsMatchSubmitted);
        }

        [Test]
        public void JoinRemoteRoom_HasNoRoomUntilAccepted_AndFailedJoinStopsClient()
        {
            BattleManager battle = Global.Register<BattleManager>();
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("TCP connect failed"));
            battle.JoinRemoteRoom();

            Assert.IsNull(battle.ActiveRoom);
            Assert.IsTrue(Global.TryGet(out NetClient _));

            battle.HandleJoinFailed();

            Assert.IsNull(battle.ActiveRoom);
            Assert.IsFalse(Global.TryGet(out NetClient _));
        }

        [Test]
        public void HandleGameJoinRequest_WithoutRoom_IsRejected()
        {
            BattleManager battle = Global.Register<BattleManager>();

            Game_Join_Response response = battle.HandleGameJoinRequest(3, new Game_Join_Request { ClientId = 8 });

            Assert.IsFalse(response.Accepted);
            Assert.IsNull(battle.ActiveRoom);
        }

        [Test]
        public void AcceptedJoin_StartBattleDoesNotSpawnPawn()
        {
            BattleManager battle = Global.Register<BattleManager>();
            Game_Join_Response response = new()
            {
                Accepted = true,
                PlayerId = 2,
                HostPlayerId = 1,
                InMatch = true,
            };
            response.PlayerIds.Add(1);
            response.PlayerIds.Add(2);
            battle.HandleGameJoinResponse(response);

            Assert.IsTrue(battle.StartBattle());
            Assert.AreEqual(0, Object.FindObjectsByType<EntityObjectIdentity>(FindObjectsSortMode.None).Length);
        }
    }
}
