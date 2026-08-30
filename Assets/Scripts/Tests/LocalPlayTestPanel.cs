using System;
using Core;
using Framework;
using GamePlay;
using GamePlay.Battle;
using GamePlay.GameSession;
using GamePlay.Simulator;
using Network;
using UnityEngine;

namespace Tests
{
    /// <summary>
    /// 单机链路调试面板：经战局管理器建房、开战、换关、结束对局。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocalPlayTestPanel : MonoBehaviour
    {
        private BattleManager _battleManager;
        private GUIStyle _titleStyle;
        private GUIStyle _statusStyle;
        private string _lastError;

        private GameManager Gameplay => _battleManager?.ActiveRoom?.GameManager;

        private void StartPlay()
        {
            if (_battleManager == null) return;
            try
            {
                if (!_battleManager.HasActiveRoom && !_battleManager.CreateLocalRoom())
                {
                    _lastError = "无法创建房间。";
                    return;
                }

                BattleRoom room = _battleManager.ActiveRoom;
                if (!room.TryGetPlayerByConnection(BattleManager.LOCAL_CONNECTION_ID, out _) &&
                    !_battleManager.AdmitLocal())
                {
                    _lastError = "本机进房失败。";
                    return;
                }

                _lastError = _battleManager.StartMatch()
                    ? string.Empty
                    : "无法开战：已在对局中、无名册、无 GameCore，或原型不可用。";
            }
            catch (Exception exception)
            {
                _lastError = exception.Message;
                Debug.LogException(exception, this);
            }
        }

        private void EndMatch()
        {
            _battleManager?.EndMatch();
            _lastError = string.Empty;
        }

        private void Dissolve()
        {
            _battleManager?.Dissolve();
            _lastError = string.Empty;
        }

        private void DrawStatus()
        {
            BattleRoom room = _battleManager?.ActiveRoom;
            LocalSimulationHost localHost = room?.SimulationHost ?? Global.Get<LocalSimulationHost>();
            Simulator simulator = localHost?.Simulator;
            NetServer server = Global.Get<NetServer>();
            NetClient client = Global.Get<NetClient>();
            GameManager gameplay = Gameplay;

            bool sessionRunning = localHost != null && localHost.IsSessionRunning;
            GUILayout.Label($"对局：{(room != null && room.HasMatch ? "进行中" : "未开战")}", _statusStyle);
            GUILayout.Label($"关卡相位：{gameplay?.Phase.ToString() ?? "无"}", _statusStyle);
            GUILayout.Label($"房间：{(_battleManager?.HasActiveRoom == true ? "活动" : "无")} 成员：{room?.MemberCount ?? 0}",
                _statusStyle);
            GUILayout.Label($"单机会话：{(sessionRunning ? "运行中" : "未启动")}", _statusStyle);
            GUILayout.Label($"本地实体：{(localHost?.LocalEntityId ?? 0)}");
            GUILayout.Label($"模拟 Tick：{simulator?.CurrentTick ?? 0}");
            GUILayout.Label($"模拟实体：{simulator?.RegisteredEntityCount ?? 0}");
            GUILayout.Label($"服务端：{(server?.IsRunning == true ? "运行中" : "未启动")}");
            GUILayout.Label($"客户端：{(client?.IsRunning == true ? "已连接" : "未连接")}");
            if (!string.IsNullOrEmpty(_lastError)) GUILayout.Label(_lastError);
        }

        #region 生命周期

        private void Start()
        {
            SystemManager systemManager = GameCore.Instance?.SystemMgr;
            if (systemManager == null)
            {
                _lastError = "GameCore 尚未初始化。";
                return;
            }

            _battleManager = systemManager.GetSystem<BattleManager>();
            if (_battleManager == null) _lastError = "GameCore 未注册 BattleManager。";
        }

        private void OnGUI()
        {
            _titleStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            _statusStyle ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };

            BattleRoom room = _battleManager?.ActiveRoom;
            bool hasMatch = room != null && room.HasMatch;
            bool inLevel = Gameplay != null && Gameplay.Phase == GameplayPhase.InLevel;

            GUILayout.BeginArea(new Rect(16f, 340f, 360f, 360f), GUI.skin.box);
            GUILayout.Label("单机模拟测试", _titleStyle);
            GUILayout.Space(8f);

            GUI.enabled = _battleManager != null && !hasMatch;
            if (GUILayout.Button("开始单机", GUILayout.Height(36f))) StartPlay();

            GUI.enabled = hasMatch;
            if (GUILayout.Button("结束对局", GUILayout.Height(30f))) EndMatch();

            GUI.enabled = _battleManager != null && _battleManager.HasActiveRoom;
            if (GUILayout.Button("解散房间", GUILayout.Height(30f))) Dissolve();
            GUI.enabled = true;

            GUILayout.Space(8f);
            DrawStatus();
            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            _battleManager?.Dissolve();
        }

        #endregion
    }
}
