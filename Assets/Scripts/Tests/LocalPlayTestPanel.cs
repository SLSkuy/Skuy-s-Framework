using System;
using Core;
using Framework;
using GamePlay.MultiPlaySystem;
using GamePlay.Simulator;
using Network;
using UnityEngine;

namespace Tests
{
    /// <summary>
    /// 单机链路调试面板：开始/停止本地会话，生成角色并绑定输入与相机。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocalPlayTestPanel : MonoBehaviour
    {
        private LocalSimulationHost _localHost;
        private MultiPlayManager _multiPlayManager;
        private GUIStyle _titleStyle;
        private GUIStyle _statusStyle;
        private string _lastError;

        private void StartSession()
        {
            if (_localHost == null) return;
            try
            {
                _lastError = _localHost.StartSession() ? string.Empty : "无法启动单机：联机端点正在运行，或原型不可用。";
            }
            catch (Exception exception)
            {
                _lastError = exception.Message;
                Debug.LogException(exception, this);
            }
        }

        private void StopSession()
        {
            _localHost?.StopSession();
            _lastError = string.Empty;
        }

        private void DrawStatus()
        {
            Simulator simulator = _localHost?.Simulator;
            CharacterReplicationSystem replication = Global.Get<CharacterReplicationSystem>();
            NetServer server = Global.Get<NetServer>();
            NetClient client = Global.Get<NetClient>();

            bool sessionRunning = _localHost != null && _localHost.IsSessionRunning;
            GUILayout.Label($"单机会话：{(sessionRunning ? "运行中" : "未启动")}", _statusStyle);
            GUILayout.Label($"本地实体：{(_localHost?.LocalEntityId ?? 0)}");
            GUILayout.Label($"模拟 Tick：{simulator?.CurrentTick ?? 0}");
            GUILayout.Label($"模拟实体：{simulator?.RegisteredEntityCount ?? 0}");
            GUILayout.Label($"复制实体：{replication?.RegisteredEntityCount ?? 0}");
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

            _localHost = systemManager.GetSystem<LocalSimulationHost>() ??
                systemManager.RegisterSystem<LocalSimulationHost>();
            _multiPlayManager = systemManager.GetSystem<MultiPlayManager>();
        }

        private void OnGUI()
        {
            _titleStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            _statusStyle ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };

            bool onlineRunning = _multiPlayManager != null && _multiPlayManager.IsRunning;
            bool sessionRunning = _localHost != null && _localHost.IsSessionRunning;

            GUILayout.BeginArea(new Rect(16f, 340f, 360f, 280f), GUI.skin.box);
            GUILayout.Label("单机模拟测试", _titleStyle);
            GUILayout.Space(8f);

            GUI.enabled = !onlineRunning && !sessionRunning;
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
            _localHost?.StopSession();
        }

        #endregion
    }
}
