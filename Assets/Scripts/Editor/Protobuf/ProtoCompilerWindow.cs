using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Framework.Editor
{
    /// <summary>
    /// Protobuf .proto 文件编译器工具窗口
    /// </summary>
    public class ProtoCompilerWindow : EditorWindow
    {
        private string _protocPath = "Protocol/protoc.exe";
        private string _outputPath = "Assets/Scripts/GamePlay/Protocol/Generated";
        private string _searchPath = "Assets";

        private Vector2 _scrollPosition;
        private Vector2 _outputScroll;
        private Vector2 _windowScroll;
        private string _fileSearch = "";
        private GUIStyle _metricStyle;
        private GUIStyle _hintStyle;
        private string _lastCompileTime = "";

        private readonly List<ProtoFileInfo> _protoFiles = new();

        private bool _showOutput;

        private string _lastError = "";
        private string _lastOutput = "";

        private class ProtoFileInfo
        {
            public string relativePath;
            public bool selected = true;
        }

        [MenuItem("Tools/Framework/Proto Compiler")]
        public static void ShowWindow()
        {
            var window = GetWindow<ProtoCompilerWindow>("协议编译");
            window.minSize = new Vector2(560, 620);
        }

        private void OnEnable()
        {
            minSize = new Vector2(560f, 620f);
            _protocPath = EditorPrefs.GetString("ProtoCompiler.ProtocPath", _protocPath);
            _outputPath = EditorPrefs.GetString("ProtoCompiler.OutputPath", _outputPath);
            _searchPath = EditorPrefs.GetString("ProtoCompiler.SearchPath", _searchPath);
            RefreshProtoFiles();
        }

        private void OnDisable()
        {
            EditorPrefs.SetString("ProtoCompiler.ProtocPath", _protocPath);
            EditorPrefs.SetString("ProtoCompiler.OutputPath", _outputPath);
            EditorPrefs.SetString("ProtoCompiler.SearchPath", _searchPath);
        }

        private void OnGUI()
        {
            _metricStyle ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 20, fixedHeight = 27f };
            _hintStyle ??= new GUIStyle(EditorStyles.wordWrappedMiniLabel);
            DrawHeader();
            _windowScroll = EditorGUILayout.BeginScrollView(_windowScroll);
            DrawProtocConfig();
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawMetric("发现协议", _protoFiles.Count.ToString());
                DrawMetric("已选文件", SelectedCount().ToString());
                DrawMetric("最近结果", !string.IsNullOrEmpty(_lastError) ? "存在错误" : string.IsNullOrEmpty(_lastOutput) ? "尚未编译" : "编译成功");
            }
            GUILayout.Space(6f);
            _showOutput = GUILayout.Toolbar(_showOutput ? 1 : 0, new[] { "协议文件", "编译输出" }) == 1;
            if (_showOutput) DrawOutput();
            else DrawProtoFiles();
            EditorGUILayout.EndScrollView();
            DrawCompileButton();
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Space(6f);
                GUILayout.Label("协议编译", EditorStyles.largeLabel);
                EditorGUILayout.LabelField("选择 .proto 文件，使用现有 protoc 配置生成 C# 协议代码。", EditorStyles.wordWrappedMiniLabel);
                GUILayout.Space(6f);
            }
        }

        private void DrawMetric(string title, string value)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.ExpandWidth(true)))
            {
                EditorGUILayout.LabelField(title, _hintStyle);
                EditorGUILayout.LabelField(value, _metricStyle);
            }
        }

        private void DrawProtocConfig()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("编译配置", EditorStyles.boldLabel);
                DrawPathField("编译器", ref _protocPath, true);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("搜索目录", GUILayout.Width(72f));
                    _searchPath = EditorGUILayout.TextField(_searchPath);
                    if (GUILayout.Button("扫描文件", GUILayout.Width(78f))) RefreshProtoFiles();
                }
                DrawPathField("输出目录", ref _outputPath, false);
                if (string.IsNullOrEmpty(_protocPath))
                    EditorGUILayout.HelpBox("未指定编译器路径，将沿用原有逻辑查找 protoc。", MessageType.Info);
                if (!Directory.Exists(_searchPath))
                    EditorGUILayout.HelpBox("搜索目录不存在，请调整后重新扫描。", MessageType.Warning);
                EditorGUILayout.LabelField("浏览输出目录时请选择项目内部目录；编译器查找规则和生成参数保持不变。", _hintStyle);
            }
            GUILayout.Space(6f);
        }

        private void DrawPathField(string label, ref string value, bool file)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(72f));
                value = EditorGUILayout.TextField(value);
                if (!GUILayout.Button("浏览…", GUILayout.Width(78f))) return;
                if (file)
                {
                    string path = EditorUtility.OpenFilePanel("选择 protoc", "", "exe");
                    if (!string.IsNullOrEmpty(path)) value = path;
                }
                else
                {
                    string path = EditorUtility.OpenFolderPanel("选择输出目录", "Assets", "");
                    if (string.IsNullOrEmpty(path)) return;
                    string projectRoot = Directory.GetParent(Application.dataPath).FullName.Replace('\\', '/');
                    path = path.Replace('\\', '/');
                    if (path.StartsWith(projectRoot + "/", StringComparison.OrdinalIgnoreCase))
                        value = path.Substring(projectRoot.Length + 1);
                    else
                        EditorUtility.DisplayDialog("输出目录", "请选择项目内部目录。", "确定");
                }
            }
        }

        private int SelectedCount()
        {
            int count = 0;
            foreach (ProtoFileInfo file in _protoFiles)
                if (file.selected) count++;
            return count;
        }

        private bool MatchesSearch(ProtoFileInfo file)
        {
            return string.IsNullOrWhiteSpace(_fileSearch)
                || file.relativePath.IndexOf(_fileSearch.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void DrawProtoFiles()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("协议文件", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    _fileSearch = GUILayout.TextField(_fileSearch, EditorStyles.toolbarSearchField);
                    if (GUILayout.Button("清除", EditorStyles.toolbarButton, GUILayout.Width(42f))) _fileSearch = "";
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("全选可见", EditorStyles.miniButtonLeft))
                        foreach (ProtoFileInfo file in _protoFiles)
                            if (MatchesSearch(file)) file.selected = true;
                    if (GUILayout.Button("取消可见", EditorStyles.miniButtonRight))
                        foreach (ProtoFileInfo file in _protoFiles)
                            if (MatchesSearch(file)) file.selected = false;
                }
                int shown = 0;
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition,
                    GUILayout.Height(Mathf.Clamp(position.height - 435f, 130f, 480f)));
                foreach (ProtoFileInfo file in _protoFiles)
                {
                    if (!MatchesSearch(file)) continue;
                    Rect row = EditorGUILayout.GetControlRect(false, 30f);
                    if (shown % 2 == 0)
                        EditorGUI.DrawRect(row, EditorGUIUtility.isProSkin
                            ? new Color(1f, 1f, 1f, 0.035f) : new Color(0f, 0f, 0f, 0.035f));
                    file.selected = EditorGUI.Toggle(new Rect(row.x + 5f, row.y + 6f, 20f, 18f), file.selected);
                    Rect labelRect = new Rect(row.x + 30f, row.y, row.width - 35f, row.height);
                    EditorGUI.LabelField(labelRect, new GUIContent(file.relativePath, file.relativePath));
                    shown++;
                }
                if (shown == 0)
                    EditorGUILayout.HelpBox(_protoFiles.Count == 0 ? "未找到 .proto 文件，请检查搜索目录后扫描。" : "没有匹配文件，请调整搜索内容。", MessageType.Info);
                EditorGUILayout.EndScrollView();
                EditorGUILayout.LabelField($"显示 {shown} / {_protoFiles.Count} 个文件 · 已选 {SelectedCount()} 个（包含筛选范围外的已选文件）", _hintStyle);
            }
        }

        private void DrawCompileButton()
        {
            int count = SelectedCount();
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUI.DisabledScope(count == 0 || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode))
                {
                    if (GUILayout.Button($"生成 C# 协议代码 · {count} 个文件", GUILayout.Height(34f))) CompileSelectedFiles();
                }
                string status = EditorApplication.isPlayingOrWillChangePlaymode ? "请退出播放模式后生成代码。"
                    : EditorApplication.isCompiling ? "Unity 正在编译脚本，请稍候。"
                    : count == 0 ? "选择至少一个协议文件后即可生成。"
                    : string.IsNullOrEmpty(_lastCompileTime) ? "就绪 · 输出至 " + _outputPath : "最近编译 " + _lastCompileTime;
                EditorGUILayout.LabelField(status, _hintStyle);
            }
        }

        private void DrawOutput()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("编译输出", EditorStyles.boldLabel);
                    using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_lastOutput) && string.IsNullOrEmpty(_lastError)))
                    {
                        if (GUILayout.Button("复制", EditorStyles.miniButtonLeft, GUILayout.Width(50f)))
                            EditorGUIUtility.systemCopyBuffer = _lastError + "\n" + _lastOutput;
                        if (GUILayout.Button("清空", EditorStyles.miniButtonRight, GUILayout.Width(50f)))
                        {
                            _lastOutput = "";
                            _lastError = "";
                            _lastCompileTime = "";
                        }
                    }
                }
                _outputScroll = EditorGUILayout.BeginScrollView(_outputScroll,
                    GUILayout.Height(Mathf.Clamp(position.height - 385f, 170f, 550f)));
                if (string.IsNullOrEmpty(_lastOutput) && string.IsNullOrEmpty(_lastError))
                    EditorGUILayout.HelpBox("尚无编译记录，生成完成后将在这里显示结果。", MessageType.Info);
                if (!string.IsNullOrEmpty(_lastError))
                {
                    EditorGUILayout.HelpBox("编译过程中存在错误，详细信息如下。", MessageType.Error);
                    DrawLogText(_lastError);
                }
                if (!string.IsNullOrEmpty(_lastOutput)) DrawLogText(_lastOutput);
                EditorGUILayout.EndScrollView();
            }
        }

        private static void DrawLogText(string text)
        {
            float height = EditorStyles.wordWrappedLabel.CalcHeight(new GUIContent(text), Mathf.Max(200f, EditorGUIUtility.currentViewWidth - 75f));
            EditorGUILayout.SelectableLabel(text, EditorStyles.wordWrappedLabel, GUILayout.MinHeight(Mathf.Max(35f, height)));
        }

        private void RefreshProtoFiles()
        {
            _protoFiles.Clear();

            if (!Directory.Exists(_searchPath))
            {
                _searchPath = "Assets";
            }

            string[] files = Directory.GetFiles(_searchPath, "*.proto", SearchOption.AllDirectories);
            Array.Sort(files);

            foreach (string filePath in files)
            {
                _protoFiles.Add(new ProtoFileInfo
                {
                    relativePath = filePath.Replace("\\", "/"),
                    selected = true
                });
            }


            Repaint();
        }

        private void CompileSelectedFiles()
        {
            List<string> selectedFiles = new();

            foreach (var file in _protoFiles)
            {
                if (file.selected)
                {
                    selectedFiles.Add(file.relativePath);
                }
            }

            if (selectedFiles.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "请至少选择一个 .proto 文件", "OK");
                return;
            }

            if (!Directory.Exists(_outputPath))
            {
                Directory.CreateDirectory(_outputPath);
            }

            string protoc = GetProtocPath();

            if (string.IsNullOrEmpty(protoc))
            {
                EditorUtility.DisplayDialog("Error", "未找到 protoc", "OK");
                return;
            }

            StringBuilder output = new();
            StringBuilder errors = new();

            bool hasError = false;

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;

            string includePath = Path.Combine(Path.GetDirectoryName(protoc), "include");

            if (!Directory.Exists(includePath))
            {
                includePath = Path.Combine(projectRoot, "Assets/Plugins/Protobuf/include");
            }

            EditorUtility.DisplayProgressBar("Compile Proto", "Starting...", 0);

            try
            {
                for (int i = 0; i < selectedFiles.Count; i++)
                {
                    string protoFile = selectedFiles[i];

                    EditorUtility.DisplayProgressBar(
                        "Compile Proto",
                        $"Compiling {Path.GetFileName(protoFile)}",
                        (float)i / selectedFiles.Count);

                    string protoAbsolutePath = Path.Combine(projectRoot, protoFile);
                    string outputAbsolutePath = Path.Combine(projectRoot, _outputPath);

                    StringBuilder args = new();

                    args.Append($"--csharp_out=\"{outputAbsolutePath}\" ");
                    args.Append($"--proto_path=\"{Path.GetDirectoryName(protoAbsolutePath)}\" ");

                    if (Directory.Exists(includePath))
                    {
                        args.Append($"--proto_path=\"{includePath}\" ");
                    }

                    args.Append($"\"{protoAbsolutePath}\"");

                    ProcessStartInfo info = new()
                    {
                        FileName = protoc,
                        Arguments = args.ToString(),
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    };

                    using (Process process = Process.Start(info))
                    {
                        string stdout = process.StandardOutput.ReadToEnd();
                        string stderr = process.StandardError.ReadToEnd();

                        process.WaitForExit();

                        if (process.ExitCode != 0 || !string.IsNullOrEmpty(stderr))
                        {
                            hasError = true;
                            errors.AppendLine($"[{Path.GetFileName(protoFile)}]");
                            errors.AppendLine(stderr);
                        }

                        if (!string.IsNullOrEmpty(stdout))
                        {
                            output.AppendLine(stdout);
                        }
                    }
                }

                AssetDatabase.Refresh();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            _showOutput = true;
            _lastCompileTime = DateTime.Now.ToString("HH:mm:ss");

            if (hasError)
            {
                _lastError = errors.ToString();
                _lastOutput = output.ToString();
            }
            else
            {
                _lastError = "";
                _lastOutput = output.Length > 0 ? output.ToString() : "所有 Proto 文件编译成功!";
            }

            EditorUtility.DisplayDialog("Compilation Finished", hasError ? 
                $"编译完成，但存在错误\n\n处理文件: {selectedFiles.Count}" : $"编译成功!\n\n处理文件: {selectedFiles.Count}", "OK");
        }
        
        private string GetProtocPath()
        {
            if (!string.IsNullOrEmpty(_protocPath) && File.Exists(_protocPath))
            {
                return _protocPath;
            }

            string[] commonPaths =
            {
                Path.Combine(Application.dataPath, "../Tools/protoc.exe"),

#if UNITY_EDITOR_WIN
                @"C:\protoc\bin\protoc.exe"
#endif
            };

            foreach (string path in commonPaths)
            {
                if (File.Exists(path))
                {
                    return Path.GetFullPath(path);
                }
            }

            try
            {
                ProcessStartInfo info = new()
                {
                    FileName = "where",
                    Arguments = "protoc",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };

                using Process process = Process.Start(info);
                string result = process.StandardOutput.ReadToEnd();

                process.WaitForExit();

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(result))
                {
                    return result.Split('\n')[0].Trim();
                }
            }
            catch
            {
                // ignored
            }

            return null;
        }
    }
}