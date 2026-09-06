using Framework;
using GamePlay.Battle;
using NUnit.Framework;

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
    }
}
