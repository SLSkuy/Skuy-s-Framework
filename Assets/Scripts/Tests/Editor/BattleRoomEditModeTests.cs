using Framework;
using GamePlay.Battle;
using GamePlay.EntitySystem;
using GamePlay.Simulator;
using Network;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Editor
{
    public sealed class BattleRoomEditModeTests
    {
        [TearDown]
        public void TearDown()
        {
            Global.Clear();
        }

        [Test]
        public void Admit_AllocatesPlayerIdIndependentOfConnection()
        {
            BattleRoom room = new(1, 8);
            Assert.IsTrue(room.TryAdmit(1000, out uint playerId));
            Assert.AreNotEqual(1000u, playerId);
            Assert.IsFalse(room.TryAdmit(1000, out _));
        }

        [Test]
        public void BattleManager_RejectsSecondActiveRoom_AndAdmitDoesNotSpawn()
        {
            BattleManager battle = CreateBattleManager();
            Assert.IsTrue(battle.CreateLocalRoom());
            Assert.IsFalse(battle.CreateLocalRoom());
            Assert.IsTrue(battle.AdmitLocal(out uint playerId));
            Assert.AreNotEqual(BattleManager.LOCAL_CONNECTION_ID, playerId);
            Assert.AreEqual(0, Object.FindObjectsByType<EntityCharacter>(FindObjectsSortMode.None).Length);
            Assert.IsFalse(battle.ActiveRoom.HasMatch);
        }

        [Test]
        public void Leave_RemovesMember_UnjoinedLeaveDoesNotChangeRoster()
        {
            BattleManager battle = CreateBattleManager();
            Assert.IsTrue(battle.CreateLocalRoom());
            Assert.IsTrue(battle.AdmitLocal(out _));
            int before = battle.ActiveRoom.MemberCount;
            battle.Leave(999);
            Assert.AreEqual(before, battle.ActiveRoom.MemberCount);
            battle.LeaveLocal();
            Assert.AreEqual(0, battle.ActiveRoom.MemberCount);
            Assert.IsFalse(battle.ActiveRoom.TryGetPlayerByConnection(BattleManager.LOCAL_CONNECTION_ID, out _));
        }

        [Test]
        public void StartMatch_WithoutGameCore_DoesNotCreateGameplayOrStartNet()
        {
            SystemManager systems = new();
            systems._Init();
            BattleManager battle = systems.RegisterSystem<BattleManager>();
            Assert.IsTrue(battle.CreateLocalRoom());
            Assert.IsTrue(battle.AdmitLocal(out _));
            Assert.IsFalse(battle.StartMatch());
            Assert.IsTrue(battle.HasActiveRoom);
            Assert.IsFalse(battle.ActiveRoom.HasMatch);
            Assert.IsNull(Global.Get<LocalSimulationHost>());
            Assert.IsFalse(Global.Get<NetServer>()?.IsRunning == true);
            Assert.IsFalse(Global.Get<NetClient>()?.IsRunning == true);
        }

        [Test]
        public void StartMatch_WithoutMembers_Fails()
        {
            BattleManager battle = CreateBattleManager();
            Assert.IsTrue(battle.CreateLocalRoom());
            Assert.IsFalse(battle.StartMatch());
            Assert.IsFalse(battle.ActiveRoom.HasMatch);
        }

        [Test]
        public void EndMatch_KeepsRoomRoster()
        {
            SystemManager systems = new();
            systems._Init();
            BattleManager battle = systems.RegisterSystem<BattleManager>();
            Assert.IsTrue(battle.CreateLocalRoom());
            Assert.IsTrue(battle.AdmitLocal(out _));
            int members = battle.ActiveRoom.MemberCount;
            battle.EndMatch();
            Assert.IsTrue(battle.HasActiveRoom);
            Assert.AreEqual(members, battle.ActiveRoom.MemberCount);
            Assert.IsFalse(battle.ActiveRoom.HasMatch);
        }

        private static BattleManager CreateBattleManager()
        {
            BattleManager battle = new();
            battle._Init();
            return battle;
        }
    }
}
