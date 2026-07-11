using System;
using System.Collections.Generic;
using System.Text;
using Core;
using UnityEngine;

namespace Network.Test
{
    /// <summary>
    /// 网络传输测试面板 - 用于测试Host/Client通信
    /// </summary>
    public class NetworkTestPanel : MonoBehaviour
    {
        private const int MAX_LOG_LINES = 30;

        // 服务端
        private NetServer _netServer;
        private bool _isServerRunning;

        // 客户端
        private NetClient _netClient;
        private bool _isClientRunning;

        // 日志
        private readonly List<string> _logLines = new();
        private Vector2 _logScrollPosition;

        // UI输入
        private string _chatInput = "";
        private string _broadcastInput = "";
        private string _serverPort = "19198";
        private string _clientIp = "127.0.0.1";
        private string _clientPort = "19198";
        private bool _showPanel = true;

        /// <summary>
        /// 运行时自动在场景中创建面板
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreateInScene()
        {
            var existing = FindFirstObjectByType<NetworkTestPanel>();
            if (existing != null) return;

            var go = new GameObject("NetworkTestPanel");
            go.AddComponent<NetworkTestPanel>();
            DontDestroyOnLoad(go);
            Debug.Log("[NetworkTestPanel] 已在场景中自动创建面板");
        }

        private void Start()
        {
            InitializeNetworkSystems();
        }

        private void OnEnable()
        {
            Application.logMessageReceived += OnLogReceived;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= OnLogReceived;
        }

        private void OnDestroy()
        {
            ShutdownNetworkSystems();
        }

        #region 初始化与销毁

        private void InitializeNetworkSystems()
        {
            var gameCore = GameCore.Instance;
            if (gameCore == null)
            {
                AddLog("[NetworkTestPanel] GameCore 未找到，请确保场景中存在 GameCore");
                return;
            }

            var sysMgr = gameCore.SystemMgr;
            if (sysMgr == null)
            {
                AddLog("[NetworkTestPanel] SystemManager 未找到");
                return;
            }

            // 注册 NetServer
            _netServer = sysMgr.GetSystem<NetServer>();
            if (_netServer == null)
            {
                _netServer = sysMgr.RegisterSystem<NetServer>();
                AddLog("[NetworkTestPanel] NetServer 已注册");
            }

            // 注册 NetClient
            _netClient = sysMgr.GetSystem<NetClient>();
            if (_netClient == null)
            {
                _netClient = sysMgr.RegisterSystem<NetClient>();
                AddLog("[NetworkTestPanel] NetClient 已注册");
            }
        }

        private void ShutdownNetworkSystems()
        {
            if (_isClientRunning)
            {
                _netClient?.StopClient();
                _isClientRunning = false;
            }

            if (_isServerRunning)
            {
                _netServer?.StopServer();
                _isServerRunning = false;
            }
        }

        #endregion

        #region 日志

        private void OnLogReceived(string logString, string stackTrace, LogType type)
        {
            // 只捕获网络相关日志
            if (logString.Contains("[NetServer]") || logString.Contains("[NetClient]") ||
                logString.Contains("[NetworkTestPanel]"))
            {
                AddLog(logString);
            }
        }

        private void AddLog(string msg)
        {
            _logLines.Add($"[{DateTime.Now:HH:mm:ss}] {msg}");
            if (_logLines.Count > MAX_LOG_LINES)
            {
                _logLines.RemoveRange(0, _logLines.Count - MAX_LOG_LINES);
            }
        }

        #endregion

        #region 网络操作

        private void StartServer()
        {
            if (_netServer == null)
            {
                AddLog("[NetworkTestPanel] NetServer 未初始化");
                return;
            }

            try
            {
                _netServer.StartServer();
                _isServerRunning = true;
                AddLog($"[NetworkTestPanel] 服务端已启动，端口: {_serverPort}");
            }
            catch (Exception e)
            {
                AddLog($"[NetworkTestPanel] 服务端启动失败: {e.Message}");
            }
        }

        private void StopServer()
        {
            _netServer?.StopServer();
            _isServerRunning = false;
            AddLog("[NetworkTestPanel] 服务端已停止");
        }

        private void StartClient()
        {
            if (_netClient == null)
            {
                AddLog("[NetworkTestPanel] NetClient 未初始化");
                return;
            }

            try
            {
                _netClient.StartClient();
                _isClientRunning = true;
                AddLog($"[NetworkTestPanel] 客户端已连接: {_clientIp}:{_clientPort}");
            }
            catch (Exception e)
            {
                AddLog($"[NetworkTestPanel] 客户端连接失败: {e.Message}");
            }
        }

        private void StopClient()
        {
            _netClient?.StopClient();
            _isClientRunning = false;
            AddLog("[NetworkTestPanel] 客户端已断开");
        }

        private void SendChatMessage()
        {
            if (string.IsNullOrWhiteSpace(_chatInput)) return;
            if (!_isClientRunning)
            {
                AddLog("[NetworkTestPanel] 客户端未连接，无法发送消息");
                return;
            }

            var data = Encoding.UTF8.GetBytes(_chatInput);
            _netClient.Send(data);
            AddLog($"[客户端 -> 服务端] {_chatInput}");
            _chatInput = "";
        }

        private void SendBroadcast()
        {
            if (string.IsNullOrWhiteSpace(_broadcastInput)) return;
            if (!_isServerRunning)
            {
                AddLog("[NetworkTestPanel] 服务端未启动，无法广播");
                return;
            }

            var data = Encoding.UTF8.GetBytes(_broadcastInput);
            _netServer.Broadcast(data);
            AddLog($"[服务端 -> 广播] {_broadcastInput}");
            _broadcastInput = "";
        }

        #endregion

        #region GUI

        private void OnGUI()
        {
            if (!_showPanel) return;

            float scale = Mathf.Min(Screen.width / 800f, Screen.height / 600f, 1.5f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
                new Vector3(scale, scale, 1f));

            GUILayout.BeginArea(new Rect(10, 10, 500, 580));
            {
                DrawTitle();
                DrawServerSection();
                DrawClientSection();
                DrawChatSection();
                DrawBroadcastSection();
                DrawLogSection();
            }
            GUILayout.EndArea();

            // 关闭按钮
            if (GUI.Button(new Rect(520, 10, 80, 25), "关闭面板"))
            {
                _showPanel = false;
            }
        }

        private void DrawTitle()
        {
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label("网络传输测试面板", titleStyle);
            GUILayout.Space(5);
        }

        private void DrawServerSection()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.Label("服务端 (Host)", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });

                GUILayout.BeginHorizontal();
                GUILayout.Label("端口:", GUILayout.Width(40));
                _serverPort = GUILayout.TextField(_serverPort, GUILayout.Width(60));
                GUILayout.FlexibleSpace();

                GUI.enabled = !_isServerRunning;
                if (GUILayout.Button("开启 Host", GUILayout.Width(100)))
                {
                    StartServer();
                }

                GUI.enabled = _isServerRunning;
                if (GUILayout.Button("停止 Host", GUILayout.Width(100)))
                {
                    StopServer();
                }

                GUI.enabled = true;
                GUILayout.EndHorizontal();

                GUILayout.Label($"状态: {(_isServerRunning ? "运行中" : "未启动")}",
                    new GUIStyle(GUI.skin.label)
                    {
                        normal = { textColor = _isServerRunning ? Color.green : Color.gray }
                    });
            }
            GUILayout.EndVertical();
            GUILayout.Space(5);
        }

        private void DrawClientSection()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.Label("客户端 (Client)", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });

                GUILayout.BeginHorizontal();
                GUILayout.Label("IP:", GUILayout.Width(25));
                _clientIp = GUILayout.TextField(_clientIp, GUILayout.Width(100));
                GUILayout.Label("端口:", GUILayout.Width(35));
                _clientPort = GUILayout.TextField(_clientPort, GUILayout.Width(50));
                GUILayout.FlexibleSpace();

                GUI.enabled = !_isClientRunning;
                if (GUILayout.Button("连接服务器", GUILayout.Width(100)))
                {
                    StartClient();
                }

                GUI.enabled = _isClientRunning;
                if (GUILayout.Button("断开连接", GUILayout.Width(100)))
                {
                    StopClient();
                }

                GUI.enabled = true;
                GUILayout.EndHorizontal();

                GUILayout.Label($"状态: {(_isClientRunning ? "已连接" : "未连接")}",
                    new GUIStyle(GUI.skin.label)
                    {
                        normal = { textColor = _isClientRunning ? Color.green : Color.gray }
                    });
            }
            GUILayout.EndVertical();
            GUILayout.Space(5);
        }

        private void DrawChatSection()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.Label("客户端 -> 服务端 消息", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });

                GUILayout.BeginHorizontal();
                _chatInput = GUILayout.TextField(_chatInput, GUILayout.ExpandWidth(true));

                GUI.enabled = _isClientRunning;
                if (GUILayout.Button("发送", GUILayout.Width(80)))
                {
                    SendChatMessage();
                }

                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.Space(5);
        }

        private void DrawBroadcastSection()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.Label("服务端 -> 广播 消息", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });

                GUILayout.BeginHorizontal();
                _broadcastInput = GUILayout.TextField(_broadcastInput, GUILayout.ExpandWidth(true));

                GUI.enabled = _isServerRunning;
                if (GUILayout.Button("广播", GUILayout.Width(80)))
                {
                    SendBroadcast();
                }

                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.Space(5);
        }

        private void DrawLogSection()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.Label("通信日志", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });

                var logStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    wordWrap = true
                };

                string logText = _logLines.Count > 0
                    ? string.Join("\n", _logLines)
                    : "等待通信...";

                // 计算日志文本总高度，用于自动滚到底部
                float logHeight = logStyle.CalcHeight(new GUIContent(logText), 480);

                _logScrollPosition = GUILayout.BeginScrollView(
                    _logScrollPosition,
                    false, true,
                    GUILayout.Height(180));

                GUILayout.Label(logText, logStyle, GUILayout.ExpandWidth(true));

                // 自动滚动到底部（内容高度超出视口时）
                if (logHeight > 180)
                {
                    _logScrollPosition.y = logHeight;
                }

                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();
        }

        #endregion
    }
}
