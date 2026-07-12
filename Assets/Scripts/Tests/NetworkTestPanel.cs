using System;
using System.Collections.Generic;
using System.Text;
using Core;
using NetConnect;
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
        private string _clientIp = "127.0.0.1";
        private bool _showPanel = true;

        // 传输类型选择
        private bool _useTcpForChat;
        private bool _useTcpForBroadcast;

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
                AddLog($"[NetworkTestPanel] 服务端已启动 (TCP:11451, KCP:19198)");
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
                _netClient.StartReliableConnect();
                _isClientRunning = true;
                AddLog($"[NetworkTestPanel] 客户端已连接 (TCP+KCP)");
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

            Chat_Test msg = new Chat_Test()
            {
                Content = _chatInput,
            };
            if (_useTcpForChat)
            {
                _netClient.SendReliable(NetEvent.CHAT_TEST, msg);
                AddLog($"[客户端 -TCP-> 服务端] {_chatInput}");
            }
            else
            {
                _netClient.Send(NetEvent.CHAT_TEST, msg);
                AddLog($"[客户端 -KCP-> 服务端] {_chatInput}");
            }
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

            Chat_Test msg = new Chat_Test()
            {
                Content = _broadcastInput,
            };
            if (_useTcpForBroadcast)
            {
                _netServer.BroadcastReliable(NetEvent.CHAT_TEST, msg);
                AddLog($"[服务端 -TCP广播->] {_broadcastInput}");
            }
            else
            {
                _netServer.Broadcast(NetEvent.CHAT_TEST, msg);
                AddLog($"[服务端 -KCP广播->] {_broadcastInput}");
            }
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

            GUILayout.BeginArea(new Rect(10, 10, 500, 650));
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
                GUILayout.Label($"TCP端口:11451  KCP端口:19198", new GUIStyle(GUI.skin.label) { fontSize = 11 });
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

                GUILayout.Label($"状态: {(_isServerRunning ? "运行中 (TCP+KCP)" : "未启动")}",
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

                GUILayout.Label($"状态: {(_isClientRunning ? "已连接 (TCP+KCP)" : "未连接")}",
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
                if (GUILayout.Button("发送", GUILayout.Width(60)))
                {
                    SendChatMessage();
                }

                GUI.enabled = true;
                GUILayout.EndHorizontal();

                // 传输类型切换
                GUILayout.BeginHorizontal();
                var transportName = _useTcpForChat ? "TCP" : "KCP";
                var transportColor = _useTcpForChat ? Color.cyan : Color.yellow;
                var toggleStyle = new GUIStyle(GUI.skin.toggle)
                {
                    normal = { textColor = transportColor },
                    fontSize = 11
                };
                _useTcpForChat = GUILayout.Toggle(_useTcpForChat, $"使用TCP发送 (当前: {transportName})", toggleStyle);
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
                if (GUILayout.Button("广播", GUILayout.Width(60)))
                {
                    SendBroadcast();
                }

                GUI.enabled = true;
                GUILayout.EndHorizontal();

                // 传输类型切换
                GUILayout.BeginHorizontal();
                var transportName = _useTcpForBroadcast ? "TCP" : "KCP";
                var transportColor = _useTcpForBroadcast ? Color.cyan : Color.yellow;
                var toggleStyle = new GUIStyle(GUI.skin.toggle)
                {
                    normal = { textColor = transportColor },
                    fontSize = 11
                };
                _useTcpForBroadcast = GUILayout.Toggle(_useTcpForBroadcast, $"使用TCP广播 (当前: {transportName})", toggleStyle);
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

                // 自动滚到底部
                _logScrollPosition.y = float.MaxValue;

                _logScrollPosition = GUILayout.BeginScrollView(
                    _logScrollPosition,
                    false, true,
                    GUILayout.ExpandHeight(true));

                GUILayout.Label(logText, logStyle, GUILayout.ExpandWidth(true));

                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();
        }

        #endregion
    }
}
