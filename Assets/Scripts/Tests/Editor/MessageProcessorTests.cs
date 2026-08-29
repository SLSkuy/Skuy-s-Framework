using Events;
using NetConnect;
using Network;
using NUnit.Framework;

namespace Tests.Editor
{
    public sealed class MessageProcessorTests
    {
        [Test]
        public void TwoHandlers_BothReceive_SecondDoesNotReplaceFirst()
        {
            MessageProcessor processor = new();
            CountingHandler first = new();
            CountingHandler second = new();
            processor.Register(NetEvent.CHAT_TEST, first);
            processor.Register(NetEvent.CHAT_TEST, second);

            processor.HandleMessage(NetEvent.CHAT_TEST, new Chat_Test { Content = "a" });

            Assert.AreEqual(1, first.Count);
            Assert.AreEqual(1, second.Count);
        }

        [Test]
        public void Unregister_RemovesOnlyThatInstance()
        {
            MessageProcessor processor = new();
            CountingHandler first = new();
            CountingHandler second = new();
            processor.Register(NetEvent.CHAT_TEST, first);
            processor.Register(NetEvent.CHAT_TEST, second);
            processor.Unregister(first);

            processor.HandleMessage(NetEvent.CHAT_TEST, new Chat_Test { Content = "b" });

            Assert.AreEqual(0, first.Count);
            Assert.AreEqual(1, second.Count);
        }

        [Test]
        public void Unregister_MissingInstance_IsNoop()
        {
            MessageProcessor processor = new();
            CountingHandler registered = new();
            CountingHandler missing = new();
            processor.Register(NetEvent.CHAT_TEST, registered);

            Assert.DoesNotThrow(() => processor.Unregister(missing));
            processor.HandleMessage(NetEvent.CHAT_TEST, new Chat_Test { Content = "c" });
            Assert.AreEqual(1, registered.Count);
        }

        private sealed class CountingHandler : INetHandler<Chat_Test>
        {
            public int Count;

            public void Handle(Chat_Test message)
            {
                Count++;
            }
        }
    }
}
