using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

namespace Framework.Editor
{
    public class UICodeGenerator : EditorWindow
    {
        private enum UIType
        {
            Panel,
            Window
        }

        private class FieldInfo
        {
            public string fieldName;
            public string fieldType;
            public GameObject gameObject;
            public bool isIncluded = true;
        }

        private UIType selectedUIType = UIType.Panel;
        private string uiID = "";
        private string namespaceName = "";
        private string savePath = "Assets/Scripts/UI";
        private bool generateProperties = true;
        private GameObject uiPrefab;
        private string prefixTag = "m_";
        private Vector2 scrollPosition;
        private List<FieldInfo> scannedFields = new();
        private string prefabRootPath = "Assets/Resources/UI";
        private string scriptRootPath = "Assets/Scripts/UI";
        private string pendingControllerName = "";
        private bool waitingForCompilation = false;

        [MenuItem("Tools/UI Framework/UI Code Generator")]
        public static void ShowWindow()
        {
            GetWindow<UICodeGenerator>("UI Code Generator");
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (waitingForCompilation && !EditorApplication.isCompiling)
            {
                waitingForCompilation = false;
                if (!string.IsNullOrEmpty(pendingControllerName))
                {
                    AttachScriptToPrefab(pendingControllerName);
                    pendingControllerName = "";
                }
            }
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                GUILayout.Label("UI Code Generator", EditorStyles.boldLabel);
                GUILayout.Space(10);

                EditorGUILayout.LabelField("Path Configuration", EditorStyles.boldLabel);
                GUILayout.Space(5);

                selectedUIType = (UIType)EditorGUILayout.EnumPopup("UI Type", selectedUIType);
                GUILayout.Space(5);

                uiID = EditorGUILayout.TextField("UI ID", uiID);
                GUILayout.Space(5);

                namespaceName = EditorGUILayout.TextField("Namespace", namespaceName);
                GUILayout.Space(5);

                EditorGUILayout.LabelField("Save Path", savePath, EditorStyles.textField);
                GUILayout.Space(5);

                generateProperties = EditorGUILayout.Toggle("Generate Properties", generateProperties);
                GUILayout.Space(10);

                EditorGUILayout.LabelField("Prefab Scanning", EditorStyles.boldLabel);
                GUILayout.Space(5);

                EditorGUI.BeginChangeCheck();
                uiPrefab = (GameObject)EditorGUILayout.ObjectField("UI Prefab", uiPrefab, typeof(GameObject), false);
                if (EditorGUI.EndChangeCheck())
                {
                    AutoParsePrefab();
                }
                GUILayout.Space(5);

                prefixTag = EditorGUILayout.TextField("Field Prefix", prefixTag);
                GUILayout.Space(5);

                if (GUILayout.Button("Scan Prefab"))
                {
                    ScanPrefab();
                }

                GUILayout.Space(10);

                if (scannedFields.Count > 0)
                {
                    EditorGUILayout.LabelField("Scanned Fields", EditorStyles.boldLabel);
                    GUILayout.Space(5);

                    scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
                    foreach (var field in scannedFields)
                    {
                        EditorGUILayout.BeginHorizontal();
                        field.isIncluded = EditorGUILayout.Toggle(field.isIncluded, GUILayout.Width(20));
                        EditorGUILayout.LabelField(field.fieldType, GUILayout.Width(120));
                        EditorGUILayout.LabelField(field.fieldName);
                        EditorGUILayout.ObjectField(field.gameObject, typeof(GameObject), false);
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndScrollView();
                }

                GUILayout.FlexibleSpace();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Generate Code", GUILayout.Height(30)))
                {
                    GenerateCode();
                }
                if (GUILayout.Button("Attach Script & Link Fields", GUILayout.Height(30)))
                {
                    AttachScriptWithWait();
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void AutoParsePrefab()
        {
            if (!uiPrefab)
            {
                return;
            }

            string prefabPath = AssetDatabase.GetAssetPath(uiPrefab);
            string prefabFileName = Path.GetFileNameWithoutExtension(prefabPath);

            uiID = prefabFileName;

            if (prefabPath.StartsWith(prefabRootPath))
            {
                string relativePath = prefabPath.Substring(prefabRootPath.Length);
                string directoryPath = Path.GetDirectoryName(relativePath);
                
                savePath = !string.IsNullOrEmpty(directoryPath) ? Path.Combine(scriptRootPath, directoryPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) : scriptRootPath;
            }
            else
            {
                savePath = scriptRootPath;
            }
        }

        private void ScanPrefab()
        {
            if (uiPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a UI prefab!", "OK");
                return;
            }

            scannedFields.Clear();

            Transform[] allChildren = uiPrefab.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in allChildren)
            {
                if (child.name.StartsWith(prefixTag))
                {
                    var fieldInfo = new FieldInfo();
                    fieldInfo.gameObject = child.gameObject;
                    fieldInfo.fieldName = child.name;

                    var component = GetPrimaryComponent(child.gameObject);
                    fieldInfo.fieldType = component.GetType().Name;

                    scannedFields.Add(fieldInfo);
                }
            }
        }

        private Component GetPrimaryComponent(GameObject obj)
        {
            var button = obj.GetComponent<Button>();
            if (button) return button;

            var textMeshPro = obj.GetComponent<TextMeshProUGUI>();
            if (textMeshPro) return textMeshPro;

            var text = obj.GetComponent<Text>();
            if (text) return text;

            var image = obj.GetComponent<Image>();
            if (image) return image;
            
            var textMeshProInput = obj.GetComponent<TMP_InputField>();
            if (textMeshProInput) return textMeshProInput;

            var inputField = obj.GetComponent<InputField>();
            if (inputField) return inputField;

            var toggle = obj.GetComponent<Toggle>();
            if (toggle) return toggle;

            var slider = obj.GetComponent<Slider>();
            if (slider) return slider;

            var scrollRect = obj.GetComponent<ScrollRect>();
            if (scrollRect) return scrollRect;

            var textMeshProDropdown = obj.GetComponent<TMP_Dropdown>();
            if (textMeshProDropdown) return textMeshProDropdown;
            
            var dropdown = obj.GetComponent<Dropdown>();
            if (dropdown) return dropdown;

            var canvasGroup = obj.GetComponent<CanvasGroup>();
            if (canvasGroup) return canvasGroup;

            var rectTransform = obj.GetComponent<RectTransform>();
            if (rectTransform) return rectTransform;

            return obj.transform;
        }

        private void GenerateCode()
        {
            if (string.IsNullOrEmpty(uiID))
            {
                EditorUtility.DisplayDialog("Error", "Please enter a UI ID!", "OK");
                return;
            }

            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            switch (selectedUIType)
            {
                case UIType.Panel:
                    GeneratePanelCode();
                    break;
                case UIType.Window:
                    GenerateWindowCode();
                    break;
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", "UI code generated successfully!", "OK");
        }

        private void AttachScriptWithWait()
        {
            if (uiPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a UI prefab!", "OK");
                return;
            }

            string controllerName = selectedUIType == UIType.Panel ? uiID + "PanelController" : uiID + "WindowController";
            
            pendingControllerName = controllerName;
            waitingForCompilation = true;
            EditorUtility.DisplayDialog("Info", "Waiting for compilation to attach script...", "OK");
        }

        private void AttachScriptToPrefab(string controllerName)
        {
            // 查找脚本类型
            MonoScript script = FindScriptByName(controllerName);
            if (script == null)
            {
                EditorUtility.DisplayDialog("Error", "Script not found! Make sure the code has been generated.", "OK");
                return;
            }

            System.Type scriptType = script.GetClass();
            if (scriptType == null)
            {
                EditorUtility.DisplayDialog("Error", "Script type not found! Make sure the script has been compiled.", "OK");
                return;
            }

            // 获取预制体路径
            string prefabPath = AssetDatabase.GetAssetPath(uiPrefab);
            GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);

            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Cannot load prefab!", "OK");
                return;
            }

            try
            {
                // 添加或获取组件
                Component existingComponent = prefab.GetComponent(scriptType);
                if (existingComponent == null)
                {
                    prefab.AddComponent(scriptType);
                }

                // 关联字段
                SerializedObject serializedObject = new SerializedObject(prefab.GetComponent(scriptType));
                SerializedProperty iterator = serializedObject.GetIterator();
                
                while (iterator.NextVisible(true))
                {
                    // 设置UI Controller ID
                    if (iterator.name == "uiControllerID")
                    {
                        iterator.stringValue = uiID;
                    }
                    // 设置对象引用
                    else if (iterator.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        foreach (var field in scannedFields)
                        {
                            if (field.isIncluded && iterator.name == field.fieldName)
                            {
                                var component = GetPrimaryComponent(field.gameObject);
                                iterator.objectReferenceValue = component;
                                break;
                            }
                        }
                    }
                }

                serializedObject.ApplyModifiedProperties();

                // 保存预制体
                PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
                EditorUtility.DisplayDialog("Success", "Script attached and fields linked successfully!", "OK");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        private MonoScript FindScriptByName(string scriptName)
        {
            string[] guids = AssetDatabase.FindAssets($"{scriptName} t:script");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null && script.name == scriptName)
                {
                    return script;
                }
            }
            return null;
        }

        private void GeneratePanelCode()
        {
            string controllerName = uiID + "PanelController";
            string propertiesName = uiID + "PanelProperties";

            string controllerPath = Path.Combine(savePath, controllerName + ".cs");
            string propertiesFolder = Path.Combine(savePath, "Properties");
            string propertiesPath = Path.Combine(propertiesFolder, propertiesName + ".cs");

            if (generateProperties && !Directory.Exists(propertiesFolder))
            {
                Directory.CreateDirectory(propertiesFolder);
            }

            GeneratePanelControllerCode(controllerPath, controllerName, propertiesName);

            if (generateProperties)
            {
                GeneratePanelPropertiesCode(propertiesPath, propertiesName);
            }
        }

        private void GenerateWindowCode()
        {
            string controllerName = uiID + "WindowController";
            string propertiesName = uiID + "WindowProperties";

            string controllerPath = Path.Combine(savePath, controllerName + ".cs");
            string propertiesFolder = Path.Combine(savePath, "Properties");
            string propertiesPath = Path.Combine(propertiesFolder, propertiesName + ".cs");

            if (generateProperties && !Directory.Exists(propertiesFolder))
            {
                Directory.CreateDirectory(propertiesFolder);
            }

            GenerateWindowControllerCode(controllerPath, controllerName, propertiesName);

            if (generateProperties)
            {
                GenerateWindowPropertiesCode(propertiesPath, propertiesName);
            }
        }

        private string GenerateFieldsCode()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var field in scannedFields)
            {
                if (!field.isIncluded) continue;

                sb.AppendLine();
                sb.Append("    ");
                if (!string.IsNullOrEmpty(namespaceName))
                {
                    sb.Append("    ");
                }
                sb.AppendLine("[SerializeField]");
                sb.Append("    ");
                if (!string.IsNullOrEmpty(namespaceName))
                {
                    sb.Append("    ");
                }
                sb.AppendLine($"private {field.fieldType} {field.fieldName};");
            }

            return sb.ToString();
        }

        private void GeneratePanelControllerCode(string path, string controllerName, string propertiesName)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using UnityEngine.UI;");
            sb.AppendLine("using TMPro;");
            sb.AppendLine("using Framework.Panel;");
            sb.AppendLine();
            
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");
            }
            
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.AppendLine($"public class {controllerName} : PanelController");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.AppendLine("{");

            sb.Append(GenerateFieldsCode());

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine("protected override void AddListener()");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine("{");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.Append("    ");
            sb.AppendLine("// TODO: 在此处添加事件监听处理");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine("}");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine("protected override void RemoveListener()");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine("{");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.Append("    ");
            sb.AppendLine("// TODO: 在此处事件删除处理");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine("    base.RemoveListener();");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine("}");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine("protected override void OnPropertyChange()");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine("{");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.Append("    ");
            sb.AppendLine("// TODO: UI属性更新");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine("}");

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.AppendLine("}");

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine("}");
            }

            File.WriteAllText(path, sb.ToString());
        }

        private void GeneratePanelPropertiesCode(string path, string propertiesName)
        {
            StringBuilder sb = new StringBuilder();

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");
            }

            sb.AppendLine("using System;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using Framework.Panel;");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.AppendLine("[Serializable]");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.AppendLine($"public class {propertiesName} : PanelProperties");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.AppendLine("{");

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine($"public {propertiesName}() : base(PanelPriority.None)");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine("{");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine("}");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine("// TODO: 在此处添加自定义属性");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine("// Example:");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine("// [SerializeField]");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine("// private string myProperty;");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine("// public string MyProperty { get => myProperty; set => myProperty = value; }");

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.AppendLine("}");

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine("}");
            }

            File.WriteAllText(path, sb.ToString());
        }

        private void GenerateWindowControllerCode(string path, string controllerName, string propertiesName)
        {
            StringBuilder sb = new StringBuilder();

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");
            }

            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using UnityEngine.UI;");
            sb.AppendLine("using TMPro;");
            sb.AppendLine("using Framework.Window;");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.AppendLine($"public class {controllerName} : WindowController");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.AppendLine("{");

            sb.Append(GenerateFieldsCode());

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine("protected override void AddListener()");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine("{");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.Append("    ");
            sb.AppendLine("// TODO: 在此处添加事件监听处理");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine("}");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine("protected override void RemoveListener()");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine("{");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.Append("    ");
            sb.AppendLine("// TODO: 在此处添加事件删除处理");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine("    base.RemoveListener();");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine("}");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine("protected override void OnPropertyChange()");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine("{");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.Append("    ");
            sb.AppendLine("// TODO: UI属性更新");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine("}");

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.AppendLine("}");

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine("}");
            }

            File.WriteAllText(path, sb.ToString());
        }

        private void GenerateWindowPropertiesCode(string path, string propertiesName)
        {
            StringBuilder sb = new StringBuilder();

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");
            }

            sb.AppendLine("using System;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using Framework.Window;");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.AppendLine("[Serializable]");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.AppendLine($"public class {propertiesName} : WindowProperties");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.AppendLine("{");

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine($"public {propertiesName}() : base(WindowPriority.ForceForeground, true, false)");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine("{");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine("}");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine("// TODO: 在此处添加自定义属性");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine("// Example:");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine("// [SerializeField]");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine("// private string myProperty;");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.Append("    ");
            sb.AppendLine("// public string MyProperty { get => myProperty; set => myProperty = value; }");

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.Append("    ");
            }
            sb.AppendLine("}");

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine("}");
            }

            File.WriteAllText(path, sb.ToString());
        }
    }
}
