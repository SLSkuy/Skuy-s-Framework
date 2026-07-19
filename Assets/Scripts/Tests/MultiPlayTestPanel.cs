using Core;
using GamePlay.NetSync;
using UnityEngine;

namespace Tests
{
    /// <summary>
    /// Runtime panel for exercising host/client startup, join protocol and player snapshots.
    /// </summary>
    public sealed class MultiPlayTestPanel : MonoBehaviour
    {
        private string _serverAddress = "127.0.0.1";
        private string _lastAction = "Idle";
        private MultiPlayManager _manager;

        private void Start()
        {
            _manager = GameCore.Instance?.MultiPlayMgr;
            if (_manager == null) _lastAction = "MultiPlayManager unavailable";
        }

        private void OnDestroy()
        {
            _manager?.StopAll();
        }

        private void OnGUI()
        {
            const float width = 360f;
            const float height = 330f;
            float scale = Mathf.Clamp(Mathf.Min(Screen.width / 1280f, Screen.height / 720f), 0.8f, 1.4f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            Rect panelRect = new Rect(16f, 16f, width, height);
            GUI.Box(panelRect, GUIContent.none);
            GUILayout.BeginArea(new Rect(panelRect.x + 12f, panelRect.y + 10f, width - 24f, height - 20f));

            GUILayout.Label("Multiplayer Sync Prototype", new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            });
            GUILayout.Space(8f);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Server", GUILayout.Width(58f));
            _serverAddress = GUILayout.TextField(_serverAddress, GUILayout.Height(24f));
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);

            GUILayout.BeginHorizontal();
            GUI.enabled = _manager != null && !_manager.IsHostRunning && !_manager.IsClientRunning;
            if (GUILayout.Button("Create Host", GUILayout.Height(30f)))
            {
                _manager.StartHost(true);
                _lastAction = "Host startup requested";
            }

            if (GUILayout.Button("Create Client", GUILayout.Height(30f)))
            {
                _manager.StartClient();
                _lastAction = $"Client startup requested: {_serverAddress}";
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUI.enabled = _manager != null && _manager.IsClientRunning;
            if (GUILayout.Button("Send Join Protocol", GUILayout.Height(28f)))
            {
                _lastAction = _manager.SendJoinRequest() ? "GAME_JOIN_REQUEST sent" : "Join protocol not ready";
            }
            GUI.enabled = _manager != null && (_manager.IsHostRunning || _manager.IsClientRunning);
            if (GUILayout.Button("Stop", GUILayout.Height(28f)))
            {
                _manager.StopAll();
                _lastAction = "Stopped";
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.Space(10f);
            DrawStatus("Host", _manager?.IsHostRunning == true ? "Running" : "Stopped");
            DrawStatus("Client", _manager?.IsClientRunning == true ? "Running" : "Stopped");
            DrawStatus("Client ID", (_manager?.LocalClientId ?? 0).ToString());
            DrawStatus("Players", $"Authority {_manager?.HostPlayerCount ?? 0} / Replicas {_manager?.ClientPlayerCount ?? 0}");
            DrawStatus("RTT", $"{(_manager?.RTT ?? 0f) * 1000f:0.0} ms");

            GUILayout.FlexibleSpace();
            GUILayout.Label(_lastAction, new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.65f, 0.85f, 0.72f) }
            });
            GUILayout.EndArea();
        }

        private static void DrawStatus(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(82f));
            GUILayout.Label(value, new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            GUILayout.EndHorizontal();
        }
    }
}
