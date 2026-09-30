using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Framework.Core;
using TMPro;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;
using Slider = UnityEngine.UI.Slider;
using Toggle = UnityEngine.UI.Toggle;

namespace Framework.Editor
{
    /// <summary>
    /// 在常用UI组件的Inspector底部画绑定按钮，并把绑定结果生成为界面脚本
    /// </summary>
    internal static class UIViewBindingEditor
    {
        #region 内部类型

        /// <summary>
        /// 一个绑定的Button对应的回调信息
        /// </summary>
        private readonly struct ButtonHook
        {
            public readonly string Field;
            public readonly string Callback;

            public ButtonHook(string fieldName)
            {
                Field = "_" + fieldName;
                Callback = "On" + ToPascalCase(fieldName);
            }
        }

        #endregion

        private const string PrefabRoot = "Assets/Prefabs/UI";
        private const string ScriptRoot = "Assets/Scripts/UI";
        private const string AddMarkerBegin = "// <ui-bind:add>";
        private const string AddMarkerEnd = "// </ui-bind:add>";
        private const string RemoveMarkerBegin = "// <ui-bind:remove>";
        private const string RemoveMarkerEnd = "// </ui-bind:remove>";
        private const string LifecycleRegion = "#region 生命周期";
        private const string CallbackRegion = "#region UI回调";

        private static readonly HashSet<string> Reserved = new()
        {
            "base", "bool", "break", "byte", "case", "catch", "char", "class", "const", "continue",
            "default", "delegate", "do", "double", "else", "enum", "event", "false", "finally",
            "fixed", "float", "for", "foreach", "goto", "if", "in", "int", "interface", "internal",
            "is", "lock", "long", "namespace", "new", "null", "object", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
            "sizeof", "static", "string", "struct", "switch", "this", "throw", "true", "try",
            "typeof", "uint", "ulong", "ushort", "using", "virtual", "void", "while"
        };

        #region 组件按钮

        /// <summary>
        /// 把绑定按钮追加到UI Toolkit面板底部。基类没有返回面板时原样返回，让Unity走IMGUI分支。
        /// </summary>
        /// <param name="root">基类构建的Inspector面板</param>
        /// <param name="editor">正在绘制的Inspector</param>
        public static VisualElement Append(VisualElement root, UnityEditor.Editor editor)
        {
            if (root == null) return null;

            root.Add(new IMGUIContainer(() => Draw(editor)));
            return root;
        }

        /// <summary>
        /// 在IMGUI面板底部画绑定按钮。不在预制体编辑模式时什么都不画。
        /// </summary>
        /// <param name="editor">正在绘制的Inspector</param>
        public static void Draw(UnityEditor.Editor editor)
        {
            if (editor.targets.Length > 1) return;
            if (editor.target is not Component target) return;

            PrefabStage stage = PrefabStageUtility.GetPrefabStage(target.gameObject);
            if (stage == null) return;

            GameObject root = stage.prefabContentsRoot;
            UIView view = root.GetComponent<UIView>();
            UIView.Binding binding = view ? view.FindBinding(target) : null;

            EditorGUILayout.Space();
            if (binding != null)
            {
                EditorGUILayout.LabelField("界面字段", binding.FieldName);
                if (GUILayout.Button("解除绑定"))
                {
                    Undo.RecordObject(view, "解除UI控件绑定");
                    view.Bindings.Remove(binding);
                    EditorUtility.SetDirty(view);
                }

                return;
            }

            if (!GUILayout.Button("绑定到界面")) return;

            if (!view) view = Undo.AddComponent<UIView>(root);

            Undo.RecordObject(view, "绑定UI控件");
            view.Bindings.Add(new UIView.Binding(MakeFieldName(view, target), target));
            Sort(view);
            EditorUtility.SetDirty(view);
        }

        /// <summary>
        /// 给已经拖入控件但还没有字段名的引用补上默认字段名
        /// </summary>
        /// <param name="view">预制体根上的界面壳</param>
        public static void NameEmptyBindings(UIView view)
        {
            foreach (UIView.Binding binding in view.Bindings)
            {
                if (!binding.Target || !string.IsNullOrEmpty(binding.FieldName)) continue;

                Undo.RecordObject(view, "命名UI控件引用");
                binding.FieldName = MakeFieldName(view, binding.Target);
                EditorUtility.SetDirty(view);
            }
        }

        /// <summary>
        /// 把引用列表按组件类型归类，供Inspector上的排序按钮调用
        /// </summary>
        /// <param name="view">预制体根上的界面壳</param>
        public static void SortBindings(UIView view)
        {
            Undo.RecordObject(view, "排序UI控件引用");
            Sort(view);
            EditorUtility.SetDirty(view);
        }

        /// <summary>
        /// 同类型的控件排在一起，同类型内按字段名排序
        /// </summary>
        private static void Sort(UIView view)
        {
            view.Bindings.Sort(Compare);
        }

        private static int Compare(UIView.Binding left, UIView.Binding right)
        {
            string leftType = left.Target ? left.Target.GetType().Name : string.Empty;
            string rightType = right.Target ? right.Target.GetType().Name : string.Empty;

            int byType = string.CompareOrdinal(leftType, rightType);
            return byType != 0 ? byType : string.CompareOrdinal(left.FieldName, right.FieldName);
        }

        #endregion

        #region 代码生成

        /// <summary>
        /// 按当前绑定列表写出绑定、逻辑与界面数据三份脚本
        /// </summary>
        /// <param name="view">预制体根上的界面壳</param>
        public static void Generate(UIView view)
        {
            string prefabPath = ResolvePrefabPath(view);
            if (string.IsNullOrEmpty(prefabPath))
            {
                Debug.LogError("[UIFramework] 找不到该 UIView 所属的预制体，请在预制体编辑模式下生成");
                return;
            }

            if (!prefabPath.StartsWith(PrefabRoot + "/", StringComparison.Ordinal))
            {
                Debug.LogError($"[UIFramework] {prefabPath} 不在 {PrefabRoot} 下，无法推导脚本路径");
                return;
            }

            if (!Validate(view)) return;

            string className = Path.GetFileNameWithoutExtension(prefabPath);
            string relativeDir = Path.GetDirectoryName(prefabPath[(PrefabRoot.Length + 1)..])?.Replace('\\', '/') ?? string.Empty;
            string namespaceName = string.IsNullOrEmpty(relativeDir) ? "UI" : "UI." + relativeDir.Replace('/', '.');
            bool isWindow = view.Kind == UIView.ViewKind.Window;

            List<ButtonHook> hooks = CollectButtonHooks(view);

            string bindingPath = Path.Combine(ScriptRoot, "Binding", relativeDir, className + ".Binding.cs").Replace('\\', '/');
            WriteFile(bindingPath, BuildBinding(view, namespaceName, className, isWindow));

            string logicPath = Path.Combine(ScriptRoot, "Logic", relativeDir, className + ".cs");
            WriteFile(logicPath, File.Exists(logicPath)
                ? UpdateLogic(File.ReadAllText(logicPath), hooks, logicPath)
                : BuildLogic(namespaceName, className, hooks));

            string propertiesPath = Path.Combine(ScriptRoot, "Properties", relativeDir, className + "Properties.cs");
            if (!File.Exists(propertiesPath))
                WriteFile(propertiesPath, BuildProperties(namespaceName, className, isWindow));

            Undo.RecordObject(view, "完成UI绑定");
            view.ControllerTypeName = $"{namespaceName}.{className}, {ResolveAssemblyName(bindingPath)}";
            EditorUtility.SetDirty(view);

            // 预制体编辑模式下的改动随预制体保存落盘，直接选中预制体资源时需要立刻写回
            if (PrefabStageUtility.GetPrefabStage(view.gameObject) == null)
                AssetDatabase.SaveAssets();

            AssetDatabase.Refresh();

            string summary = $"已生成 {namespaceName}.{className}\n{view.Bindings.Count} 个控件绑定，界面ID 为 {className}";
            Debug.Log($"[UIFramework] {summary}");

            // 预制体编辑模式下的Scene视图就是预制体预览窗口
            SceneView preview = SceneView.lastActiveSceneView;
            if (preview) preview.ShowNotification(new GUIContent(summary), 4d);
        }

        /// <summary>
        /// 生成前检查每条引用都有控件和字段名，空槽会写出无法编译的代码
        /// </summary>
        private static bool Validate(UIView view)
        {
            for (int i = 0; i < view.Bindings.Count; i++)
            {
                UIView.Binding binding = view.Bindings[i];
                if (!binding.Target)
                {
                    Debug.LogError($"[UIFramework] 第 {i + 1} 条引用还没有拖入控件，先补齐再生成");
                    return false;
                }

                if (string.IsNullOrEmpty(binding.FieldName))
                {
                    Debug.LogError($"[UIFramework] {binding.Target.name} 上 {binding.Target.GetType().Name} 的引用没有字段名，先填好再生成");
                    return false;
                }
            }

            return true;
        }

        private static string BuildBinding(UIView view, string namespaceName, string className, bool isWindow)
        {
            SortedSet<string> usings = new(StringComparer.Ordinal) { "Framework.Core" };
            usings.Add(isWindow ? "Framework.Window" : "Framework.Panel");

            foreach (UIView.Binding binding in view.Bindings)
                usings.Add(binding.Target.GetType().Namespace);

            StringBuilder builder = new();
            builder.AppendLine("// <auto-generated>");
            builder.AppendLine("//     此文件为自动生成文件，请勿手改，重新绑定会覆盖。");
            builder.AppendLine("// </auto-generated>");
            builder.AppendLine();

            foreach (string name in usings)
                builder.AppendLine($"using {name};");

            builder.AppendLine();
            builder.AppendLine($"namespace {namespaceName}");
            builder.AppendLine("{");
            builder.AppendLine("    /// <summary>");
            builder.AppendLine($"    /// {className} 的控件绑定");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine($"    public partial class {className} : {(isWindow ? "WindowController" : "PanelController")}, UIView.IBindable");
            builder.AppendLine("    {");

            foreach (UIView.Binding binding in view.Bindings)
                builder.AppendLine($"        private {binding.Target.GetType().Name} _{binding.FieldName};");

            builder.AppendLine();
            builder.AppendLine("        /// <summary>");
            builder.AppendLine("        /// 由 UIView 在挂载后调用，写入预制体上绑定的控件");
            builder.AppendLine("        /// </summary>");
            builder.AppendLine("        public void Bind(UIView view)");
            builder.AppendLine("        {");

            foreach (UIView.Binding binding in view.Bindings)
            {
                string type = binding.Target.GetType().Name;
                builder.AppendLine($"            _{binding.FieldName} = ({type})view.GetBinding(\"{binding.FieldName}\");");
            }

            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static string BuildLogic(string namespaceName, string className, List<ButtonHook> hooks)
        {
            StringBuilder builder = new();
            builder.AppendLine($"namespace {namespaceName}");
            builder.AppendLine("{");
            builder.AppendLine("    /// <summary>");
            builder.AppendLine($"    /// {className} 的界面逻辑");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine($"    public partial class {className}");
            builder.AppendLine("    {");
            builder.AppendLine($"        {LifecycleRegion}");
            builder.AppendLine();
            builder.AppendLine("        protected override void Init()");
            builder.AppendLine("        {");
            builder.AppendLine("            // TODO: 初始化界面数据与控件状态");
            builder.AppendLine($"            {AddMarkerBegin}");
            builder.Append(BuildListenerLines(hooks, true));
            builder.AppendLine($"            {AddMarkerEnd}");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        protected override void AddListener()");
            builder.AppendLine("        {");
            builder.AppendLine("            // TODO: 订阅界面以外的事件");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        protected override void RemoveListener()");
            builder.AppendLine("        {");
            builder.AppendLine("            // TODO: 取消订阅，并交给基类清理控制器事件");
            builder.AppendLine($"            {RemoveMarkerBegin}");
            builder.Append(BuildListenerLines(hooks, false));
            builder.AppendLine($"            {RemoveMarkerEnd}");
            builder.AppendLine("            base.RemoveListener();");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.Append(BuildUpdateView());
            builder.AppendLine();
            builder.AppendLine("        #endregion");

            if (hooks.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine($"        {CallbackRegion}");

                foreach (ButtonHook hook in hooks)
                {
                    builder.AppendLine();
                    builder.Append(BuildCallback(hook));
                }

                builder.AppendLine();
                builder.AppendLine("        #endregion");
            }

            builder.AppendLine("    }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static string UpdateLogic(string text, List<ButtonHook> hooks, string logicPath)
        {
            text = ReplaceBetween(text, AddMarkerBegin, AddMarkerEnd, BuildListenerLines(hooks, true), logicPath);
            text = ReplaceBetween(text, RemoveMarkerBegin, RemoveMarkerEnd, BuildListenerLines(hooks, false), logicPath);

            if (!text.Contains("void UpdateView(", StringComparison.Ordinal))
                text = InsertBeforeRegionEnd(text, LifecycleRegion, BuildUpdateView());

            foreach (ButtonHook hook in hooks)
            {
                if (text.Contains($"void {hook.Callback}(", StringComparison.Ordinal)) continue;
                text = InsertBeforeRegionEnd(text, CallbackRegion, BuildCallback(hook));
            }

            return text;
        }

        private static string BuildProperties(string namespaceName, string className, bool isWindow)
        {
            StringBuilder builder = new();
            builder.AppendLine("using System;");
            builder.AppendLine(isWindow ? "using Framework.Window;" : "using Framework.Panel;");
            builder.AppendLine();
            builder.AppendLine($"namespace {namespaceName}");
            builder.AppendLine("{");
            builder.AppendLine("    /// <summary>");
            builder.AppendLine($"    /// {className} 的界面数据，打开界面时传入");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine("    [Serializable]");

            if (isWindow)
            {
                builder.AppendLine($"    public class {className}Properties : WindowProperties");
                builder.AppendLine("    {");
                builder.AppendLine($"        public {className}Properties(WindowPriority priority, bool hideOnForegroundLost, bool isPopup)");
                builder.AppendLine("            : base(priority, hideOnForegroundLost, isPopup)");
                builder.AppendLine("        {");
                builder.AppendLine("            // TODO: 补上该界面需要的数据字段");
                builder.AppendLine("        }");
            }
            else
            {
                builder.AppendLine($"    public class {className}Properties : PanelProperties");
                builder.AppendLine("    {");
                builder.AppendLine($"        public {className}Properties(PanelPriority priority) : base(priority)");
                builder.AppendLine("        {");
                builder.AppendLine("            // TODO: 补上该界面需要的数据字段");
                builder.AppendLine("        }");
            }

            builder.AppendLine("    }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        #endregion

        #region 生成片段

        private static string BuildUpdateView()
        {
            StringBuilder builder = new();
            builder.AppendLine("        protected override void UpdateView()");
            builder.AppendLine("        {");
            builder.AppendLine("            // TODO: 用界面属性刷新控件显示");
            builder.AppendLine("        }");
            return builder.ToString();
        }

        private static string BuildCallback(ButtonHook hook)
        {
            StringBuilder builder = new();
            builder.AppendLine($"        private void {hook.Callback}()");
            builder.AppendLine("        {");
            builder.AppendLine($"            // TODO: {hook.Field} 点击后的逻辑");
            builder.AppendLine("        }");
            return builder.ToString();
        }

        private static string BuildListenerLines(List<ButtonHook> hooks, bool add)
        {
            StringBuilder builder = new();
            string method = add ? "AddListener" : "RemoveListener";

            foreach (ButtonHook hook in hooks)
                builder.AppendLine($"            {hook.Field}.onClick.{method}({hook.Callback});");

            return builder.ToString();
        }

        private static List<ButtonHook> CollectButtonHooks(UIView view)
        {
            List<ButtonHook> hooks = new();

            foreach (UIView.Binding binding in view.Bindings)
            {
                if (binding.Target is Button)
                    hooks.Add(new ButtonHook(binding.FieldName));
            }

            return hooks;
        }

        #endregion

        #region 文本处理

        private static string ReplaceBetween(string text, string begin, string end, string body, string logicPath)
        {
            int beginIndex = text.IndexOf(begin, StringComparison.Ordinal);
            int endIndex = text.IndexOf(end, StringComparison.Ordinal);
            if (beginIndex < 0 || endIndex < beginIndex)
            {
                Debug.LogWarning($"[UIFramework] {logicPath} 里缺少 {begin} 标记，按钮监听没有更新");
                return text;
            }

            int bodyStart = text.IndexOf('\n', beginIndex) + 1;
            int bodyEnd = text.LastIndexOf('\n', endIndex) + 1;
            return text[..bodyStart] + body + text[bodyEnd..];
        }

        /// <summary>
        /// 把一段方法插入到指定region的结尾之前。region不存在时补一个新的region。
        /// </summary>
        private static string InsertBeforeRegionEnd(string text, string region, string block)
        {
            int regionIndex = text.IndexOf(region, StringComparison.Ordinal);
            if (regionIndex < 0)
            {
                int classEnd = FindClassEnd(text);
                return text[..classEnd] + $"\n        {region}\n\n{block}\n        #endregion\n" + text[classEnd..];
            }

            int endIndex = text.IndexOf("#endregion", regionIndex, StringComparison.Ordinal);
            if (endIndex < 0)
            {
                Debug.LogWarning($"[UIFramework] {region} 没有配套的 #endregion，生成的方法追加到了类末尾");
                int classEnd = FindClassEnd(text);
                return text[..classEnd] + block + text[classEnd..];
            }

            int lineStart = text.LastIndexOf('\n', endIndex) + 1;
            return text[..lineStart] + block + "\n" + text[lineStart..];
        }

        /// <summary>
        /// 定位类的闭合大括号所在行，用于在类末尾追加内容
        /// </summary>
        private static int FindClassEnd(string text)
        {
            int namespaceEnd = text.LastIndexOf("\n}", StringComparison.Ordinal);
            int classEnd = text.LastIndexOf("\n    }", namespaceEnd < 0 ? text.Length - 1 : namespaceEnd, StringComparison.Ordinal);
            return classEnd < 0 ? text.Length : classEnd + 1;
        }

        #endregion

        #region 命名与路径

        private static string MakeFieldName(UIView view, Component target)
        {
            string baseName = ToCamelCase(target.gameObject.name);
            if (baseName.Length == 0) baseName = ToCamelCase(target.GetType().Name);
            if (IsAvailable(view, baseName)) return baseName;

            string withType = baseName + target.GetType().Name;
            if (IsAvailable(view, withType)) return withType;

            for (int i = 2; ; i++)
            {
                string numbered = withType + i;
                if (IsAvailable(view, numbered)) return numbered;
            }
        }

        private static bool IsAvailable(UIView view, string fieldName)
        {
            if (Reserved.Contains(fieldName)) return false;

            foreach (UIView.Binding binding in view.Bindings)
            {
                if (binding.FieldName == fieldName) return false;
            }

            return true;
        }

        /// <summary>
        /// 把手填的字段名整理成合法标识符，整理不出内容时返回空串
        /// </summary>
        /// <param name="source">Inspector里填入的字段名</param>
        public static string SanitizeFieldName(string source) => ToCamelCase(source);

        /// <summary>
        /// 把节点名整理成合法的camelCase标识符
        /// </summary>
        private static string ToCamelCase(string source)
        {
            StringBuilder builder = new();
            bool upperNext = false;

            foreach (char c in source)
            {
                if (!char.IsLetterOrDigit(c))
                {
                    upperNext = builder.Length > 0;
                    continue;
                }

                if (builder.Length == 0 && char.IsDigit(c)) continue;

                builder.Append(upperNext ? char.ToUpperInvariant(c) : c);
                upperNext = false;
            }

            if (builder.Length == 0) return string.Empty;

            builder[0] = char.ToLowerInvariant(builder[0]);
            return builder.ToString();
        }

        private static string ToPascalCase(string source)
        {
            if (string.IsNullOrEmpty(source)) return source;
            return char.ToUpperInvariant(source[0]) + source[1..];
        }

        /// <summary>
        /// 取出该界面的ID。ID由预制体名推导：资源定位名是 <c>UI_预制体名</c>，与生成的类名一致。
        /// </summary>
        /// <param name="view">预制体根上的界面壳</param>
        public static string ResolveScreenId(UIView view)
        {
            string prefabPath = ResolvePrefabPath(view);
            return string.IsNullOrEmpty(prefabPath) ? string.Empty : Path.GetFileNameWithoutExtension(prefabPath);
        }

        /// <summary>
        /// 取出生成脚本所属的程序集名，运行时按程序集限定名直接解析类型
        /// </summary>
        /// <param name="scriptPath">生成脚本的资源路径</param>
        private static string ResolveAssemblyName(string scriptPath)
        {
            string assembly = CompilationPipeline.GetAssemblyNameFromScriptPath(scriptPath);
            return string.IsNullOrEmpty(assembly) ? "Assembly-CSharp" : Path.GetFileNameWithoutExtension(assembly);
        }

        private static string ResolvePrefabPath(UIView view)
        {
            PrefabStage stage = PrefabStageUtility.GetPrefabStage(view.gameObject);
            if (stage != null) return stage.assetPath;

            string assetPath = AssetDatabase.GetAssetPath(view.gameObject);
            if (!string.IsNullOrEmpty(assetPath)) return assetPath;

            return PrefabUtility.IsPartOfPrefabInstance(view)
                ? PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(view)
                : null;
        }

        private static void WriteFile(string path, string content)
        {
            path = path.Replace('\\', '/');
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, content, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(path);
        }

        #endregion
    }

    /// <summary>
    /// 界面壳的Inspector，用来核对字段名并触发代码生成
    /// </summary>
    [CustomEditor(typeof(UIView))]
    internal sealed class UIViewInspector : UnityEditor.Editor
    {
        private const string RemoveLabel = "×";

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("kind"));

            string screenId = UIViewBindingEditor.ResolveScreenId((UIView)target);
            EditorGUILayout.LabelField("界面ID", string.IsNullOrEmpty(screenId) ? "未找到所属预制体" : screenId);

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("controllerTypeName"));

            SerializedProperty bindings = serializedObject.FindProperty("bindings");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"控件引用（{bindings.arraySize}）", EditorStyles.boldLabel);

            int removeIndex = -1;

            for (int i = 0; i < bindings.arraySize; i++)
            {
                SerializedProperty entry = bindings.GetArrayElementAtIndex(i);
                SerializedProperty fieldName = entry.FindPropertyRelative("fieldName");

                using (new EditorGUILayout.HorizontalScope())
                {
                    string previous = fieldName.stringValue;

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.DelayedTextField(fieldName, GUIContent.none);
                    if (EditorGUI.EndChangeCheck())
                    {
                        string cleaned = UIViewBindingEditor.SanitizeFieldName(fieldName.stringValue);
                        fieldName.stringValue = cleaned.Length > 0 ? cleaned : previous;
                    }

                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("target"), GUIContent.none);

                    if (GUILayout.Button(RemoveLabel, GUILayout.Width(24f)))
                        removeIndex = i;
                }
            }

            if (removeIndex >= 0) bindings.DeleteArrayElementAtIndex(removeIndex);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("添加组件引用"))
                {
                    bindings.InsertArrayElementAtIndex(bindings.arraySize);

                    // 插入的元素会复制上一条，清空后等待拖入控件
                    SerializedProperty added = bindings.GetArrayElementAtIndex(bindings.arraySize - 1);
                    added.FindPropertyRelative("fieldName").stringValue = string.Empty;
                    added.FindPropertyRelative("target").objectReferenceValue = null;
                }

                using (new EditorGUI.DisabledScope(bindings.arraySize == 0))
                {
                    if (GUILayout.Button("按类型排序"))
                        UIViewBindingEditor.SortBindings((UIView)target);
                }
            }

            serializedObject.ApplyModifiedProperties();

            // 拖入控件后补上默认字段名，拖入的可以是 Transform 等任意组件
            UIViewBindingEditor.NameEmptyBindings((UIView)target);

            EditorGUILayout.Space();
            if (bindings.arraySize == 0)
            {
                EditorGUILayout.HelpBox("在子节点的组件 Inspector 底部点「绑定到界面」，或在这里加引用槽后拖入控件。", MessageType.Info);
                return;
            }

            if (GUILayout.Button("生成代码", GUILayout.Height(24f)))
                UIViewBindingEditor.Generate((UIView)target);
        }
    }

    #region 组件Inspector

    [CustomEditor(typeof(Button), true)]
    [CanEditMultipleObjects]
    internal sealed class UIBindButtonEditor : UnityEditor.UI.ButtonEditor
    {
        public override VisualElement CreateInspectorGUI() => UIViewBindingEditor.Append(base.CreateInspectorGUI(), this);

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            UIViewBindingEditor.Draw(this);
        }
    }

    [CustomEditor(typeof(Toggle), true)]
    [CanEditMultipleObjects]
    internal sealed class UIBindToggleEditor : UnityEditor.UI.ToggleEditor
    {
        public override VisualElement CreateInspectorGUI() => UIViewBindingEditor.Append(base.CreateInspectorGUI(), this);

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            UIViewBindingEditor.Draw(this);
        }
    }

    [CustomEditor(typeof(Slider), true)]
    [CanEditMultipleObjects]
    internal sealed class UIBindSliderEditor : UnityEditor.UI.SliderEditor
    {
        public override VisualElement CreateInspectorGUI() => UIViewBindingEditor.Append(base.CreateInspectorGUI(), this);

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            UIViewBindingEditor.Draw(this);
        }
    }

    [CustomEditor(typeof(Scrollbar), true)]
    [CanEditMultipleObjects]
    internal sealed class UIBindScrollbarEditor : UnityEditor.UI.ScrollbarEditor
    {
        public override VisualElement CreateInspectorGUI() => UIViewBindingEditor.Append(base.CreateInspectorGUI(), this);

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            UIViewBindingEditor.Draw(this);
        }
    }

    [CustomEditor(typeof(ScrollRect), true)]
    [CanEditMultipleObjects]
    internal sealed class UIBindScrollRectEditor : UnityEditor.UI.ScrollRectEditor
    {
        public override VisualElement CreateInspectorGUI() => UIViewBindingEditor.Append(base.CreateInspectorGUI(), this);

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            UIViewBindingEditor.Draw(this);
        }
    }

    [CustomEditor(typeof(Image), true)]
    [CanEditMultipleObjects]
    internal sealed class UIBindImageEditor : UnityEditor.UI.ImageEditor
    {
        public override VisualElement CreateInspectorGUI() => UIViewBindingEditor.Append(base.CreateInspectorGUI(), this);

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            UIViewBindingEditor.Draw(this);
        }
    }

    [CustomEditor(typeof(RawImage), true)]
    [CanEditMultipleObjects]
    internal sealed class UIBindRawImageEditor : UnityEditor.UI.RawImageEditor
    {
        public override VisualElement CreateInspectorGUI() => UIViewBindingEditor.Append(base.CreateInspectorGUI(), this);

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            UIViewBindingEditor.Draw(this);
        }
    }

    [CustomEditor(typeof(Text), true)]
    [CanEditMultipleObjects]
    internal sealed class UIBindTextEditor : UnityEditor.UI.TextEditor
    {
        public override VisualElement CreateInspectorGUI() => UIViewBindingEditor.Append(base.CreateInspectorGUI(), this);

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            UIViewBindingEditor.Draw(this);
        }
    }

    [CustomEditor(typeof(InputField), true)]
    [CanEditMultipleObjects]
    internal sealed class UIBindInputFieldEditor : UnityEditor.UI.InputFieldEditor
    {
        public override VisualElement CreateInspectorGUI() => UIViewBindingEditor.Append(base.CreateInspectorGUI(), this);

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            UIViewBindingEditor.Draw(this);
        }
    }

    [CustomEditor(typeof(Dropdown), true)]
    [CanEditMultipleObjects]
    internal sealed class UIBindDropdownEditor : UnityEditor.UI.DropdownEditor
    {
        public override VisualElement CreateInspectorGUI() => UIViewBindingEditor.Append(base.CreateInspectorGUI(), this);

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            UIViewBindingEditor.Draw(this);
        }
    }

    [CustomEditor(typeof(CanvasGroup), true)]
    [CanEditMultipleObjects]
    internal sealed class UIBindCanvasGroupEditor : UnityEditor.UI.CanvasGroupInspector
    {
        public override VisualElement CreateInspectorGUI() => UIViewBindingEditor.Append(base.CreateInspectorGUI(), this);

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            UIViewBindingEditor.Draw(this);
        }
    }

    [CustomEditor(typeof(TextMeshProUGUI), true)]
    [CanEditMultipleObjects]
    internal sealed class UIBindTmpTextEditor : TMPro.EditorUtilities.TMP_EditorPanelUI
    {
        public override VisualElement CreateInspectorGUI() => UIViewBindingEditor.Append(base.CreateInspectorGUI(), this);

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            UIViewBindingEditor.Draw(this);
        }
    }

    [CustomEditor(typeof(TMP_InputField), true)]
    [CanEditMultipleObjects]
    internal sealed class UIBindTmpInputFieldEditor : TMPro.EditorUtilities.TMP_InputFieldEditor
    {
        public override VisualElement CreateInspectorGUI() => UIViewBindingEditor.Append(base.CreateInspectorGUI(), this);

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            UIViewBindingEditor.Draw(this);
        }
    }

    [CustomEditor(typeof(TMP_Dropdown), true)]
    [CanEditMultipleObjects]
    internal sealed class UIBindTmpDropdownEditor : TMPro.EditorUtilities.DropdownEditor
    {
        public override VisualElement CreateInspectorGUI() => UIViewBindingEditor.Append(base.CreateInspectorGUI(), this);

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            UIViewBindingEditor.Draw(this);
        }
    }

    #endregion
}
