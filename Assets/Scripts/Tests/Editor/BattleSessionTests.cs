using System.Collections.Generic;
using Events;
using GamePlay.Battle;
using Google.Protobuf;
using NetConnect;
using NetSync;
using Network;
using NUnit.Framework;

namespace Tests.Editor
{
    public sealed class BattleSessionTests
    {
        [Test]
        public void Join_AllocatesPlayerIdDifferentFromClientId()
        {
            ConnectionEventHub hub = new();
            RecordingMatchMessenger messenger = new();
            using BattleSession battle = new(hub, messenger);

            Assert.IsTrue(battle.TryJoin(5, out uint playerId));
            Assert.AreNotEqual(5u, playerId);
            Assert.IsTrue(battle.TryGetPlayer(playerId, out BattlePlayer player));
            Assert.AreEqual(5u, player.ClientId);
            Assert.AreEqual(1, messenger.ReliableSends.Count);
            Assert.AreEqual(NetEvent.GAME_JOIN_RESPONSE, messenger.ReliableSends[0].EventId);
        }

        [Test]
        public void DuplicateJoin_Rejected()
        {
            ConnectionEventHub hub = new();
            RecordingMatchMessenger messenger = new();
            using BattleSession battle = new(hub, messenger);

            Assert.IsTrue(battle.TryJoin(2, out uint firstId));
            Assert.IsFalse(battle.TryJoin(2, out uint secondId));
            Assert.AreEqual(0u, secondId);
            Assert.AreEqual(1, battle.PlayerCount);
            Assert.IsTrue(battle.TryGetPlayer(firstId, out _));
        }

        [Test]
        public void RemovedConnection_LeavesJoinedPlayer_Only()
        {
            ConnectionEventHub hub = new();
            RecordingMatchMessenger messenger = new();
            using BattleSession battle = new(hub, messenger);
            Assert.IsTrue(battle.TryJoin(8, out uint playerId));

            hub.NotifyRemoved(99);
            Assert.AreEqual(1, battle.PlayerCount);
            Assert.IsTrue(battle.TryGetPlayer(playerId, out _));

            hub.NotifyRemoved(8);
            Assert.AreEqual(0, battle.PlayerCount);
            Assert.IsFalse(battle.TryGetPlayer(playerId, out _));
        }

        [Test]
        public void Join_DoesNotBroadcastSnapshot_OrSpawn()
        {
            ConnectionEventHub hub = new();
            RecordingMatchMessenger messenger = new();
            using BattleSession battle = new(hub, messenger);
            Assert.IsTrue(battle.TryJoin(1, out _));
            Assert.AreEqual(0, battle.WorldSnapshotBroadcastCount);
            Assert.IsFalse(messenger.HasEvent(NetEvent.WORLD_SNAPSHOT));
        }

        [Test]
        public void ConnectionHub_NotifiesRemoved()
        {
            ConnectionEventHub hub = new();
            uint observed = 0;
            IConnectionEvents events = hub;
            events.ClientRemoved += id => observed = id;
            hub.NotifyRemoved(7);
            Assert.AreEqual(7u, observed);
        }

        [Test]
        public void GameJoinHandler_DoesNotTouchNetServer()
        {
            ConnectionEventHub hub = new();
            RecordingMatchMessenger messenger = new();
            using BattleSession battle = new(hub, messenger);
            MessageProcessor processor = new();
            processor.RegisterServer(NetEvent.GAME_JOIN_REQUEST, new GameJoinRequestHandler(battle));

            processor.HandleServerMessage(11, NetEvent.GAME_JOIN_REQUEST, new Game_Join_Request { ClientId = 11 });

            Assert.IsTrue(battle.TryGetPlayerByClient(11, out BattlePlayer player));
            Assert.AreNotEqual(11u, player.PlayerId);
        }

        [Test]
        public void Heartbeat_DoesNotChangeRoster()
        {
            ConnectionEventHub hub = new();
            RecordingMatchMessenger messenger = new();
            using BattleSession battle = new(hub, messenger);
            Assert.IsTrue(battle.TryJoin(4, out _));
            int count = battle.PlayerCount;

            MessageProcessor processor = new();
            processor.RegisterServer(NetEvent.HEART_BEAT_REQUEST, new NoopHeartBeatHandler());
            processor.HandleServerMessage(4, NetEvent.HEART_BEAT_REQUEST, new Heart_Beat_Request());

            Assert.AreEqual(count, battle.PlayerCount);
        }

        private sealed class NoopHeartBeatHandler : IServerNetHandler<Heart_Beat_Request>
        {
            public void Handle(uint senderId, Heart_Beat_Request message)
            {
            }
        }

        private sealed class RecordingMatchMessenger : IMatchMessenger
        {
            public readonly List<(uint ClientId, NetEvent EventId, IMessage Message)> ReliableSends = new();
            public readonly List<NetEvent> Broadcasts = new();

            public void SendReliable(uint clientId, NetEvent evt, IMessage message)
            {
                ReliableSends.Add((clientId, evt, message));
            }

            public void BroadcastReliable(NetEvent evt, IMessage message)
            {
                Broadcasts.Add(evt);
            }

            public bool HasEvent(NetEvent evt)
            {
                for (int i = 0; i < ReliableSends.Count; i++)
                {
                    if (ReliableSends[i].EventId == evt) return true;
                }

                return Broadcasts.Contains(evt);
            }
        }
    }
}
