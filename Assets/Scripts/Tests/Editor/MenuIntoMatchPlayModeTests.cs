using System.Collections;
using Core;
using Framework;
using GamePlay.Battle;
using GamePlay.Procedure;
using GamePlay.Simulator;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    /// <summary>
    /// 菜单开战进关：流程态、切关后开战、关卡无第二套进程壳、解散回菜单。
    /// </summary>
    public sealed class MenuIntoMatchPlayModeTests
    {
        private const string MenuScene = "MainScene";
        private const string LevelScene = "GameScene";

        [UnitySetUp]
        public IEnumerator EnterPlay()
        {
            yield return new EnterPlayMode();
            if (SceneManager.GetActiveScene().name != MenuScene)
            {
                AsyncOperation load = SceneManager.LoadSceneAsync(MenuScene);
                yield return load;
            }

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator ExitPlay()
        {
            if (ProcedureCore.Instance != null &&
                ProcedureCore.Instance.CurrentProcedure != GameProcedure.Menu)
            {
                if (Global.TryGet(out SceneLoader loader))
                {
                    float loadWait = 0f;
                    while (loader.IsLoading && loadWait < 8f)
                    {
                        loadWait += Time.unscaledDeltaTime;
                        yield return null;
                    }
                }

                ProcedureCore.Instance.LeaveSession();
                yield return WaitForScene(MenuScene);
            }

            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator StartLocal_EntersMatchWithoutLobby()
        {
            ProcedureCore.Instance.StartLocal();
            yield return null;

            Assert.AreEqual(GameProcedure.Match, ProcedureCore.Instance.CurrentProcedure);
            Assert.IsTrue(Global.TryGet(out BattleManager battle));
            Assert.IsNotNull(battle.ActiveRoom);
            Assert.AreEqual(1, battle.ActiveRoom.MemberCount);
            Assert.IsTrue(battle.IsMatchSubmitted);
        }

        [UnityTest]
        public IEnumerator HostMultiplayer_EntersMatchWithoutLobby()
        {
            ProcedureCore.Instance.HostMultiplayer();
            yield return null;

            Assert.AreEqual(GameProcedure.Match, ProcedureCore.Instance.CurrentProcedure);
            Assert.IsTrue(Global.TryGet(out BattleManager battle));
            Assert.AreEqual(4, battle.ActiveRoom.Capacity);
            Assert.IsTrue(battle.IsMatchSubmitted);
            Assert.IsTrue(battle.IsListening);

            yield return WaitForScene(LevelScene);
            uint playerId = battle.ActiveRoom.HostPlayerId;
            EntityObjectIdentity[] pawns = Object.FindObjectsByType<EntityObjectIdentity>(FindObjectsSortMode.None);
            Assert.AreEqual(1, pawns.Length);
            Assert.AreEqual(playerId, pawns[0].EntityId);
        }

        [UnityTest]
        public IEnumerator StartLocal_LoadsLevelThenSpawnsOnePawnWithPlayerId()
        {
            ProcedureCore.Instance.StartLocal();
            yield return WaitForScene(LevelScene);

            Assert.AreEqual(1, Object.FindObjectsByType<GameCore>(FindObjectsSortMode.None).Length);

            BattleManager battle = Global.Get<BattleManager>();
            uint playerId = battle.ActiveRoom.HostPlayerId;
            EntityObjectIdentity[] pawns = Object.FindObjectsByType<EntityObjectIdentity>(FindObjectsSortMode.None);
            Assert.AreEqual(1, pawns.Length);
            Assert.AreEqual(playerId, pawns[0].EntityId);
        }

        [UnityTest]
        public IEnumerator LeaveSession_UnloadsLevelAndReturnsToMenu()
        {
            ProcedureCore.Instance.StartLocal();
            yield return WaitForScene(LevelScene);

            ProcedureCore.Instance.LeaveSession();
            yield return WaitForScene(MenuScene);

            Assert.AreEqual(GameProcedure.Menu, ProcedureCore.Instance.CurrentProcedure);
            Assert.IsFalse(Global.TryGet(out BattleManager _));
            Assert.AreEqual(1, Object.FindObjectsByType<GameCore>(FindObjectsSortMode.None).Length);
        }

        private static IEnumerator WaitForScene(string sceneName)
        {
            float elapsed = 0f;
            while (SceneManager.GetActiveScene().name != sceneName && elapsed < 8f)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.AreEqual(sceneName, SceneManager.GetActiveScene().name);
            yield return null;
        }
    }
}
