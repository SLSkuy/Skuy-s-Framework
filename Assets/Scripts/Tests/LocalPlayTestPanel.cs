using System;
using Core;
using Framework;
using GamePlay;
using GamePlay.Simulator;
using Network;
using UnityEngine;

namespace Tests
{
    /// <summary>
    /// 单机链路调试面板：经 GameManager 开始/停止本地会话。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocalPlayTestPanel : MonoBehaviour
    {
        private GameManager _gameManager;
        private GUIStyle _titleStyle;
        private GUIStyle _statusStyle;
        private string _lastError;

        private void StartSession()
        {
            if (_gameManager == null) return;
            try
            {
                _lastError = _gameManager.StartLocal() ? string.Empty : "无法启动单机：会话已在进行中，或原型不可用。";
            }
            catch (Exception exception)
            {
                _lastError = exception.Message;
                Debug.LogException(exception, this);
            }
        }

        private void StopSession()
        {
            _gameManager?.Stop();
            _lastError = string.Empty;
        }

        private void DrawStatus()
        {
            LocalSimulationHost localHost = Global.Get<LocalSimulationHost>();
            Simulator simulator = localHost?.Simulator;
            NetServer server = Global.Get<NetServer>();
            NetClient client = Global.Get<NetClient>();

            bool sessionRunning = localHost != null && localHost.IsSessionRunning;
            GUILayout.Label($"玩法相位：{_gameManager?.Phase.ToString() ?? "无"}", _statusStyle);
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

            _gameManager = systemManager.GetSystem<GameManager>() ??
                systemManager.RegisterSystem<GameManager>();
        }

        private void OnGUI()
        {
            _titleStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            _statusStyle ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };

            LocalSimulationHost localHost = Global.Get<LocalSimulationHost>();
            bool sessionRunning = localHost != null && localHost.IsSessionRunning;

            GUILayout.BeginArea(new Rect(16f, 340f, 360f, 280f), GUI.skin.box);
            GUILayout.Label("单机模拟测试", _titleStyle);
            GUILayout.Space(8f);

            GUI.enabled = !sessionRunning;
            if (GUILayout.Button("开始单机", GUILayout.Height(36f))) StartSession();

            GUI.enabled = sessionRunning;
            if (GUILayout.Button("停止单机", GUILayout.Height(30f))) StopSession();
            GUI.enabled = true;

            GUILayout.Space(8f);
            DrawStatus();
            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            _gameManager?.Stop();
        }

        #endregion
    }
}
