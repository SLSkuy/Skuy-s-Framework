using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Events;
using Google.Protobuf;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Framework.Editor
{
    /// <summary>
    /// 按项目 Handler 结构生成网络消息收发与事件适配代码。
    /// </summary>
    public sealed class HandlerGeneratorWindow : EditorWindow
    {
        private enum HandlerSide { Client, Server, RoomPlayer }
        private enum ProtocolDirection { Receive, Send, Both }

        [Serializable]
        private class ProtocolField
        {
            public NetEvent eventId;
            public string messageType = "";
            public string memberName = "";
            public ProtocolDirection direction;
            public bool reliable = true;
        }

        [SerializeField] private string className = "FeatureClientHandler";
        [SerializeField] private string namespaceName = "GamePlay";
        [SerializeField] private string outputPath = "Assets/Scripts/GamePlay/Handler";
        [SerializeField] private HandlerSide side;
        [SerializeField] private List<ProtocolField> protocols = new();
        private ProtocolDropdown _protocolDropdown;
        private Type[] _messageTypes;
        private string[] _messageLabels;
        private Dictionary<string, Type> _messageLookup;
        private Vector2 _windowScroll;
        private Vector2 _previewScroll;
        private GUIStyle _metricStyle;
        private GUIStyle _codeStyle;
        private bool _showPreview;
        private string _lastResult = "";

        private static readonly HashSet<string> Keywords = new(
            ("abstract as base bool break byte case catch char checked class const continue decimal default delegate " +
             "do double else enum event explicit extern false finally fixed float for foreach goto if implicit in int " +
             "interface internal is lock long namespace new null object operator out override params private protected " +
             "public readonly ref return sbyte sealed short sizeof stackalloc static string struct switch this throw " +
             "true try typeof uint ulong unchecked unsafe ushort using virtual void volatile while").Split(' '));

        [MenuItem("Tools/Framework/Handler Generator")]
        public static void ShowWindow()
        {
            GetWindow<HandlerGeneratorWindow>("Handler 生成器").minSize = new Vector2(640f, 620f);
        }

        private void OnEnable()
        {
            minSize = new Vector2(640f, 620f);
            RefreshMessageTypes();
        }

        private void RefreshMessageTypes()
        {
            HashSet<string> namespaces = ReadProtocolNamespaces();
            _messageTypes = TypeCache.GetTypesDerivedFrom<IMessage>()
                .Where(type => namespaces.Contains(type.Namespace ?? "") && type.IsClass && type.IsVisible && !type.IsAbstract && !type.ContainsGenericParameters
                    && type.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(type => type.FullName, StringComparer.Ordinal).ToArray();
            _messageLookup = _messageTypes.ToDictionary(type => type.FullName, type => type);
            _messageLabels = new[] { "选择协议名称…" }.Concat(_messageTypes.Select(type => type.FullName)).ToArray();
        }

        /// <summary>只使用项目 proto 声明的命名空间，不包含依赖包中的内置协议。</summary>
        private static HashSet<string> ReadProtocolNamespaces()
        {
            var namespaces = new HashSet<string>(StringComparer.Ordinal);
            foreach (string path in Directory.EnumerateFiles(Application.dataPath, "*.proto", SearchOption.AllDirectories))
            {
                string relativePath = path.Substring(Application.dataPath.Length).Replace('\\', '/');
                if (relativePath.StartsWith("/ThirdParty/", StringComparison.OrdinalIgnoreCase)) continue;

                string source = File.ReadAllText(path);
                // 保留字符串字面量，避免把字符串中的 // 或 /* 当作注释。
                source = Regex.Replace(source, @"""(?:\\.|[^""\\])*""|//[^\r\n]*|/\*[\s\S]*?\*/",
                    match => match.Value.StartsWith("\"", StringComparison.Ordinal) ? match.Value : " ");
                Match explicitNamespace = Regex.Match(source, @"\boption\s+csharp_namespace\s*=\s*""([^""]*)""\s*;");
                if (explicitNamespace.Success)
                {
                    namespaces.Add(explicitNamespace.Groups[1].Value);
                    continue;
                }

                Match package = Regex.Match(source, @"\bpackage\s+([A-Za-z_][A-Za-z0-9_.]*)\s*;");
                namespaces.Add(package.Success ? GetPackageNamespace(package.Groups[1].Value) : "");
            }
            return namespaces;
        }

        private static string GetPackageNamespace(string package)
        {
            // protoc 未指定 csharp_namespace 时，将 package 的各段转为 PascalCase。
            return string.Join(".", package.Split('.').Select(segment =>
            {
                var name = new StringBuilder();
                bool capitalize = true;
                foreach (char character in segment)
                {
                    if (character == '_')
                    {
                        capitalize = true;
                        continue;
                    }
                    name.Append(capitalize ? char.ToUpperInvariant(character) : character);
                    capitalize = char.IsDigit(character);
                }
                return name.ToString();
            }));
        }

        private void OnGUI()
        {
            _metricStyle ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 20, fixedHeight = 27f };
            _codeStyle ??= new GUIStyle(EditorStyles.textArea) { wordWrap = false };
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Space(6f);
                GUILayout.Label("Handler 生成器", EditorStyles.largeLabel);
                EditorGUILayout.LabelField("组合协议类型与名称，生成事件、消息绑定和收发方法。", EditorStyles.wordWrappedMiniLabel);
                GUILayout.Space(6f);
            }
            _windowScroll = EditorGUILayout.BeginScrollView(_windowScroll);
            DrawConfiguration();
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawMetric("可用消息", _messageTypes.Length);
                DrawMetric("接收协议", protocols.Count(Receives));
                DrawMetric("发送协议", protocols.Count(Sends));
            }
            GUILayout.Space(6f);
            _showPreview = GUILayout.Toolbar(_showPreview ? 1 : 0, new[] { "协议配置", "代码预览" }) == 1;
            if (_showPreview)
            {
                _previewScroll = EditorGUILayout.BeginScrollView(_previewScroll, GUILayout.MinHeight(240f));
                EditorGUILayout.TextArea(BuildSource(), _codeStyle, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
            }
            else DrawProtocols();
            EditorGUILayout.EndScrollView();
            DrawFooter();
        }

        private void DrawMetric(string label, int count)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.ExpandWidth(true)))
            {
                EditorGUILayout.LabelField(label, EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField(count.ToString(), _metricStyle);
            }
        }

        private void DrawConfiguration()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("生成配置", EditorStyles.boldLabel);
                className = EditorGUILayout.TextField("类名", className);
                namespaceName = EditorGUILayout.TextField("命名空间", namespaceName);
                side = (HandlerSide)EditorGUILayout.Popup("处理端", (int)side,
                    new[] { "客户端 · NetClient", "服务端 · NetServer", "房间服务端 · RoomManager" });
                using (new EditorGUILayout.HorizontalScope())
                {
                    outputPath = EditorGUILayout.TextField("输出目录", outputPath);
                    if (GUILayout.Button("浏览…", GUILayout.Width(70f)))
                    {
                        string selected = EditorUtility.OpenFolderPanel("选择 Handler 输出目录", Application.dataPath, "");
                        if (!string.IsNullOrEmpty(selected))
                        {
                            string relative = FileUtil.GetProjectRelativePath(selected);
                            if (relative == "Assets" || relative.StartsWith("Assets/", StringComparison.Ordinal)) outputPath = relative;
                            else EditorUtility.DisplayDialog("输出目录", "请选择当前项目 Assets 内的目录。", "确定");
                        }
                    }
                }
                EditorGUILayout.HelpBox("模板：依赖字段 → 事件 → 消息绑定 → 发送消息 → 接收消息。\n" +
                    "Unbind 会清空事件订阅，请先 Bind，再由业务模块订阅事件；重新绑定后需重新订阅。", MessageType.Info);
            }
        }

        private void DrawProtocols()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("协议字段", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("刷新消息", EditorStyles.toolbarButton)) RefreshMessageTypes();
                if (GUILayout.Button("添加协议", EditorStyles.toolbarButton)) protocols.Add(new ProtocolField());
            }
            for (int i = 0; i < protocols.Count; i++)
            {
                ProtocolField protocol = protocols[i];
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"协议 {i + 1}", EditorStyles.boldLabel);
                        if (GUILayout.Button("移除", EditorStyles.miniButton, GUILayout.Width(50f)))
                        {
                            protocols.RemoveAt(i);
                            break;
                        }
                    }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PrefixLabel("协议（类型 / 名称）");
                        string[] eventNames = Enum.GetNames(typeof(NetEvent));
                        DrawSearchableDropdown("协议类型", protocol.eventId.ToString(), eventNames,
                            selected => protocol.eventId = (NetEvent)Enum.Parse(typeof(NetEvent), eventNames[selected]));
                        Type[] messageTypes = _messageTypes;
                        int index = Array.FindIndex(messageTypes, type => type.FullName == protocol.messageType) + 1;
                        DrawSearchableDropdown("协议名称", _messageLabels[index], _messageLabels, selected =>
                        {
                            protocol.messageType = selected == 0 ? "" : messageTypes[selected - 1].FullName;
                            protocol.memberName = selected == 0 ? "" : string.Concat(messageTypes[selected - 1].Name.Split('_'));
                        });
                    }
                    if (!string.IsNullOrEmpty(protocol.messageType) && !_messageLookup.ContainsKey(protocol.messageType))
                        EditorGUILayout.HelpBox("消息类型已不可用，请重新选择协议名称：" + protocol.messageType, MessageType.Warning);
                    protocol.direction = (ProtocolDirection)EditorGUILayout.Popup("方向", (int)protocol.direction,
                        new[] { "接收 · 事件通知", "发送", "接收与发送" });
                    protocol.memberName = EditorGUILayout.TextField("成员名称", protocol.memberName);
                    if (Sends(protocol)) protocol.reliable = EditorGUILayout.Toggle("可靠发送", protocol.reliable);
                    EditorGUILayout.LabelField("生成成员", (Receives(protocol) ? protocol.memberName + "Received / Handle" + protocol.memberName : "")
                        + (Sends(protocol) ? "  Send" + protocol.memberName : ""), EditorStyles.wordWrappedMiniLabel);
                }
            }
            if (protocols.Count == 0)
                EditorGUILayout.HelpBox("添加协议后，在同一个字段中选择 NetEvent 和对应的 Protobuf 消息类。", MessageType.Info);
        }

        private void DrawSearchableDropdown(string title, string selectedLabel, string[] labels, Action<int> onSelected)
        {
            Rect rect = EditorGUILayout.GetControlRect(GUILayout.MinWidth(100f));
            if (!EditorGUI.DropdownButton(rect, new GUIContent(selectedLabel, selectedLabel), FocusType.Keyboard)) return;
            _protocolDropdown = new ProtocolDropdown(title, labels, selected =>
            {
                onSelected(selected);
                Repaint();
            });
            _protocolDropdown.Show(rect);
        }

        /// <summary>使用 Unity 原生搜索下拉窗口，支持关键字筛选与键盘选择。</summary>
        private sealed class ProtocolDropdown : AdvancedDropdown
        {
            private readonly string _title;
            private readonly string[] _labels;
            private readonly Action<int> _onSelected;
            private readonly Dictionary<string, int> _indices = new(StringComparer.Ordinal);

            public ProtocolDropdown(string title, string[] labels, Action<int> onSelected) : base(new AdvancedDropdownState())
            {
                _title = title;
                _labels = labels;
                _onSelected = onSelected;
                for (int i = 0; i < labels.Length; i++) _indices.Add(labels[i], i);
                minimumSize = new Vector2(380f, 320f);
            }

            protected override AdvancedDropdownItem BuildRoot()
            {
                var root = new AdvancedDropdownItem(_title);
                for (int i = 0; i < _labels.Length; i++)
                    root.AddChild(new AdvancedDropdownItem(_labels[i]));
                return root;
            }

            protected override void ItemSelected(AdvancedDropdownItem item)
            {
                // Unity 的下拉项 ID 不代表原始数组下标，搜索结果也按唯一名称映射。
                _onSelected(_indices[item.name]);
            }
        }

        private void DrawFooter()
        {
            string error = ValidateConfiguration();
            bool overwrite = error == null && File.Exists(Path.Combine(outputPath, className + ".cs"));
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (!string.IsNullOrEmpty(error)) EditorGUILayout.HelpBox(error, MessageType.Warning);
                if (overwrite) EditorGUILayout.HelpBox("目标 Handler 已存在。覆盖将替换整个脚本，请先检查代码预览。", MessageType.Warning);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("复制代码", GUILayout.Height(32f))) EditorGUIUtility.systemCopyBuffer = BuildSource();
                    using (new EditorGUI.DisabledScope(error != null || EditorApplication.isCompiling
                        || EditorApplication.isPlayingOrWillChangePlaymode))
                    {
                        if (GUILayout.Button(overwrite ? "覆盖 Handler" : "生成 Handler", GUILayout.Height(32f))) Generate();
                    }
                }
                EditorGUILayout.LabelField(string.IsNullOrEmpty(_lastResult)
                    ? (overwrite ? "覆盖现有文件 · " : "生成新文件 · ") + outputPath + "/" + className + ".cs" : _lastResult, EditorStyles.wordWrappedMiniLabel);
            }
        }

        private static bool Receives(ProtocolField protocol) => protocol.direction != ProtocolDirection.Send;
        private static bool Sends(ProtocolField protocol) => protocol.direction != ProtocolDirection.Receive;
        private static bool IsIdentifier(string value) => !string.IsNullOrEmpty(value)
            && Regex.IsMatch(value, @"^[\p{L}_][\p{L}\p{Nd}_]*$") && !Keywords.Contains(value);

        private string ValidateConfiguration()
        {
            if (!IsIdentifier(className) || !className.EndsWith("Handler", StringComparison.Ordinal))
                return "类名必须是合法 C# 标识符，且以 Handler 结尾。";
            if (string.IsNullOrWhiteSpace(namespaceName) || !namespaceName.Split('.').All(IsIdentifier))
                return "请输入合法的命名空间。";
            string path = outputPath.Replace('\\', '/');
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || path.Split('/').Any(part => part == ".."
                || part == "." || part.Equals("Editor", StringComparison.OrdinalIgnoreCase))
                || path.IndexOfAny(new[] { ':', '*', '?', '"', '<', '>', '|' }) >= 0)
                return "输出目录必须位于 Assets 内，且不能包含 Editor、相对跳转或非法字符。";
            if (protocols.Count == 0) return "请添加至少一个协议。";
            var events = new HashSet<NetEvent>();
            var members = new HashSet<string> { className, "Bind", "Unbind", "_client", "_server", "_room" };
            foreach (ProtocolField protocol in protocols)
            {
                if (!_messageLookup.ContainsKey(protocol.messageType)) return "请为每条协议选择有效的消息类型。";
                if (!IsIdentifier(protocol.memberName)) return "成员名称必须是合法 C# 标识符。";
                if (!events.Add(protocol.eventId)) return "同一个 NetEvent 只能配置一次；双向协议请选择“接收与发送”。";
                if (Receives(protocol) && (!members.Add(protocol.memberName + "Received") || !members.Add("Handle" + protocol.memberName)))
                    return "生成的事件或接收方法重名，请调整成员名称。";
                if (Sends(protocol) && !members.Add("Send" + protocol.memberName)) return "生成的发送方法重名，请调整成员名称。";
            }
            return null;
        }

        private string BuildSource()
        {
            string dependency = side == HandlerSide.Client ? "NetClient" : side == HandlerSide.Server ? "NetServer" : "RoomManager";
            string field = side == HandlerSide.Client ? "_client" : side == HandlerSide.Server ? "_server" : "_room";
            string idType = side == HandlerSide.Server ? "uint" : "Guid";
            string idName = side == HandlerSide.Server ? "connectionId" : "playerId";
            string idParameter = side == HandlerSide.Client ? "" : idType + " " + idName + ", ";
            string idArgument = side == HandlerSide.Client ? "" : idName + ", ";
            string register = side == HandlerSide.RoomPlayer ? "RegisterPlayerHandler" : "RegisterHandler";
            string unregister = side == HandlerSide.RoomPlayer ? "UnregisterPlayerHandler" : "UnregisterHandler";
            var source = new StringBuilder();
            void Line(string text = "") => source.Append(text).Append("\r\n");
            string MessageType(ProtocolField protocol) => string.IsNullOrEmpty(protocol.messageType)
                ? "IMessage" : "global::" + protocol.messageType.Replace('+', '.');
            Line("using System;");
            Line("using Events;");
            Line("using Framework;");
            Line(side == HandlerSide.RoomPlayer ? "using GamePlay.Room;" : "using Network;");
            if (protocols.Any(protocol => string.IsNullOrEmpty(protocol.messageType))) Line("using Google.Protobuf;");
            Line();
            Line("namespace " + namespaceName);
            Line("{");
            Line("    public sealed class " + className + " : IHandler");
            Line("    {");
            Line($"        private {dependency} {field};");
            Line();
            Line("        #region 事件");
            foreach (ProtocolField protocol in protocols.Where(Receives))
                Line($"        public event Action<{(side == HandlerSide.Client ? "" : idType + ", ")}{MessageType(protocol)}> {protocol.memberName}Received;");
            Line("        #endregion");
            Line();
            Line("        #region 消息绑定");
            Line();
            Line("        public void Bind()");
            Line("        {");
            Line("            Unbind();");
            Line($"            {field} = Global.Get<{dependency}>();");
            foreach (ProtocolField protocol in protocols.Where(Receives))
                Line($"            {field}.{register}<{MessageType(protocol)}>(NetEvent.{protocol.eventId}, Handle{protocol.memberName});");
            Line("        }");
            Line();
            Line("        public void Unbind()");
            Line("        {");
            Line($"            if ({field} == null) return;");
            Line();
            foreach (ProtocolField protocol in protocols.Where(Receives))
                Line($"            {field}.{unregister}<{MessageType(protocol)}>(NetEvent.{protocol.eventId}, Handle{protocol.memberName});");
            Line($"            {field} = null;");
            if (protocols.Any(Receives)) Line();
            foreach (ProtocolField protocol in protocols.Where(Receives)) Line($"            {protocol.memberName}Received = null;");
            Line("        }");
            Line();
            Line("        #endregion");
            Line();
            Line("        #region 发送消息");
            foreach (ProtocolField protocol in protocols.Where(Sends))
            {
                Line();
                Line($"        public void Send{protocol.memberName}({idParameter}{MessageType(protocol)} message)");
                Line("        {");
                Line($"            {field}.{(protocol.reliable ? "SendReliable" : "Send")}({idArgument}NetEvent.{protocol.eventId}, message);");
                Line("        }");
            }
            Line();
            Line("        #endregion");
            Line();
            Line("        #region 接收消息");
            foreach (ProtocolField protocol in protocols.Where(Receives))
            {
                Line();
                Line($"        private void Handle{protocol.memberName}({idParameter}{MessageType(protocol)} message)");
                Line("        {");
                Line($"            {protocol.memberName}Received?.Invoke({idArgument}message);");
                Line("        }");
            }
            Line();
            Line("        #endregion");
            Line("    }");
            Line("}");
            return source.ToString().TrimEnd('\r', '\n');
        }

        private void Generate()
        {
            string error = ValidateConfiguration();
            if (error != null)
            {
                EditorUtility.DisplayDialog("无法生成", error, "确定");
                return;
            }
            string path = Path.Combine(outputPath, className + ".cs").Replace('\\', '/');
            bool overwrite = File.Exists(path);
            if (overwrite && !EditorUtility.DisplayDialog("覆盖 Handler",
                "将替换以下脚本的全部内容（包含手动修改），保留原有 .meta：\n" + path, "覆盖", "取消")) return;
            try
            {
                string source = BuildSource();
                Directory.CreateDirectory(outputPath);
                using (var stream = new FileStream(path, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(true))) writer.Write(source);
                AssetDatabase.Refresh();
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                EditorGUIUtility.PingObject(Selection.activeObject);
                _lastResult = (overwrite ? "已覆盖 · " : "已生成 · ") + path;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                _lastResult = "生成失败 · " + exception.Message;
                EditorUtility.DisplayDialog("生成失败", exception.Message, "确定");
            }
        }
    }
}