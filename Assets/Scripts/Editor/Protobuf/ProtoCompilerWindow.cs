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

        private readonly List<ProtoFileInfo> _protoFiles = new();

        private bool _selectAll = true;
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
            var window = GetWindow<ProtoCompilerWindow>("Proto Compiler");
            window.minSize = new Vector2(520, 600);
        }

        private void OnEnable()
        {
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
            EditorGUILayout.BeginVertical();

            DrawHeader();
            DrawProtocConfig();

            DrawProtoFiles();

            GUILayout.FlexibleSpace();

            DrawCompileButton();
            DrawOutput();

            EditorGUILayout.EndVertical();
        }

        private void DrawHeader()
        {
            GUILayout.Space(5);

            GUILayout.Label("Protobuf Compiler", EditorStyles.largeLabel);
            EditorGUILayout.LabelField("Generate C# protocol classes from .proto files", EditorStyles.miniLabel);

            GUILayout.Space(10);
        }

        private void DrawSection(string title, Action content, params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, options);

            GUILayout.Label(title, EditorStyles.boldLabel);
            GUILayout.Space(5);

            content?.Invoke();

            EditorGUILayout.EndVertical();
            GUILayout.Space(8);
        }

        private void DrawProtocConfig()
        {
            DrawSection("⚙ Protoc Configuration", () =>
            {
                DrawPathField("Protoc Path", ref _protocPath, true);

                GUILayout.Space(5);

                if (string.IsNullOrEmpty(_protocPath))
                {
                    EditorGUILayout.HelpBox("请设置 protoc 路径，或者加入系统 PATH", MessageType.Info);
                }

                GUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();

                _searchPath = EditorGUILayout.TextField("Search Path", _searchPath);

                if (GUILayout.Button("Refresh", GUILayout.Width(70)))
                {
                    RefreshProtoFiles();
                }

                EditorGUILayout.EndHorizontal();

                GUILayout.Space(5);

                DrawPathField("Output Path", ref _outputPath, false);
            });
        }

        private void DrawPathField(string label, ref string value, bool file)
        {
            EditorGUILayout.LabelField(label);

            EditorGUILayout.BeginHorizontal();

            value = EditorGUILayout.TextField(value);

            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                if (file)
                {
                    string path = EditorUtility.OpenFilePanel("Select protoc", "", "exe");

                    if (!string.IsNullOrEmpty(path))
                    {
                        value = path;
                    }
                }
                else
                {
                    string path = EditorUtility.OpenFolderPanel("Select output directory", "Assets", "");

                    if (!string.IsNullOrEmpty(path))
                    {
                        string projectRoot = Directory.GetParent(Application.dataPath).FullName;

                        if (path.StartsWith(projectRoot))
                        {
                            value = path.Substring(projectRoot.Length + 1);
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Error", "请选择项目内部目录", "OK");
                        }
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawProtoFiles()
        {
            DrawSection($"📄 Proto Files ({_protoFiles.Count})", () =>
            {
                if (_protoFiles.Count == 0)
                {
                    EditorGUILayout.HelpBox("未找到 .proto 文件", MessageType.Warning);
                    return;
                }

                _selectAll = _protoFiles.TrueForAll(x => x.selected);

                EditorGUI.BeginChangeCheck();

                _selectAll = EditorGUILayout.Toggle("Select All", _selectAll);

                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var file in _protoFiles)
                    {
                        file.selected = _selectAll;
                    }
                }

                GUILayout.Space(5);

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));

                    foreach (var file in _protoFiles)
                    {
                        file.selected = EditorGUILayout.ToggleLeft(file.relativePath, file.selected);
                    }

                    EditorGUILayout.EndScrollView();
                }
            }, GUILayout.ExpandHeight(true));
        }

        private void DrawCompileButton()
        {
            int count = 0;

            foreach (var file in _protoFiles)
            {
                if (file.selected)
                {
                    count++;
                }
            }

            GUI.enabled = count > 0;

            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.35f, 0.8f, 0.35f);

            if (GUILayout.Button($"▶ Compile ({count}) Files", GUILayout.Height(40)))
            {
                CompileSelectedFiles();
            }

            GUI.backgroundColor = oldColor;
            GUI.enabled = true;

            GUILayout.Space(5);
        }

        private void DrawOutput()
        {
            if (!string.IsNullOrEmpty(_lastOutput) || !string.IsNullOrEmpty(_lastError))
            {
                _showOutput = EditorGUILayout.Foldout(_showOutput, "📤 Output Log", true);
            }

            if (!_showOutput)
            {
                return;
            }

            DrawSection("Compilation Result", () =>
            {
                if (!string.IsNullOrEmpty(_lastError))
                {
                    EditorGUILayout.HelpBox(_lastError, MessageType.Error);
                }

                if (!string.IsNullOrEmpty(_lastOutput))
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField(_lastOutput, EditorStyles.wordWrappedLabel);
                    }
                }

                if (GUILayout.Button("Clear Output"))
                {
                    _lastOutput = "";
                    _lastError = "";
                    _showOutput = false;
                }
            });
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

            _selectAll = true;

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