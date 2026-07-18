using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Framework.Editor
{
    /// <summary>
    /// Protobuf .proto 文件编译器工具窗口
    /// 识别项目中的 .proto 文件，并使用 protoc 生成 C# 协议文件
    /// </summary>
    public class ProtoCompilerWindow : EditorWindow
    {
        private string protocPath = "Protocol/protoc.exe";
        private string outputPath = "Assets/Scripts/GamePlay/Protocol/Generated";
        private string searchPath = "Assets";
        private Vector2 scrollPosition;
        private List<ProtoFileInfo> protoFiles = new();
        private bool selectAll = true;
        private string lastError = "";
        private string lastOutput = "";
        private bool showOutput;

        private class ProtoFileInfo
        {
            public string relativePath;
            public bool selected = true;
        }

        [MenuItem("Tools/Protobuf/Proto Compiler")]
        public static void ShowWindow()
        {
            var window = GetWindow<ProtoCompilerWindow>("Proto Compiler");
            window.minSize = new Vector2(480, 400);
        }

        private void OnEnable()
        {
            RefreshProtoFiles();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                // 标题
                GUILayout.Label("Protobuf Compiler", EditorStyles.boldLabel);
                GUILayout.Space(5);

                // protoc 路径配置
                EditorGUILayout.LabelField("Protoc Configuration", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    protocPath = EditorGUILayout.TextField("Protoc Path", protocPath);
                    if (GUILayout.Button("Browse", GUILayout.Width(60)))
                    {
                        string selectedPath = EditorUtility.OpenFilePanel("Select protoc executable", "", "exe");
                        if (!string.IsNullOrEmpty(selectedPath))
                        {
                            protocPath = selectedPath;
                        }
                    }
                }

                if (string.IsNullOrEmpty(protocPath))
                {
                    EditorGUILayout.HelpBox("请设置 protoc 可执行文件路径，或确保 protoc 已在系统 PATH 中", MessageType.Info);
                }

                GUILayout.Space(5);

                // 搜索路径
                using (new EditorGUILayout.HorizontalScope())
                {
                    searchPath = EditorGUILayout.TextField("Search Path", searchPath);
                    if (GUILayout.Button("Refresh", GUILayout.Width(60)))
                    {
                        RefreshProtoFiles();
                    }
                }

                GUILayout.Space(5);

                // 输出路径
                using (new EditorGUILayout.HorizontalScope())
                {
                    outputPath = EditorGUILayout.TextField("Output Path", outputPath);
                    if (GUILayout.Button("Browse", GUILayout.Width(60)))
                    {
                        string selectedPath = EditorUtility.OpenFolderPanel("Select output directory", "Assets", "");
                        if (!string.IsNullOrEmpty(selectedPath))
                        {
                            string projectPath = Directory.GetParent(Application.dataPath).FullName;
                            if (selectedPath.StartsWith(projectPath))
                            {
                                outputPath = selectedPath.Substring(projectPath.Length + 1);
                            }
                            else
                            {
                                EditorUtility.DisplayDialog("Error", "请选择项目内的目录", "OK");
                            }
                        }
                    }
                }

                GUILayout.Space(10);

                // Proto 文件列表
                EditorGUILayout.LabelField($"Found Proto Files ({protoFiles.Count})", EditorStyles.boldLabel);
                GUILayout.Space(5);

                if (protoFiles.Count > 0)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.BeginChangeCheck();
                        selectAll = EditorGUILayout.Toggle("Select All", selectAll);
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var f in protoFiles)
                            {
                                f.selected = selectAll;
                            }
                        }
                    }

                    GUILayout.Space(5);

                    scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
                    foreach (var file in protoFiles)
                    {
                        file.selected = EditorGUILayout.ToggleLeft(file.relativePath, file.selected);
                    }
                    EditorGUILayout.EndScrollView();
                }
                else
                {
                    EditorGUILayout.HelpBox("未找到 .proto 文件", MessageType.Warning);
                }

                GUILayout.Space(10);

                // 编译按钮
                GUI.enabled = protoFiles.Count > 0;
                if (GUILayout.Button("Compile Proto Files", GUILayout.Height(35)))
                {
                    CompileSelectedFiles();
                }
                GUI.enabled = true;

                GUILayout.Space(10);

                // 输出日志区域
                if (showOutput && (!string.IsNullOrEmpty(lastOutput) || !string.IsNullOrEmpty(lastError)))
                {
                    EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);

                    if (!string.IsNullOrEmpty(lastError))
                    {
                        EditorGUILayout.HelpBox(lastError, MessageType.Error);
                    }

                    if (!string.IsNullOrEmpty(lastOutput))
                    {
                        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                        {
                            EditorGUILayout.LabelField(lastOutput, EditorStyles.wordWrappedLabel);
                        }
                    }

                    if (GUILayout.Button("Clear Output"))
                    {
                        lastOutput = "";
                        lastError = "";
                        showOutput = false;
                    }
                }
            }
        }

        private void RefreshProtoFiles()
        {
            protoFiles.Clear();

            if (!Directory.Exists(searchPath))
            {
                searchPath = "Assets";
            }

            string[] files = Directory.GetFiles(searchPath, "*.proto", SearchOption.AllDirectories);
            foreach (string filePath in files)
            {
                protoFiles.Add(new ProtoFileInfo
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
            foreach (var file in protoFiles)
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

            // 确保输出目录存在
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            // 确定 protoc 路径
            string protoc = GetProtocPath();
            if (string.IsNullOrEmpty(protoc))
            {
                EditorUtility.DisplayDialog("Error", "未找到 protoc，请在设置中指定路径或将 protoc 添加到系统 PATH", "OK");
                return;
            }

            StringBuilder allOutput = new StringBuilder();
            StringBuilder allErrors = new StringBuilder();
            bool hasErrors = false;

            // 获取项目根目录
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;

            // 获取 include 目录 (protoc 需要 google/protobuf 的 .proto 导入)
            string includePath = Path.Combine(Path.GetDirectoryName(protoc), "include");
            if (!Directory.Exists(includePath))
            {
                includePath = Path.Combine(projectRoot, "Assets/Plugins/Protobuf/include");
            }

            EditorUtility.DisplayProgressBar("Compiling Proto", "Compiling...", 0f);

            try
            {
                for (int i = 0; i < selectedFiles.Count; i++)
                {
                    string protoFile = selectedFiles[i];
                    float progress = (float)i / selectedFiles.Count;

                    EditorUtility.DisplayProgressBar(
                        "Compiling Proto",
                        $"Compiling {Path.GetFileName(protoFile)}... ({i + 1}/{selectedFiles.Count})",
                        progress
                    );

                    // 为每个 .proto 文件单独调用 protoc
                    string protoAbsolutePath = Path.Combine(projectRoot, protoFile);
                    string outputAbsolutePath = Path.Combine(projectRoot, outputPath);
                    string protoDir = Path.GetDirectoryName(protoAbsolutePath);

                    // 构建参数
                    StringBuilder args = new StringBuilder();
                    args.Append($"--csharp_out=\"{outputAbsolutePath}\" ");
                    args.Append($"--proto_path=\"{protoDir}\" ");

                    if (Directory.Exists(includePath))
                    {
                        args.Append($"--proto_path=\"{includePath}\" ");
                    }

                    args.Append($"\"{protoAbsolutePath}\"");

                    ProcessStartInfo startInfo = new ProcessStartInfo
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

                    using (Process process = Process.Start(startInfo))
                    {
                        string stdout = process.StandardOutput.ReadToEnd();
                        string stderr = process.StandardError.ReadToEnd();
                        process.WaitForExit();

                        if (process.ExitCode != 0 || !string.IsNullOrEmpty(stderr))
                        {
                            hasErrors = true;
                            allErrors.AppendLine($"[{Path.GetFileName(protoFile)}]");
                            allErrors.AppendLine(stderr);
                        }

                        if (!string.IsNullOrEmpty(stdout))
                        {
                            allOutput.AppendLine(stdout);
                        }
                    }
                }

                // 刷新 AssetDatabase
                AssetDatabase.Refresh();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            showOutput = true;

            if (hasErrors)
            {
                lastError = allErrors.ToString();
                lastOutput = allOutput.ToString();
                EditorUtility.DisplayDialog("Compilation Finished",
                    $"编译完成，有错误。\n\n成功: {selectedFiles.Count} 个文件已处理\n输出目录: {outputPath}",
                    "OK");
            }
            else
            {
                lastError = "";
                lastOutput = allOutput.Length > 0 ? allOutput.ToString() : "所有文件编译成功！";
                EditorUtility.DisplayDialog("Compilation Finished",
                    $"编译成功！\n\n已处理: {selectedFiles.Count} 个 .proto 文件\n输出目录: {outputPath}",
                    "OK");
            }
        }

        private string GetProtocPath()
        {
            // 优先使用用户设置的路径
            if (!string.IsNullOrEmpty(protocPath) && File.Exists(protocPath))
            {
                return protocPath;
            }

            // 尝试在常见位置查找
            string[] commonPaths =
            {
                Path.Combine(Application.dataPath, "../Tools/protoc.exe"),
#if UNITY_EDITOR_WIN
                @"C:\protoc\bin\protoc.exe",
#endif
            };

            foreach (string path in commonPaths)
            {
                if (File.Exists(path))
                {
                    return Path.GetFullPath(path);
                }
            }

            // 尝试系统 PATH
            try
            {
                ProcessStartInfo whichInfo = new ProcessStartInfo
                {
                    FileName = "where",
                    Arguments = "protoc",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };

                using (Process proc = Process.Start(whichInfo))
                {
                    string result = proc.StandardOutput.ReadToEnd().Trim();
                    proc.WaitForExit();
                    if (proc.ExitCode == 0 && !string.IsNullOrEmpty(result))
                    {
                        return result.Split('\n')[0].Trim();
                    }
                }
            }
            catch
            {
                // 忽略异常
            }

            return null;
        }
    }
}
