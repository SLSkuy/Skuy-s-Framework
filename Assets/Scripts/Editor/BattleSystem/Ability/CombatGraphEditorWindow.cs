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
    public sealed class CombatGraphEditorWindow : EditorWindow, ISearchWindowProvider
    {
        [SerializeField] private CombatGraphConfig config;
        private CombatGraphView _graph;
        private VisualElement _skills;
        private HelpBox _message;
        private string _search = "";
        private Port _pendingOutput;
        private Vector2 _dropPosition;

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
            var sidebar = new VisualElement { style = { width = 240, flexShrink = 0, paddingLeft = 6, paddingRight = 6 } };
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
            var search = new ToolbarSearchField
            {
                value = _search,
                style = { width = StyleKeyword.Auto, minWidth = 0, alignSelf = Align.Stretch }
            };
            search.RegisterValueChangedCallback(evt => { _search = evt.newValue; RefreshSkills(); });
            sidebar.Add(search);
            _message = new HelpBox("", HelpBoxMessageType.Warning);
            sidebar.Add(_message);
            _skills = new ScrollView { style = { flexGrow = 1 } };
            sidebar.Add(_skills);
            content.Add(sidebar);
            _graph = new CombatGraphView(config, OpenNodeSearch);
            content.Add(_graph);
            rootVisualElement.Add(content);
            RefreshSkills();
        }

        private void OpenNodeSearch(Port output, Vector2 position)
        {
            _pendingOutput = output;
            _dropPosition = _graph.contentViewContainer.WorldToLocal(position);
            SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(position), 320), this);
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var entries = new List<SearchTreeEntry> { new SearchTreeGroupEntry(new GUIContent("选择后续技能")) };
            var skills = config.skillTable ? config.skillTable.skills : null;
            foreach (var skill in (skills ?? Array.Empty<SkillConfig>()).Where(s => s != null)
                         .GroupBy(s => s.skillId).Select(group => group.First()).OrderBy(s => s.skillId))
            {
                entries.Add(new SearchTreeEntry(new GUIContent($"{skill.skillId}  {skill.skillName}"))
                    { level = 1, userData = skill.skillId });
            }
            return entries;
        }

        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            if (entry.userData is uint id && _pendingOutput?.panel != null)
                _graph.AddSkill(id, _dropPosition, _pendingOutput);
            _pendingOutput = null;
            return true;
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
            private readonly Dictionary<EntityCommandFlags, Port> _outputs = new();

            public uint Id { get; }
            public CombatNodeView(uint id, string label, Vector2 position)
            {
                Id = id;
                title = label;
                if (id == 0)
                {
                    capabilities &= ~(Capabilities.Deletable | Capabilities.Copiable | Capabilities.Collapsible);
                    style.minWidth = 156;
                    titleContainer.style.minHeight = 30;
                    titleContainer.style.backgroundColor = new Color(0.16f, 0.43f, 0.28f);
                    var heading = titleContainer.Q<Label>("title-label");
                    heading.style.fontSize = 14;
                    heading.style.unityFontStyleAndWeight = FontStyle.Bold;
                }
                SetPosition(new Rect(position, new Vector2(210, 150)));
                if (id != 0)
                {
                    var input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
                    input.portName = "进入";
                    inputContainer.Add(input);
                }
                var addTransition = new ToolbarMenu { text = "添加转换" };
                foreach (EntityCommandFlags command in Enum.GetValues(typeof(EntityCommandFlags)))
                {
                    if (command == EntityCommandFlags.None) continue;
                    addTransition.menu.AppendAction(command.ToString(), _ => AddOutput(command),
                        _ => _outputs.ContainsKey(command) ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
                }
                extensionContainer.Add(addTransition);
                RefreshExpandedState();
                RefreshPorts();
            }

            public Port AddOutput(EntityCommandFlags command)
            {
                if (_outputs.TryGetValue(command, out var output)) return output;
                var graph = GetFirstAncestorOfType<CombatGraphView>();
                output = new TransitionPort(graph);
                output.portName = command.ToString();
                output.userData = command;
                var controls = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                controls.Add(ActionButton("−", $"删除 {command} 转换", () => RemoveOutput(command)));
                controls.Add(ActionButton("↑", "上移转换", () => MoveOutput(output, -1)));
                controls.Add(ActionButton("↓", "下移转换", () => MoveOutput(output, 1)));
                output.Add(controls);
                _outputs.Add(command, output);
                outputContainer.Add(output);
                RefreshPorts();
                return output;
            }

            private static Button ActionButton(string text, string tooltip, Action action)
            {
                return new Button(action)
                {
                    text = text, tooltip = tooltip,
                    style = { width = 19, paddingLeft = 0, paddingRight = 0, marginLeft = 0, marginRight = 1 }
                };
            }

            private void MoveOutput(Port output, int offset)
            {
                int target = outputContainer.IndexOf(output) + offset;
                if (target < 0 || target >= outputContainer.childCount) return;
                var sibling = outputContainer[target];
                if (offset < 0) output.PlaceBehind(sibling);
                else output.PlaceInFront(sibling);
                GetFirstAncestorOfType<CombatGraphView>().SaveTransitionOrder(this);
            }

            public int OutputIndex(EntityCommandFlags command)
            {
                return _outputs.TryGetValue(command, out var port) ? outputContainer.IndexOf(port) : int.MaxValue;
            }

            private void RemoveOutput(EntityCommandFlags command)
            {
                var port = _outputs[command];
                if (port.connected) GetFirstAncestorOfType<CombatGraphView>().DeleteElements(port.connections.ToList());
                port.RemoveFromHierarchy();
                _outputs.Remove(command);
                RefreshPorts();
            }
        }

        private sealed class TransitionPort : Port
        {
            public TransitionPort(IEdgeConnectorListener listener)
                : base(Orientation.Horizontal, Direction.Output, Capacity.Single, typeof(bool))
            {
                m_EdgeConnector = new EdgeConnector<Edge>(listener);
                this.AddManipulator(m_EdgeConnector);
            }
        }

        private sealed class CombatGraphView : GraphView, IEdgeConnectorListener
        {
            private readonly CombatGraphConfig _config;
            private readonly Dictionary<uint, CombatNodeView> _nodes = new();

            private readonly Action<Port, Vector2> _openSearch;

            public CombatGraphView(CombatGraphConfig asset, Action<Port, Vector2> openSearch)
            {
                _config = asset;
                _openSearch = openSearch;
                style.flexGrow = 1;
                style.minWidth = 0;
                style.overflow = Overflow.Hidden;
                var grid = new GridBackground { pickingMode = PickingMode.Ignore };
                Insert(0, grid);
                grid.StretchToParentSize();
                SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
                this.AddManipulator(new ContentDragger());
                this.AddManipulator(new SelectionDragger());
                this.AddManipulator(new RectangleSelector());
                AddNode(0, "连招入口", asset.rootPosition);
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
                        var output = pair.Value.AddOutput(transition.command);
                        AddElement(output.ConnectTo(target.inputContainer.Children().OfType<Port>().First()));
                    }
                }
                graphViewChanged = OnGraphChanged;
            }

            public void AddSkill(uint skillId)
            {
                AddSkill(skillId, contentViewContainer.WorldToLocal(worldBound.center), null);
            }

            public void AddSkill(uint skillId, Vector2 position, Port output)
            {
                // 添加时重新查询技能表，避免表编辑后旧列表加入非法 ID。
                if (!_config.skillTable || !(_config.skillTable.skills?.Any(s => s != null && s.skillId == skillId) ?? false)) return;
                var id = _config.nodes.Count == 0 ? 1u : checked(_config.nodes.Max(n => n.nodeId) + 1);
                Undo.RecordObject(_config, "添加连招节点");
                _config.nodes.Add(new CombatGraphConfig.NodeData { nodeId = id, skillId = skillId, position = position });
                var skill = _config.skillTable.skills.First(s => s != null && s.skillId == skillId);
                AddNode(id, $"{skillId}  {skill.skillName}", position);
                if (output != null)
                {
                    var target = _nodes[id];
                    OnDrop(this, new Edge { output = output, input = target.inputContainer.Children().OfType<Port>().First() });
                    ClearSelection();
                    AddToSelection(target);
                }
                EditorUtility.SetDirty(_config);
            }

            public void OnDropOutsidePort(Edge edge, Vector2 position)
            {
                if (edge.output != null && worldBound.Contains(position)) _openSearch(edge.output, position);
            }

            public void OnDrop(GraphView graphView, Edge edge)
            {
                if (edge.output.connected) DeleteElements(edge.output.connections.ToList());
                OnGraphChanged(new GraphViewChange { edgesToCreate = new List<Edge> { edge } });
                edge.output.Connect(edge);
                edge.input.Connect(edge);
                AddElement(edge);
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

            public void SaveTransitionOrder(CombatNodeView node)
            {
                Undo.RecordObject(_config, "调整连招转换顺序");
                SortTransitions(node);
                EditorUtility.SetDirty(_config);
            }

            private void SortTransitions(CombatNodeView node)
            {
                GetTransitions(node.Id).Sort((a, b) => node.OutputIndex(a.command).CompareTo(node.OutputIndex(b.command)));
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
                        SortTransitions(source);
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
