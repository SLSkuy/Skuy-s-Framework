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
        private Action<uint> _chooseSkill;

        private void OpenSkillPicker(Vector2 panelPosition, Action<uint> chooseSkill)
        {
            _chooseSkill = chooseSkill;
            SearchWindow.Open(new SearchWindowContext(position.position + panelPosition, 320), this);
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var entries = new List<SearchTreeEntry> { new SearchTreeGroupEntry(new GUIContent("创建技能节点")) };
            var skills = config.skillTable ? config.skillTable.skills : null;
            foreach (var skill in (skills ?? Array.Empty<SkillConfig>()).Where(s => s != null)
                         .GroupBy(s => s.skillId).Select(g => g.First()).OrderBy(s => s.skillId))
            {
                entries.Add(new SearchTreeEntry(new GUIContent($"{skill.skillId}  {skill.skillName}"))
                    { level = 1, userData = skill.skillId });
            }
            return entries;
        }

        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            if (entry.userData is uint id) _chooseSkill?.Invoke(id);
            _chooseSkill = null;
            return true;
        }

        [MenuItem("Tools/BattleSystem/连招编辑器")]
        public static void Open()
        {
            var window = GetWindow<CombatGraphEditorWindow>("连招编辑器");
            window.Show();
            window._graph?.schedule.Execute(() => window._graph?.FrameAll());
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
            _chooseSkill = null;
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
            sidebar.Add(new Label("备选技能") { style = { unityFontStyleAndWeight = FontStyle.Bold } });
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
            _graph = new CombatGraphView(config, OpenSkillPicker);
            content.Add(_graph);
            var inspector = new VisualElement { style = { width = 275, flexShrink = 0, paddingLeft = 8, paddingRight = 8 } };
            var details = new ScrollView { style = { flexGrow = 1, flexBasis = 0 } };
            details.Add(new IMGUIContainer(_graph.DrawInspector));
            inspector.Add(details);
            inspector.Add(_graph.DeleteButton);
            content.Add(inspector);
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
                _skills.Add(new Button(() => _graph.AddSkill(skill.skillId, _graph.contentViewContainer.WorldToLocal(_graph.worldBound.center))) { text = label, tooltip = "添加技能节点" });
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
            public int View { get; }
            public Port Input { get; }
            public Port Output { get; }

            public CombatNodeView(uint id, int view, string label, Vector2 position, IEdgeConnectorListener listener)
            {
                Id = id;
                View = view;
                title = label;
                capabilities &= ~(Capabilities.Copiable | Capabilities.Deletable | Capabilities.Collapsible);
                titleContainer.style.backgroundColor = id == 0 ? new Color(.15f, .42f, .28f)
                    : id == uint.MaxValue ? new Color(.23f, .35f, .49f)
                    : view > 0 ? new Color(.48f, .30f, .16f) : new Color(.24f, .28f, .48f);
                if (id != 0 && id != uint.MaxValue)
                {
                    Input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
                    Input.portName = "进入";
                    inputContainer.Add(Input);
                }
                Output = new TransitionPort(listener);
                Output.portName = "转换";
                outputContainer.Add(Output);
                RefreshPorts();
                RefreshExpandedState();
                SetPosition(new Rect(position, new Vector2(180, 70)));
            }
        }

        private sealed class TransitionPort : Port
        {
            public TransitionPort(IEdgeConnectorListener listener)
                : base(Orientation.Horizontal, Direction.Output, Capacity.Multi, typeof(bool))
            {
                m_EdgeConnector = new EdgeConnector<Edge>(listener);
                this.AddManipulator(m_EdgeConnector);
            }
        }

        private sealed class CombatGraphView : GraphView, IEdgeConnectorListener
        {
            private static readonly EntityState[] EntityStates = (EntityState[])Enum.GetValues(typeof(EntityState));
            private static readonly string[] EntityStateNames = Enum.GetNames(typeof(EntityState));

            private readonly CombatGraphConfig _config;
            private readonly Action<Vector2, Action<uint>> _openPicker;
            private readonly Dictionary<(uint, int), CombatNodeView> _views = new();
            private readonly HashSet<EntityStateTransitionCondition> _collapsedConditions = new();
            private CombatNodeView _pending;
            private CombatNodeView _selected;
            private CombatGraphConfig.TransitionData _transition;
            private uint _source;
            private int _pressedRow;
            private Vector2 _pressPosition;
            private bool _draggingRow;

            public Button DeleteButton { get; }

            public CombatGraphView(CombatGraphConfig asset, Action<Vector2, Action<uint>> openPicker)
            {
                _config = asset;
                _openPicker = openPicker;
                style.flexGrow = 1;
                style.minWidth = 0;
                style.overflow = Overflow.Hidden;
                DeleteButton = new Button(() => DeleteElements(edges.Where(e => e.userData == _transition).ToList()))
                {
                    text = "删除转换", style = { display = DisplayStyle.None, flexShrink = 0 }
                };
                var grid = new VisualElement
                {
                    pickingMode = PickingMode.Ignore, style = { backgroundColor = new Color(.105f, .12f, .14f) }
                };
                grid.generateVisualContent += ctx => DrawGrid(ctx, grid);
                RegisterCallback<PointerMoveEvent>(evt =>
                {
                    if (evt.pressedButtons != 0) grid.MarkDirtyRepaint();
                }, TrickleDown.TrickleDown);
                viewTransformChanged += _ => grid.MarkDirtyRepaint();
                RegisterCallback<GeometryChangedEvent>(OnFirstLayout);
                Insert(0, grid);
                grid.StretchToParentSize();
                SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
                this.AddManipulator(new ContentDragger());
                this.AddManipulator(new SelectionDragger());
                this.AddManipulator(new RectangleSelector());
                AddNode(0, 0, "入口", asset.rootPosition);
                AddNode(uint.MaxValue, 0, "任意状态", asset.anyPosition);
                foreach (var data in asset.nodes)
                {
                    var skill = asset.skillTable ? asset.skillTable.skills?.FirstOrDefault(s => s != null && s.skillId == data.skillId) : null;
                    string label = skill != null ? skill.skillName : "缺失技能";
                    AddNode(data.nodeId, 0, label, data.position);
                    for (int i = 0; i < data.copies.Count; i++)
                        AddNode(data.nodeId, i + 1, label, data.copies[i]);
                }
                foreach (var node in _views.Values.Where(n => n.View == 0))
                    foreach (var transition in Transitions(node.Id))
                        DrawEdge(node.Id, transition);

                graphViewChanged = OnGraphChanged;
                RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
                RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Escape) _pending = null;
                    if (evt.keyCode == KeyCode.Delete && selection.OfType<CombatNodeView>().Any(n => n.Input != null))
                    {
                        evt.StopImmediatePropagation();
                        RemoveNodes(selection.OfType<CombatNodeView>().Where(n => n.Input != null),
                            selection.OfType<Edge>());
                    }
                }, TrickleDown.TrickleDown);
            }

            private void OnFirstLayout(GeometryChangedEvent evt)
            {
                if (evt.newRect.width <= 0 || evt.newRect.height <= 0) return;
                UnregisterCallback<GeometryChangedEvent>(OnFirstLayout);
                schedule.Execute(() => FrameAll());
            }

            private void DrawGrid(MeshGenerationContext context, VisualElement grid)
            {
                var painter = context.painter2D;
                var rect = grid.contentRect;
                var origin = contentViewContainer.ChangeCoordinatesTo(grid, Vector2.zero);
                float spacing = 24 * viewTransform.scale.x;
                while (spacing < 12) spacing *= 5;
                for (int layer = 0; layer < 2; layer++, spacing *= 5)
                {
                    painter.lineWidth = 1;
                    painter.strokeColor = layer == 0 ? new Color(.15f, .17f, .19f) : new Color(.21f, .23f, .25f);
                    painter.BeginPath();
                    for (float x = Mathf.Repeat(origin.x, spacing); x < rect.width; x += spacing)
                    {
                        painter.MoveTo(new Vector2(x, 0));
                        painter.LineTo(new Vector2(x, rect.height));
                    }
                    for (float y = Mathf.Repeat(origin.y, spacing); y < rect.height; y += spacing)
                    {
                        painter.MoveTo(new Vector2(0, y));
                        painter.LineTo(new Vector2(rect.width, y));
                    }
                    painter.Stroke();
                }
            }

            private void OnMouseDown(MouseDownEvent evt)
            {
                if (evt.button != 0) return;
                var element = (VisualElement)evt.target;
                var node = element as CombatNodeView ?? element.GetFirstAncestorOfType<CombatNodeView>();
                var edge = element as Edge ?? element.GetFirstAncestorOfType<Edge>();
                if (_pending != null && node?.Input != null)
                {
                    Record("创建转换");
                    var transition = CreateTransition(_pending, node);
                    DrawEdge(_pending.Id, transition);
                    Inspect(_pending.Id, transition);
                    evt.StopImmediatePropagation();
                }
                else if (edge != null)
                    Inspect(((CombatNodeView)edge.output.node).Id, (CombatGraphConfig.TransitionData)edge.userData);
                else
                {
                    _selected = node;
                    SelectTransition(null);
                }
                _pending = null;
            }

            public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
            {
                var element = (VisualElement)evt.target;
                var node = element as CombatNodeView ?? element.GetFirstAncestorOfType<CombatNodeView>();
                if (node != null)
                {
                    evt.menu.AppendAction("创建转换 → 点击目标节点", _ => _pending = node);
                    if (node.Input != null)
                    {
                        evt.menu.AppendAction("创建复制节点", _ =>
                        {
                            Record("复制节点视图");
                            var data = Data(node.Id);
                            var position = node.GetPosition().position + new Vector2(45, 95);
                            data.copies.Add(position);
                            AddNode(node.Id, data.copies.Count, _views[(node.Id, 0)].title, position);
                        });
                        evt.menu.AppendAction(node.View == 0 ? "删除状态及其转换" : "移除副本",
                            _ => RemoveNodes(new[] { node }, Array.Empty<Edge>()));
                    }
                    return;
                }
                if (element is Edge || element.GetFirstAncestorOfType<Edge>() != null) return;
                ShowSkillPicker(evt.mousePosition, null);
                evt.StopPropagation();
            }

            private void ShowSkillPicker(Vector2 panelPosition, CombatNodeView source)
            {
                var point = contentViewContainer.WorldToLocal(panelPosition);
                _openPicker(panelPosition, id => AddSkill(id, point, source));
            }

            public void OnDropOutsidePort(Edge edge, Vector2 position)
            {
                if (edge.output?.node is not CombatNodeView source || !worldBound.Contains(position)) return;
                if (_views.Values.Any(n => n.worldBound.Contains(position))) return;
                ShowSkillPicker(position, source);
            }

            public void OnDrop(GraphView graphView, Edge edge)
            {
                Record("创建转换");
                var source = (CombatNodeView)edge.output.node;
                edge.userData = CreateTransition(source, (CombatNodeView)edge.input.node);
                edge.output.Connect(edge);
                edge.input.Connect(edge);
                AddElement(edge);
            }

            public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
            {
                return ports.Where(p => p.direction != startPort.direction).ToList();
            }

            public void AddSkill(uint skillId, Vector2 position, CombatNodeView source = null)
            {
                Record("添加连招节点");
                uint id = _config.nodes.Count == 0 ? 1 : checked(_config.nodes.Max(n => n.nodeId) + 1);
                var skill = _config.skillTable.skills.First(s => s != null && s.skillId == skillId);
                _config.nodes.Add(new CombatGraphConfig.NodeData { nodeId = id, skillId = skillId, position = position });
                AddNode(id, 0, skill.skillName, position);
                _selected = _views[(id, 0)];
                if (source != null) DrawEdge(source.Id, CreateTransition(source, _selected));
                SelectTransition(null);
                ClearSelection();
                AddToSelection(_selected);
            }

            private void RemoveNodes(IEnumerable<CombatNodeView> nodes, IEnumerable<Edge> selectedEdges)
            {
                Record("移除连招节点");
                var removedIds = nodes.Where(n => n.View == 0).Select(n => n.Id).ToHashSet();
                var removedEdges = selectedEdges.Select(edge => edge.userData).ToHashSet();
                var copies = nodes.Where(n => n.View > 0 && !removedIds.Contains(n.Id))
                    .GroupBy(n => n.Id).ToDictionary(g => g.Key, g => g.Select(n => n.View).OrderByDescending(i => i).ToArray());
                foreach (var source in _views.Values.Where(n => n.View == 0))
                {
                    var transitions = Transitions(source.Id);
                    transitions.RemoveAll(t => removedIds.Contains(t.targetNodeId) || removedEdges.Contains(t));
                    foreach (var t in transitions)
                    {
                        if (copies.TryGetValue(source.Id, out var sourceCopies))
                            foreach (var view in sourceCopies) t.sourceView = RemapView(t.sourceView, view);
                        if (copies.TryGetValue(t.targetNodeId, out var targetCopies))
                            foreach (var view in targetCopies) t.targetView = RemapView(t.targetView, view);
                    }
                }
                foreach (var pair in copies)
                {
                    var data = Data(pair.Key);
                    foreach (var view in pair.Value) data.copies.RemoveAt(view - 1);
                }
                _config.nodes.RemoveAll(n => removedIds.Contains(n.nodeId));
                GetWindow<CombatGraphEditorWindow>().CreateGUI();
            }

            private static int RemapView(int index, int removed) => index == removed ? 0 : index > removed ? index - 1 : index;

            private void AddNode(uint id, int view, string label, Vector2 position)
            {
                var node = new CombatNodeView(id, view, label, position, this);
                _views.Add((id, view), node);
                AddElement(node);
            }

            private CombatGraphConfig.NodeData Data(uint id) => _config.nodes.First(n => n.nodeId == id);

            private List<CombatGraphConfig.TransitionData> Transitions(uint id) =>
                id == 0 ? _config.rootTransitions : id == uint.MaxValue ? _config.anyTransitions : Data(id).transitions;

            private void Record(string action)
            {
                Undo.RecordObject(_config, action);
                EditorUtility.SetDirty(_config);
            }

            private CombatGraphConfig.TransitionData CreateTransition(CombatNodeView source, CombatNodeView target)
            {
                var transition = new CombatGraphConfig.TransitionData
                {
                    targetNodeId = target.Id, sourceView = source.View, targetView = target.View,
                    command = InputCommandFlags.PrimaryAttack
                };
                Transitions(source.Id).Add(transition);
                return transition;
            }

            private void DrawEdge(uint source, CombatGraphConfig.TransitionData transition)
            {
                var edge = _views[(source, transition.sourceView)].Output.ConnectTo(_views[(transition.targetNodeId, transition.targetView)].Input);
                edge.userData = transition;
                AddElement(edge);
            }

            private GraphViewChange OnGraphChanged(GraphViewChange change)
            {
                Record("编辑连招图");
                if (change.edgesToCreate != null)
                    foreach (var edge in change.edgesToCreate)
                        edge.userData = CreateTransition((CombatNodeView)edge.output.node, (CombatNodeView)edge.input.node);
                if (change.elementsToRemove != null)
                    foreach (var edge in change.elementsToRemove.OfType<Edge>())
                    {
                        Transitions(((CombatNodeView)edge.output.node).Id).Remove((CombatGraphConfig.TransitionData)edge.userData);
                        if (edge.userData == _transition) SelectTransition(null);
                    }
                if (change.movedElements != null)
                {
                    foreach (var node in change.movedElements.OfType<CombatNodeView>())
                    {
                        var position = node.GetPosition().position;
                        if (node.Id == 0) _config.rootPosition = position;
                        else if (node.Id == uint.MaxValue) _config.anyPosition = position;
                        else
                        {
                            var data = Data(node.Id);
                            if (node.View == 0) data.position = position;
                            else data.copies[node.View - 1] = position;
                        }
                    }
                }
                return change;
            }

            private void SelectTransition(CombatGraphConfig.TransitionData transition)
            {
                _transition = transition;
                DeleteButton.style.display = transition == null ? DisplayStyle.None : DisplayStyle.Flex;
            }

            private void Inspect(uint source, CombatGraphConfig.TransitionData transition)
            {
                _source = source;
                SelectTransition(transition);
                _selected = null;
                ClearSelection();
                AddToSelection(edges.First(e => e.userData == transition));
            }

            private int SelectView(string label, uint id, int current)
            {
                if (_views[(id, 0)].Input == null || Data(id).copies.Count == 0) return current;
                var labels = Enumerable.Range(0, Data(id).copies.Count + 1)
                    .Select(i => i == 0 ? "主节点" : $"副本 {i}").ToArray();
                return EditorGUILayout.Popup(label, current, labels);
            }

            private static void DrawDivider()
            {
                GUILayout.Space(6);
                EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), new Color(.45f, .45f, .45f, .65f));
                GUILayout.Space(4);
            }

            private string Preview(uint source, CombatGraphConfig.TransitionData transition)
            {
                var from = _views[(source, transition.sourceView)];
                var to = _views[(transition.targetNodeId, transition.targetView)];
                return $"{from.title} → {to.title}\n{transition.command} · {transition.commandState}";
            }

            private void DrawConnections(uint id)
            {
                GUILayout.Label("进入", EditorStyles.boldLabel);
                foreach (var node in _views.Values.Where(n => n.View == 0))
                {
                    foreach (var transition in Transitions(node.Id))
                    {
                        if (transition.targetNodeId == id && GUILayout.Button(Preview(node.Id, transition)))
                        {
                            Inspect(node.Id, transition);
                            return;
                        }
                    }
                }
                DrawDivider();
                GUILayout.Label("转换（上下拖拽调整优先级）", EditorStyles.boldLabel);
                var outgoing = Transitions(id);
                if (outgoing.Count == 0) return;

                const float rowHeight = 38;
                var bounds = EditorGUILayout.GetControlRect(false, outgoing.Count * rowHeight);
                int control = GUIUtility.GetControlID(FocusType.Passive, bounds);
                var evt = Event.current;
                int hovered = Mathf.Clamp(Mathf.FloorToInt((evt.mousePosition.y - bounds.y) / rowHeight), 0, outgoing.Count - 1);
                bool captured = GUIUtility.hotControl == control;
                switch (evt.GetTypeForControl(control))
                {
                    case EventType.MouseDown when evt.button == 0 && bounds.Contains(evt.mousePosition):
                        GUIUtility.hotControl = control;
                        _pressedRow = hovered;
                        _pressPosition = evt.mousePosition;
                        _draggingRow = false;
                        evt.Use();
                        break;
                    case EventType.MouseDrag when captured:
                        _draggingRow |= Mathf.Abs(evt.mousePosition.y - _pressPosition.y) >= 4;
                        evt.Use();
                        break;
                    case EventType.MouseUp when captured:
                        GUIUtility.hotControl = 0;
                        evt.Use();
                        if (bounds.Contains(evt.mousePosition))
                        {
                            var item = outgoing[_pressedRow];
                            if (!_draggingRow) Inspect(id, item);
                            else if (_pressedRow != hovered)
                            {
                                Record("调整转换判定顺序");
                                outgoing.RemoveAt(_pressedRow);
                                outgoing.Insert(hovered, item);
                            }
                        }
                        _draggingRow = false;
                        break;
                    case EventType.Repaint:
                        for (int i = 0; i < outgoing.Count; i++)
                        {
                            var row = new Rect(bounds.x, bounds.y + i * rowHeight, bounds.width, rowHeight - 2);
                            GUI.skin.button.Draw(row, new GUIContent(Preview(id, outgoing[i])), row.Contains(evt.mousePosition),
                                captured && i == _pressedRow, false, false);
                            EditorGUIUtility.AddCursorRect(row, MouseCursor.Pan);
                            if (captured && _draggingRow && i == hovered && i != _pressedRow)
                                EditorGUI.DrawRect(new Rect(row.x, i < _pressedRow ? row.y : row.yMax, row.width, 2), Color.cyan);
                        }
                        break;
                }
            }

            public void DrawInspector()
            {
                if (_pending != null)
                    EditorGUILayout.HelpBox("点击目标技能节点以完成转换。Esc 取消。", MessageType.Info);
                if (_transition == null)
                {
                    GUILayout.Label("节点状态", EditorStyles.boldLabel);
                    if (_selected == null)
                    {
                        GUILayout.Label("选择节点或转换线查看详情。");
                        return;
                    }
                    DrawDivider();
                    GUILayout.Label(_selected.title);
                    if (_selected.Input != null) GUILayout.Label($"状态 ID   {_selected.Id}");
                    DrawDivider();
                    DrawConnections(_selected.Id);
                    return;
                }

                var transition = _transition;
                GUILayout.Label("基础信息", EditorStyles.boldLabel);
                GUILayout.Label($"{_views[(_source, 0)].title} → {_views[(transition.targetNodeId, 0)].title}", EditorStyles.wordWrappedLabel);

                int targetView = SelectView("终点视图", transition.targetNodeId, transition.targetView);
                if (targetView != transition.targetView)
                {
                    Record("更改转换连线视图");
                    var edge = edges.First(e => e.userData == transition);
                    edge.output.Disconnect(edge);
                    edge.input.Disconnect(edge);
                    ClearSelection();
                    RemoveElement(edge);

                    transition.targetView = targetView;
                    DrawEdge(_source, transition);
                    Inspect(_source, transition);
                }
                EditorGUI.BeginChangeCheck();
                var command = (InputCommandFlags)EditorGUILayout.EnumPopup("输入命令", transition.command);
                var state = (InputCommandState)EditorGUILayout.EnumPopup("命令状态", transition.commandState);
                if (EditorGUI.EndChangeCheck())
                {
                    Record("编辑输入条件");
                    transition.command = command;
                    transition.commandState = state;
                }
                DrawDivider();
                GUILayout.Label("转换条件", EditorStyles.boldLabel);
                var conditions = transition.stateConditions;
                for (int i = 0; i < conditions.Count; i++)
                {
                    var condition = conditions[i];
                    using var card = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);
                    bool expanded = !_collapsedConditions.Contains(condition);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (EditorGUILayout.Foldout(expanded, "实体状态", true) != expanded)
                        {
                            expanded = !expanded;
                            if (expanded) _collapsedConditions.Remove(condition);
                            else _collapsedConditions.Add(condition);
                        }
                        int destination = i;
                        using (new EditorGUI.DisabledScope(i == 0))
                            if (GUILayout.Button("↑", GUILayout.Width(24))) destination--;
                        using (new EditorGUI.DisabledScope(i == conditions.Count - 1))
                            if (GUILayout.Button("↓", GUILayout.Width(24))) destination++;
                        if (destination != i)
                        {
                            Record("调整转换条件顺序");
                            conditions.RemoveAt(i);
                            conditions.Insert(destination, condition);
                            break;
                        }
                        if (GUILayout.Button("×", GUILayout.Width(24)))
                        {
                            Record("移除转换条件");
                            conditions.RemoveAt(i);
                            _collapsedConditions.Remove(condition);
                            break;
                        }
                    }
                    if (!expanded) continue;
                    int mask = 0;
                    foreach (var allowed in condition.allowedStates)
                    {
                        int index = Array.IndexOf(EntityStates, allowed);
                        if (index >= 0) mask |= 1 << index;
                    }
                    EditorGUI.BeginChangeCheck();
                    int selected = EditorGUILayout.MaskField(new GUIContent("允许状态", "匹配任意勾选状态时允许转换。"),
                        mask, EntityStateNames);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Record("编辑允许状态");
                        condition.allowedStates = EntityStates.Where((value, index) => (selected & (1 << index)) != 0).ToArray();
                    }
                }
                if (EditorGUILayout.DropdownButton(new GUIContent("添加条件"), FocusType.Keyboard))
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("实体状态"), false, () =>
                    {
                        Record("添加转换条件");
                        transition.stateConditions.Add(new EntityStateTransitionCondition());
                    });
                    menu.ShowAsContext();
                }
            }
        }
    }
}
