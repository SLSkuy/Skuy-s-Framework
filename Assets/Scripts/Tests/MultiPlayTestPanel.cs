using Core;
using Framework;
using GamePlay.NetSync;
using Network;
using UnityEngine;

namespace Tests
{
    public sealed class MultiPlayTestPanel : MonoBehaviour
    {
        [SerializeField] private string _serverAddress = "127.0.0.1";
        [SerializeField] private string _lastAction = "Idle";

        private MultiPlayManager _manager;
        private Rect _windowRect = new(16f, 16f, 330f, 285f);

        private void Start()
        {
            TryResolveManager();
        }

        private void Update()
        {
            if (_manager == null) TryResolveManager();
        }

        private void TryResolveManager()
        {
            SystemManager systems = GameCore.Instance?.SystemMgr;
            if (systems == null)
            {
                return;
            }

            _manager = systems.GetSystem<MultiPlayManager>() ??
                       systems.RegisterSystem<MultiPlayManager>();
            _lastAction = "Ready";
        }

        private void OnDestroy()
        {
            _manager?.Stop();
        }

        private void OnGUI()
        {
            _windowRect = GUI.Window(
                GetInstanceID(),
                _windowRect,
                DrawWindow,
                "Multiplayer Sync Test");
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.Label($"Mode: {_manager?.Mode.ToString() ?? "Unavailable"}");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Server", GUILayout.Width(54f));
            _serverAddress = GUILayout.TextField(_serverAddress);
            GUILayout.EndHorizontal();

            GUI.enabled = _manager != null && _manager.Mode == MultiPlayMode.None;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Start Server", GUILayout.Height(30f))) StartServer();
            if (GUILayout.Button("Start Client", GUILayout.Height(30f))) StartClient();
            GUILayout.EndHorizontal();

            GUI.enabled = _manager != null && _manager.Mode != MultiPlayMode.None;
            if (GUILayout.Button("Stop", GUILayout.Height(26f)))
            {
                _manager.Stop();
                _lastAction = "Stopped";
            }
            GUI.enabled = true;

            GUILayout.Space(8f);
            DrawStatus();
            GUILayout.Space(8f);
            GUILayout.Label($"Last action: {_lastAction}");
            GUILayout.Label("Client movement: WASD / left stick");

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        private void DrawStatus()
        {
            if (_manager?.Server != null)
            {
                GUILayout.Label($"Server tick: {_manager.Server.CurrentTick}");
                GUILayout.Label($"Authority players: {_manager.Server.PlayerCount}");
            }
            else if (_manager?.Client != null)
            {
                GUILayout.Label($"Client ID: {_manager.Client.ClientId}");
                GUILayout.Label($"Joined: {_manager.Client.IsJoined}");
                GUILayout.Label($"Input tick: {_manager.Client.CurrentInputTick}");
                GUILayout.Label($"Visible entities: {_manager.Client.EntityCount}");
            }
        }

        private void StartServer()
        {
            _lastAction = _manager.StartServer() ? "Server started" : "Server start rejected";
        }

        private void StartClient()
        {
            if (!string.IsNullOrWhiteSpace(_serverAddress))
            {
                NetClientConfig.Instance.ip = _serverAddress.Trim();
            }

            _lastAction = _manager.StartClient() ? "Client connecting" : "Client start rejected";
        }
    }
}
