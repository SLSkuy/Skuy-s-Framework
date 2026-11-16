using System;
using System.Collections.Generic;
using System.Linq;
using Framework;
using GamePlay.BattleSystem;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor
{
    /// <summary>编辑连招资产中的节点及输入转换。</summary>
    public sealed class CombatGraphEditorWindow : EditorWindow
    {
        [SerializeField] private CombatGraphConfig config;
        private CombatGraphView _graph;
        private VisualElement _skills;
        private HelpBox _message;
        private string _search = "";

        [MenuItem("Tools/BattleSystem/连招编辑器")]
        public static void Open()
        {
            GetWindow<CombatGraphEditorWindow>("连招编辑器").Show();
        }

        public static void Open(CombatGraphConfig asset)
        {
            var window = GetWindow<CombatGraphEditorWindow>("连招编辑器");
            window.config = asset;
            window.CreateGUI();
            window.Show();
        }

        [OnOpenAsset]
        private static bool OnOpenAsset(EntityId instanceId, int line)
        {
            if (EditorUtility.EntityIdToObject(instanceId) is not CombatGraphConfig asset) return false;
            Open(asset);
            return true;
        }

        public void CreateGUI()
        {
            minSize = new Vector2(900, 550);
            rootVisualElement.Clear();
            var toolbar = new Toolbar();
            var assetField = new ObjectField { objectType = typeof(CombatGraphConfig), value = config, allowSceneObjects = false };
            assetField.style.width = 260;
            assetField.RegisterValueChangedCallback(evt =>
            {
                config = (CombatGraphConfig)evt.newValue;
                CreateGUI();
            });
            toolbar.Add(assetField);
            toolbar.Add(new ToolbarButton(OnCreateAsset) { text = "新建" });
            toolbar.Add(new ToolbarButton(OnSave) { text = "保存" });
            toolbar.Add(new ToolbarButton(() => _graph?.FrameAll()) { text = "显示全部" });
            rootVisualElement.Add(toolbar);
            if (!config)
            {
                rootVisualElement.Add(new HelpBox("请选择或新建连招配置。", HelpBoxMessageType.Info));
                return;
            }
            var content = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 } };
            var sidebar = new VisualElement { style = { width = 240, paddingLeft = 4, paddingRight = 4 } };
            var tableField = new ObjectField("技能表") { objectType = typeof(SkillTable), value = config.skillTable, allowSceneObjects = false };
            tableField.RegisterValueChangedCallback(evt =>
            {
                var table = (SkillTable)evt.newValue;
                var ids = new HashSet<uint>((table ? table.skills : null)?.Where(s => s != null).Select(s => s.skillId) ?? Enumerable.Empty<uint>());
                if (config.nodes.Any(n => !ids.Contains(n.skillId)))
                {
                    tableField.SetValueWithoutNotify(config.skillTable);
                    EditorUtility.DisplayDialog("无法更换技能表", "新技能表不包含当前图的全部技能，请先删除对应节点。", "确定");
                    return;
                }
                Undo.RecordObject(config, "更换连招技能表");
                config.skillTable = table;
                EditorUtility.SetDirty(config);
                RefreshSkills();
            });
            sidebar.Add(tableField);
            var search = new ToolbarSearchField { value = _search };
            search.RegisterValueChangedCallback(evt => { _search = evt.newValue; RefreshSkills(); });
            sidebar.Add(search);
            _message = new HelpBox("", HelpBoxMessageType.Warning);
            sidebar.Add(_message);
            _skills = new ScrollView { style = { flexGrow = 1 } };
            sidebar.Add(_skills);
            content.Add(sidebar);
            _graph = new CombatGraphView(config);
            content.Add(_graph);
            rootVisualElement.Add(content);
            RefreshSkills();
        }

        private void RefreshSkills()
        {
            _skills.Clear();
            var skills = (config.skillTable ? config.skillTable.skills : null) ?? Array.Empty<SkillConfig>();
            var ids = new HashSet<uint>(skills.Where(s => s != null).Select(s => s.skillId));
            var invalid = config.nodes.Where(n => !ids.Contains(n.skillId)).Select(n => n.skillId).Distinct().ToArray();
            _message.text = !config.skillTable ? "请选择 SkillTable。" : invalid.Length > 0 ? $"表内不存在技能：{string.Join(", ", invalid)}" : "";
            _message.style.display = _message.text.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            foreach (var skill in skills.Where(s => s != null).GroupBy(s => s.skillId).Select(g => g.First()).OrderBy(s => s.skillId))
            {
                var label = $"{skill.skillId}  {skill.skillName}";
                if (label.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0) continue;
                _skills.Add(new Button(() => _graph.AddSkill(skill.skillId)) { text = label, tooltip = "添加技能节点" });
            }
        }

        private void OnCreateAsset()
        {
            var path = EditorUtility.SaveFilePanelInProject("新建连招配置", "CombatGraphConfig", "asset", "选择保存位置");
            if (string.IsNullOrEmpty(path)) return;
            config = CreateInstance<CombatGraphConfig>();
            AssetDatabase.CreateAsset(config, path);
            Selection.activeObject = config;
            CreateGUI();
        }

        private void OnSave()
        {
            if (!config) return;
            try
            {
                config.CreateGraph();
                AssetDatabase.SaveAssetIfDirty(config);
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog("连招配置无效", exception.Message, "确定");
            }
        }

        private void OnUndoRedo()
        {
            CreateGUI();
        }

        private void OnProjectChanged()
        {
            if (config && _skills != null) RefreshSkills();
        }

        private void OnEnable()
        {
            if (!config)
            {
                var guid = AssetDatabase.FindAssets("t:CombatGraphConfig").FirstOrDefault();
                if (guid != null) config = AssetDatabase.LoadAssetAtPath<CombatGraphConfig>(AssetDatabase.GUIDToAssetPath(guid));
            }
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private sealed class CombatNodeView : Node
        {
            public uint Id { get; }
            public CombatNodeView(uint id, string label, Vector2 position)
            {
                Id = id;
                title = label;
                if (id == 0) capabilities &= ~(Capabilities.Deletable | Capabilities.Copiable);
                SetPosition(new Rect(position, new Vector2(210, 150)));
                if (id != 0)
                {
                    var input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
                    input.portName = "进入";
                    inputContainer.Add(input);
                }
                foreach (EntityCommandFlags command in Enum.GetValues(typeof(EntityCommandFlags)))
                {
                    if (command == EntityCommandFlags.None) continue;
                    var output = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                    output.portName = command.ToString();
                    output.userData = command;
                    outputContainer.Add(output);
                }
                RefreshExpandedState();
                RefreshPorts();
            }
        }

        private sealed class CombatGraphView : GraphView
        {
            private readonly CombatGraphConfig _config;
            private readonly Dictionary<uint, CombatNodeView> _nodes = new();

            public CombatGraphView(CombatGraphConfig asset)
            {
                _config = asset;
                style.flexGrow = 1;
                SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
                this.AddManipulator(new ContentDragger());
                this.AddManipulator(new SelectionDragger());
                this.AddManipulator(new RectangleSelector());
                AddNode(0, "入口", asset.rootPosition);
                foreach (var data in asset.nodes)
                {
                    var skill = asset.skillTable ? asset.skillTable.skills?.FirstOrDefault(s => s != null && s.skillId == data.skillId) : null;
                    AddNode(data.nodeId, $"{data.skillId}  {skill?.skillName}", data.position);
                }
                foreach (var pair in _nodes)
                {
                    foreach (var transition in GetTransitions(pair.Key))
                    {
                        if (!_nodes.TryGetValue(transition.targetNodeId, out var target) || target.Id == 0) continue;
                        var output = pair.Value.outputContainer.Children().OfType<Port>().First(p => (EntityCommandFlags)p.userData == transition.command);
                        AddElement(output.ConnectTo(target.inputContainer.Children().OfType<Port>().First()));
                    }
                }
                graphViewChanged = OnGraphChanged;
            }

            public void AddSkill(uint skillId)
            {
                // 添加时重新查询技能表，避免表编辑后旧列表加入非法 ID。
                if (!_config.skillTable || !(_config.skillTable.skills?.Any(s => s != null && s.skillId == skillId) ?? false)) return;
                var id = _config.nodes.Count == 0 ? 1u : checked(_config.nodes.Max(n => n.nodeId) + 1);
                Undo.RecordObject(_config, "添加连招节点");
                var position = contentViewContainer.WorldToLocal(worldBound.center);
                _config.nodes.Add(new CombatGraphConfig.NodeData { nodeId = id, skillId = skillId, position = position });
                var skill = _config.skillTable.skills.First(s => s != null && s.skillId == skillId);
                AddNode(id, $"{skillId}  {skill.skillName}", position);
                EditorUtility.SetDirty(_config);
            }

            public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
            {
                return ports.Where(p => p.direction != startPort.direction && p.portType == startPort.portType).ToList();
            }

            private void AddNode(uint id, string label, Vector2 position)
            {
                var node = new CombatNodeView(id, label, position);
                _nodes.Add(id, node);
                AddElement(node);
            }

            private List<CombatGraphConfig.TransitionData> GetTransitions(uint id)
            {
                return id == 0 ? _config.rootTransitions : _config.nodes.First(n => n.nodeId == id).transitions;
            }

            private GraphViewChange OnGraphChanged(GraphViewChange change)
            {
                Undo.RecordObject(_config, "编辑连招图");
                if (change.elementsToRemove != null)
                {
                    foreach (var edge in change.elementsToRemove.OfType<Edge>())
                    {
                        var source = (CombatNodeView)edge.output.node;
                        GetTransitions(source.Id).RemoveAll(t => t.command == (EntityCommandFlags)edge.output.userData);
                    }
                    foreach (var node in change.elementsToRemove.OfType<CombatNodeView>())
                    {
                        _config.nodes.RemoveAll(n => n.nodeId == node.Id);
                        _config.rootTransitions.RemoveAll(t => t.targetNodeId == node.Id);
                        foreach (var data in _config.nodes) data.transitions.RemoveAll(t => t.targetNodeId == node.Id);
                        _nodes.Remove(node.Id);
                    }
                }
                if (change.edgesToCreate != null)
                {
                    foreach (var edge in change.edgesToCreate)
                    {
                        var source = (CombatNodeView)edge.output.node;
                        var target = (CombatNodeView)edge.input.node;
                        var command = (EntityCommandFlags)edge.output.userData;
                        var transitions = GetTransitions(source.Id);
                        transitions.RemoveAll(t => t.command == command);
                        transitions.Add(new CombatGraphConfig.TransitionData { command = command, targetNodeId = target.Id });
                    }
                }
                if (change.movedElements != null)
                {
                    foreach (var node in change.movedElements.OfType<CombatNodeView>())
                    {
                        if (node.Id == 0) _config.rootPosition = node.GetPosition().position;
                        else _config.nodes.First(n => n.nodeId == node.Id).position = node.GetPosition().position;
                    }
                }
                EditorUtility.SetDirty(_config);
                return change;
            }
        }
    }
}