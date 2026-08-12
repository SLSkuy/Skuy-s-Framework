using System;
using Core;
using Framework;
using Network;
using UnityEngine;

namespace GamePlay.NetSync
{
    /// <summary>
    /// SyncTest 场景的服务端权威同步测试面板。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SyncTestPanel : MonoBehaviour
    {
        private MultiPlayManager _multiPlayManager;
        private GUIStyle _titleStyle;
        private GUIStyle _statusStyle;
        private string _lastError;

        #region 属性
        public MultiPlayMode Mode => _multiPlayManager?.Mode ?? MultiPlayMode.None;
        #endregion

        private void StartEndpoint(MultiPlayMode mode)
        {
            if (_multiPlayManager == null) return;

            try
            {
                bool started = mode == MultiPlayMode.Server ?
                    _multiPlayManager.StartServer() : _multiPlayManager.StartClient();
                _lastError = started ? string.Empty : "请先停止当前端点，再启动另一端。";
            }
            catch (Exception exception)
            {
                _lastError = exception.Message;
                Debug.LogException(exception, this);
            }
        }

        private void DrawStatus()
        {
            NetServer server = Global.Get<NetServer>();
            NetClient client = Global.Get<NetClient>();
            EntityReplicationSystem replication = Global.Get<EntityReplicationSystem>();

            GUILayout.Label($"模式：{Mode}", _statusStyle);
            GUILayout.Label($"服务端：{(server?.IsRunning == true ? "运行中" : "未启动")}");
            GUILayout.Label($"客户端：{(client?.IsRunning == true ? "已连接" : "未连接")}");
            GUILayout.Label($"ClientId：{client?.ClientId ?? 0}");
            GUILayout.Label($"同步实体：{replication?.RegisteredEntityCount ?? 0}");
            GUILayout.Label($"网络 Tick：{replication?.CurrentTick ?? 0}");
            if (client?.IsRunning == true) GUILayout.Label($"KCP RTT：{client.RTT * 1000f:F0} ms");
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

            _multiPlayManager = systemManager.GetSystem<MultiPlayManager>() ??
                systemManager.RegisterSystem<MultiPlayManager>();
        }

        private void OnGUI()
        {
            _titleStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            _statusStyle ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };

            GUILayout.BeginArea(new Rect(16f, 16f, 360f, 310f), GUI.skin.box);
            GUILayout.Label("服务端权威同步测试", _titleStyle);
            GUILayout.Space(8f);

            GUI.enabled = Mode == MultiPlayMode.None;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("开启服务端", GUILayout.Height(36f))) StartEndpoint(MultiPlayMode.Server);
            if (GUILayout.Button("开启客户端", GUILayout.Height(36f))) StartEndpoint(MultiPlayMode.Client);
            GUILayout.EndHorizontal();

            GUI.enabled = Mode != MultiPlayMode.None;
            if (GUILayout.Button("停止", GUILayout.Height(30f))) _multiPlayManager?.Stop();
            GUI.enabled = true;

            GUILayout.Space(8f);
            DrawStatus();
            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            _multiPlayManager?.Stop();
        }

        #endregion
    }
}
