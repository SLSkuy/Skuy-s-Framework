using Framework;
using GamePlay;
using GamePlay.Simulator;
using Network;
using NUnit.Framework;

namespace Tests.Editor
{
    public sealed class GameManagerTests
    {
        private SystemManager _systemManager;

        [SetUp]
        public void SetUp()
        {
            _systemManager = new SystemManager();
            _systemManager._Init();
        }

        [TearDown]
        public void TearDown()
        {
            _systemManager._Destroy();
            Global.Clear();
        }

        [Test]
        public void DefaultPhase_IsIdle_AndDistinctFromGameState()
        {
            GameManager gameManager = _systemManager.RegisterSystem<GameManager>();
            Assert.AreEqual(GameplaySessionPhase.Idle, gameManager.Phase);
            Assert.AreNotEqual((int)GameState.MainMenu, (int)GameplaySessionPhase.Idle);
        }

        [Test]
        public void StartLocal_WithoutGameCore_DoesNotStartNetwork()
        {
            GameManager gameManager = _systemManager.RegisterSystem<GameManager>();
            Assert.IsFalse(gameManager.StartLocal());
            Assert.AreEqual(GameplaySessionPhase.Idle, gameManager.Phase);
            Assert.IsNull(Global.Get<NetServer>());
            Assert.IsNull(Global.Get<NetClient>());
            LocalSimulationHost host = _systemManager.GetSystem<LocalSimulationHost>();
            if (host != null)
            {
                Assert.IsFalse(host.IsSessionRunning);
            }
        }

        [Test]
        public void PauseGameState_DoesNotStopGameplaySession()
        {
            GameManager gameManager = _systemManager.RegisterSystem<GameManager>();
            GameStateManager stateManager = _systemManager.RegisterSystem<GameStateManager>();
            stateManager.ChangeState(GameState.GamePaused);
            Assert.AreEqual(GameplaySessionPhase.Idle, gameManager.Phase);
            Assert.IsFalse(gameManager.IsInPlay);
        }
    }
}
