#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Framework.Editor
{
    public sealed class InstantiateManagerDebugWindow : EditorWindow
    {
        private const double REFRESH_INTERVAL = 0.5d;

        private readonly List<InstantiateManager.DebugGroupStat> _groups = new();
        private readonly List<InstantiateManager.DebugLocationStat> _locations = new();
        private readonly List<InstantiateManager.DebugInstanceInfo> _instances = new();
        private readonly List<GameObject> _unpooled = new();
        private readonly HashSet<string> _expandedLocations = new();

        private ToolbarToggle _autoRefreshToggle;
        private VisualElement _body;
        private ScrollView _locationScroll;
        private ToolbarSearchField _searchField;
        private ToolbarToggle _onlyBusyToggle;
        private Label _statusLabel;
        private Label _modeLabel;
        private readonly List<ToolbarToggle> _tabs = new();
        private int _viewIndex;
        private int _sortIndex;
        private static readonly Color Accent = new Color(0.25f, 0.75f, 0.95f);

        private float _locationScrollOffset;

        private double _nextRefresh;

        [MenuItem("Tools/Framework/Instantiate Pool")]
        public static void ShowWindow()
        {
            InstantiateManagerDebugWindow window =
                GetWindow<InstantiateManagerDebugWindow>("对象池查看");

            window.minSize = new Vector2(760, 500);
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            RefreshView();
        }

        private void CreateGUI()
        {
            BuildWindow();
            RefreshView();
        }

        private void OnEditorUpdate()
        {
            if (_autoRefreshToggle == null ||
                !_autoRefreshToggle.value ||
                !Application.isPlaying)
            {
                return;
            }

            if (EditorApplication.timeSinceStartup < _nextRefresh)
                return;

            _nextRefresh = EditorApplication.timeSinceStartup + REFRESH_INTERVAL;
            RefreshView();
        }

        #region Window

        private void BuildWindow()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.flexDirection = FlexDirection.Column;

            VisualElement heading = new VisualElement();
            ApplyHelpBox(heading);
            heading.style.paddingTop = 12;
            heading.style.paddingBottom = 12;
            Label title = new Label("对象池查看");
            title.style.fontSize = 20;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.Add(title);
            Label subtitle = new Label("查看资源池占用、借出实例和加载请求，点击实例可在场景中定位。");
            subtitle.style.color = MutedColor();
            subtitle.style.whiteSpace = WhiteSpace.Normal;
            heading.Add(subtitle);
            rootVisualElement.Add(heading);
            CreateToolbar();
            Toolbar navigation = new Toolbar();
            _tabs.Clear();
            string[] titles = { "资源明细", "分组统计", "非池化实例" };
            for (int i = 0; i < titles.Length; i++)
            {
                int index = i;
                ToolbarToggle tab = new ToolbarToggle { text = titles[i], value = _viewIndex == i };
                tab.style.minWidth = 105;
                tab.RegisterValueChangedCallback(evt =>
                {
                    if (!evt.newValue)
                    {
                        tab.SetValueWithoutNotify(_viewIndex == index);
                        return;
                    }
                    _viewIndex = index;
                    for (int j = 0; j < _tabs.Count; j++) _tabs[j].SetValueWithoutNotify(j == index);
                    RefreshView();
                });
                _tabs.Add(tab);
                navigation.Add(tab);
            }
            rootVisualElement.Add(navigation);

            _body = new VisualElement
            {
                name = "content"
            };

            _body.style.flexGrow = 1;
            _body.style.flexDirection = FlexDirection.Column;
            _body.style.overflow = Overflow.Hidden;
            _body.style.paddingLeft = 8;
            _body.style.paddingRight = 8;
            _body.style.paddingTop = 8;
            _body.style.paddingBottom = 8;

            rootVisualElement.Add(_body);
            VisualElement footer = CreateRow();
            footer.style.paddingLeft = 10;
            footer.style.paddingRight = 10;
            footer.style.minHeight = 30;
            footer.style.flexShrink = 0;
            _modeLabel = new Label();
            _modeLabel.style.color = Accent;
            _modeLabel.style.marginRight = 12;
            _statusLabel = new Label();
            _statusLabel.style.color = MutedColor();
            _statusLabel.style.flexGrow = 1;
            _statusLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            footer.Add(_modeLabel);
            footer.Add(_statusLabel);
            rootVisualElement.Add(footer);
        }

        private void CreateToolbar()
        {
            Toolbar toolbar = new Toolbar();

            _searchField = new ToolbarSearchField
            {
                value = string.Empty
            };

            _searchField.tooltip = "按资源位置搜索，仅影响资源明细；概览始终显示全部对象池。";
            _searchField.style.flexGrow = 1;
            _searchField.style.minWidth = 150;
            _searchField.style.marginRight = 6;

            _searchField.RegisterValueChangedCallback(_ => RefreshView());

            toolbar.Add(_searchField);

            ToolbarMenu sortMenu = new ToolbarMenu { text = "排序：资源名" };
            string[] sorts = { "资源名", "借出数量", "空闲数量", "等待请求", "实例内存" };
            for (int i = 0; i < sorts.Length; i++)
            {
                int index = i;
                sortMenu.menu.AppendAction(sorts[i], _ =>
                {
                    _sortIndex = index;
                    sortMenu.text = "排序：" + sorts[index];
                    RefreshView();
                }, _ => _sortIndex == index ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            }
            toolbar.Add(sortMenu);
            _onlyBusyToggle = new ToolbarToggle { text = "仅借出 / 等待" };
            _onlyBusyToggle.tooltip = "仅显示仍有借出实例或等待加载请求的资源。";
            _onlyBusyToggle.RegisterValueChangedCallback(_ => RefreshView());
            toolbar.Add(_onlyBusyToggle);
            _autoRefreshToggle = new ToolbarToggle
            {
                text = "自动刷新",
                value = true
            };
            _autoRefreshToggle.style.marginRight = 4;
            _autoRefreshToggle.tooltip = "播放模式下每 0.5 秒刷新一次。";
            _autoRefreshToggle.RegisterValueChangedCallback(_ => RefreshView());
            toolbar.Add(_autoRefreshToggle);


            Button refreshButton = new Button(ForceRefresh)
            {
                text = "立即刷新"
            };

            refreshButton.style.width = 75;

            toolbar.Add(refreshButton);

            rootVisualElement.Add(toolbar);
        }

        #endregion

        #region Refresh

        private void ForceRefresh()
        {
            _nextRefresh = 0;
            RefreshView();
        }

        private void RefreshView()
        {
            if (_body == null)
                return;

            if (_locationScroll != null)
                _locationScrollOffset = _locationScroll.scrollOffset.y;

            _body.Clear();
            _locationScroll = null;
            _modeLabel.text = Application.isPlaying ? (_autoRefreshToggle.value ? "自动刷新 · 0.5 s" : "手动刷新") : "等待播放";
            _statusLabel.text = Application.isPlaying ? "" : "进入播放模式以读取运行时对象池";
            _searchField.SetEnabled(_viewIndex == 0);
            _onlyBusyToggle.SetEnabled(_viewIndex == 0);

            if (!Application.isPlaying)
            {
                _body.Add(new HelpBox(
                    "进入播放模式后，这里将显示资源池统计与实例列表。",
                    HelpBoxMessageType.Info));

                return;
            }

            InstantiateManager manager = Global.Get<InstantiateManager>();

            if (manager == null)
            {
                _body.Add(new HelpBox(
                    "播放模式已开启，InstantiateManager 尚未初始化。",
                    HelpBoxMessageType.Warning));

                return;
            }

            RefreshData(manager);

            CreateOverview(manager);
            if (_viewIndex == 0) CreateLocations(manager);
            else if (_viewIndex == 1) CreateGroups();
            else CreateUnpooled();
            _statusLabel.text = $"{_locations.Count} 个资源 · 最近刷新 {DateTime.Now:HH:mm:ss}";
        }

        private void RefreshData(InstantiateManager manager)
        {
            _groups.Clear();
            _locations.Clear();
            _unpooled.Clear();

            manager.CopyDebugGroupStats(_groups);
            manager.CopyDebugLocationStats(_locations);
            manager.CopyDebugUnpooled(_unpooled);

            _locations.Sort((x, y) =>
            {
                int comparison = _sortIndex switch
                {
                    1 => y.Lent.CompareTo(x.Lent),
                    2 => y.Idle.CompareTo(x.Idle),
                    3 => y.Pending.CompareTo(x.Pending),
                    4 => y.MemoryBytes.CompareTo(x.MemoryBytes),
                    _ => 0
                };
                return comparison != 0 ? comparison : string.CompareOrdinal(x.Location, y.Location);
            });
        }

        #endregion

        #region Overview

        private void CreateOverview(InstantiateManager manager)
        {
            int active = 0;
            int idle = 0;

            foreach (InstantiateManager.DebugGroupStat group in _groups)
            {
                active += group.Active;
                idle += group.Idle;
            }

            VisualElement section = CreateSection("全局概览");
            VisualElement row = CreateRow();

            row.style.flexWrap = Wrap.Wrap;
            long memory = 0;
            foreach (InstantiateManager.DebugLocationStat location in _locations) memory += location.MemoryBytes;
            AddStat(row, "资源条目", manager.DebugEntryCount.ToString());
            AddStat(row, "借出实例", active.ToString());
            AddStat(row, "空闲实例", idle.ToString());
            AddStat(row, "等待请求", manager.DebugPendingCount.ToString());
            AddStat(row, "非池化实例", manager.DebugUnpooledCount.ToString());
            AddStat(row, "实例内存估算", FormatBytes(memory));

            section.Add(row);
            _body.Add(section);
        }

        private static void AddStat(
            VisualElement parent,
            string name,
            string value)
        {
            VisualElement container = new VisualElement();

            container.style.flexGrow = 1;
            container.style.flexBasis = 100;
            container.style.marginRight = 6;
            container.style.marginBottom = 4;
            container.style.paddingLeft = 10;
            container.style.paddingTop = 8;
            container.style.paddingBottom = 8;
            container.style.backgroundColor = EditorGUIUtility.isProSkin
                ? new Color(1f, 1f, 1f, 0.035f) : new Color(0f, 0f, 0f, 0.025f);
            container.style.borderTopLeftRadius = 5;
            container.style.borderTopRightRadius = 5;
            container.style.borderBottomLeftRadius = 5;
            container.style.borderBottomRightRadius = 5;

            if (name == "实例内存估算")
                container.tooltip = "调试接口估算的池内实例内存，不代表完整依赖资源或进程总内存。";
            Label nameLabel = new Label(name);

            nameLabel.style.fontSize = 10;
            nameLabel.style.color = EditorGUIUtility.isProSkin
                ? new Color(0.65f, 0.65f, 0.65f)
                : new Color(0.35f, 0.35f, 0.35f);

            Label valueLabel = new Label(value.ToString());

            valueLabel.style.fontSize = 21;
            valueLabel.style.color = Accent;
            valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            container.Add(nameLabel);
            container.Add(valueLabel);

            parent.Add(container);
        }

        #endregion

        #region Groups

        private void CreateGroups()
        {
            VisualElement section = CreateSection("分组统计");

            if (_groups.Count == 0)
            {
                section.Add(new HelpBox("当前没有对象池分组。", HelpBoxMessageType.Info));
                _body.Add(section);
                return;
            }

            VisualElement header = CreateRow();

            AddHeader(header, "资源分组", 2.5f, TextAnchor.MiddleLeft);
            AddHeader(header, "借出", 1f, TextAnchor.MiddleRight);
            AddHeader(header, "空闲", 1f, TextAnchor.MiddleRight);

            section.Add(header);

            foreach (InstantiateManager.DebugGroupStat group in _groups)
            {
                VisualElement row = CreateTableRow();

                AddCell(row, group.Group.ToString(), 2.5f, TextAnchor.MiddleLeft);
                AddCell(row, group.Active.ToString(), 1f, TextAnchor.MiddleRight);
                AddCell(row, group.Idle.ToString(), 1f, TextAnchor.MiddleRight);

                section.Add(row);
            }

            _body.Add(section);
        }

        #endregion

        #region Locations

        private void CreateLocations(InstantiateManager manager)
        {
            string search = _searchField?.value?.Trim() ?? string.Empty;
            VisualElement section = CreateSection("资源明细", true);
            VisualElement listBox = CreateInnerBox(out ScrollView list);
            int shown = 0;

            foreach (InstantiateManager.DebugLocationStat location in _locations)
            {
                if (!string.IsNullOrEmpty(search) &&
                    location.Location.IndexOf(
                        search,
                        StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                if (_onlyBusyToggle.value && location.Lent == 0 && location.Pending == 0) continue;
                shown++;
                VisualElement item = CreateLocation(manager, location);
                if (shown % 2 == 0)
                    item.style.backgroundColor = EditorGUIUtility.isProSkin
                        ? new Color(1f, 1f, 1f, 0.025f) : new Color(0f, 0f, 0f, 0.025f);
                list.Add(item);
            }

            Label title = (Label)section[0];
            title.text = $"资源明细  {shown} / {_locations.Count}";
            VisualElement columns = CreateRow();
            Label nameColumn = new Label("资源路径 / 展开查看实例");
            nameColumn.style.flexGrow = 1;
            nameColumn.style.flexBasis = 0;
            nameColumn.style.color = MutedColor();
            columns.Add(nameColumn);
            AddRightCell(columns, "分组", 70);
            AddRightCell(columns, "借出", 55);
            AddRightCell(columns, "空闲", 55);
            AddRightCell(columns, "等待", 60);
            AddRightCell(columns, "空闲时长", 85);
            AddRightCell(columns, "内存估算", 75);
            section.Add(columns);

            if (shown == 0)
            {
                section.Add(new HelpBox(
                    string.IsNullOrEmpty(search)
                        ? "当前没有符合筛选条件的资源条目。"
                        : "未找到匹配的资源，请调整搜索内容或筛选条件。",
                    HelpBoxMessageType.Warning));
            }
            else
            {
                _locationScroll = list;
                float offset = _locationScrollOffset;
                list.schedule.Execute(() =>
                {
                    if (_locationScroll != list)
                        return;

                    list.scrollOffset = new Vector2(0f, offset);
                });
                section.Add(listBox);
            }

            _body.Add(section);
        }

        private VisualElement CreateLocation(
            InstantiateManager manager,
            InstantiateManager.DebugLocationStat location)
        {
            VisualElement container = new VisualElement();

            container.style.marginBottom = 2;

            bool expanded =
                _expandedLocations.Contains(location.Location);

            VisualElement header = CreateLocationHeader(location);

            Foldout foldout = header.Q<Foldout>();
            foldout.value = expanded;

            foldout.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                    _expandedLocations.Add(location.Location);
                else
                    _expandedLocations.Remove(location.Location);

                RefreshView();
            });

            container.Add(header);

            if (expanded)
            {
                _instances.Clear();

                manager.TryCopyDebugInstances(
                    location.Location,
                    _instances);

                VisualElement instanceContainer =
                    new VisualElement();

                instanceContainer.style.marginLeft = 20;
                instanceContainer.style.paddingTop = 2;
                instanceContainer.style.paddingBottom = 4;

                foreach (InstantiateManager.DebugInstanceInfo info in _instances)
                {
                    instanceContainer.Add(CreateInstanceRow(info));
                }

                if (_instances.Count == 0)
                {
                    instanceContainer.Add(new HelpBox(
                        "该资源当前没有可查看的实例。",
                        HelpBoxMessageType.Info));
                }

                container.Add(instanceContainer);
            }

            return container;
        }

        private static VisualElement CreateLocationHeader(
            InstantiateManager.DebugLocationStat location)
        {
            VisualElement row = new VisualElement();

            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.minHeight = 30;

            Foldout foldout = new Foldout
            {
                text = location.Location,
                tooltip = location.Location
            };

            foldout.style.flexGrow = 1;
            foldout.style.flexBasis = 0;
            foldout.style.marginRight = 4;
            Label locationLabel = foldout.Q<Label>();
            locationLabel.style.overflow = Overflow.Hidden;
            locationLabel.style.textOverflow = TextOverflow.Ellipsis;

            row.Add(foldout);

            AddRightCell(
                row,
                location.Group.ToString(),
                70);

            AddRightCell(
                row,
                location.Lent.ToString(),
                55);

            AddRightCell(
                row,
                location.Idle.ToString(),
                55);

            AddRightCell(
                row,
                location.Pending.ToString(),
                60);

            AddRightCell(
                row,
                FormatTime(location.IdleElapsed),
                85);

            AddRightCell(
                row,
                FormatBytes(location.MemoryBytes),
                75);

            return row;
        }

        #endregion

        #region Instances

        private static VisualElement CreateInstanceRow(
            InstantiateManager.DebugInstanceInfo info)
        {
            VisualElement row = new VisualElement();

            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.minHeight = 26;

            ObjectField objectField =
                CreateReadonlyObjectField(info.Instance);

            objectField.style.flexGrow = 1;
            objectField.style.flexBasis = 0;
            objectField.style.marginRight = 8;

            row.Add(objectField);

            Label state = new Label(
                info.Active ? "借出" : "空闲");

            state.style.width = 55;
            state.style.color = new Color(0.3f, 0.75f, 0.5f);
            state.style.unityTextAlign = TextAnchor.MiddleRight;
            state.style.fontSize = 10;
            state.style.unityFontStyleAndWeight = FontStyle.Bold;

            if (!info.Active)
            {
                state.style.color = EditorGUIUtility.isProSkin
                    ? new Color(0.55f, 0.55f, 0.55f)
                    : new Color(0.45f, 0.45f, 0.45f);
            }

            row.Add(state);

            return row;
        }

        private static ObjectField CreateReadonlyObjectField(
            GameObject instance)
        {
            ObjectField field = new ObjectField
            {
                value = instance,
                objectType = typeof(GameObject),
                allowSceneObjects = true
            };

            VisualElement overlay = new VisualElement();

            overlay.style.position = Position.Absolute;
            overlay.style.left = 0;
            overlay.style.right = 0;
            overlay.style.top = 0;
            overlay.style.bottom = 0;

            overlay.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (instance == null)
                    return;

                Selection.activeGameObject = instance;
                EditorGUIUtility.PingObject(instance);

                evt.StopImmediatePropagation();
            });

            field.Add(overlay);

            return field;
        }

        #endregion

        #region Unpooled

        private void CreateUnpooled()
        {
            if (_unpooled.Count == 0)
            {
                _body.Add(new HelpBox("当前没有非池化实例。", HelpBoxMessageType.Info));
                return;
            }

            VisualElement section = CreateSection($"非池化实例 ({_unpooled.Count})", true);
            ScrollView instances = new ScrollView(ScrollViewMode.Vertical);

            instances.style.flexGrow = 1;
            instances.style.minHeight = 100;
            instances.style.flexShrink = 1;

            foreach (GameObject instance in _unpooled)
            {
                if (instance == null)
                    continue;

                InstantiateManager.DebugInstanceInfo info =
                    new InstantiateManager.DebugInstanceInfo
                    {
                        Instance = instance,
                        Active = instance.activeSelf
                    };

                instances.Add(CreateInstanceRow(info));
            }

            section.Add(instances);

            _body.Add(section);
        }

        #endregion

        #region Common UI

        private static VisualElement CreateSection(string title, bool expand = false)
        {
            VisualElement section = new VisualElement();

            ApplyHelpBox(section);
            section.style.marginBottom = 8;
            section.style.flexShrink = expand ? 1 : 0;

            if (expand)
            {
                section.style.flexGrow = 1;
                section.style.minHeight = 120;
                section.style.overflow = Overflow.Hidden;
            }

            Label titleLabel = new Label(title);

            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 8;
            titleLabel.style.fontSize = 12;

            section.Add(titleLabel);

            return section;
        }

        private static VisualElement CreateInnerBox(out ScrollView scroll)
        {
            scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            scroll.style.flexShrink = 1;
            scroll.style.minHeight = 0;

            VisualElement box = new VisualElement();

            ApplyHelpBox(box);
            box.style.flexGrow = 1;
            box.style.flexShrink = 1;
            box.style.minHeight = 0;
            box.style.overflow = Overflow.Hidden;
            box.Add(scroll);

            return box;
        }

        private static VisualElement CreateRow()
        {
            VisualElement row = new VisualElement();

            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.minHeight = 28;

            return row;
        }

        private static VisualElement CreateTableRow()
        {
            VisualElement row = CreateRow();

            row.style.paddingLeft = 4;
            row.style.paddingRight = 4;

            return row;
        }

        private static void AddHeader(
            VisualElement parent,
            string text,
            float flexGrow,
            TextAnchor align)
        {
            Label label = new Label(text);

            label.style.flexGrow = flexGrow;
            label.style.flexBasis = 0;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.fontSize = 10;
            label.style.unityTextAlign = align;

            parent.Add(label);
        }

        private static void AddCell(
            VisualElement parent,
            string text,
            float flexGrow,
            TextAnchor align)
        {
            Label label = new Label(text);

            label.style.flexGrow = flexGrow;
            label.style.flexBasis = 0;
            label.style.unityTextAlign = align;

            parent.Add(label);
        }

        private static void ApplyHelpBox(VisualElement element)
        {
            GUIStyle style = EditorStyles.helpBox;
            Texture2D background = style.normal.background;

            if (background == null &&
                style.normal.scaledBackgrounds != null &&
                style.normal.scaledBackgrounds.Length > 0)
            {
                background = style.normal.scaledBackgrounds[0];
            }

            if (background != null)
                element.style.backgroundImage = background;

            element.style.unitySliceLeft = style.border.left;
            element.style.unitySliceRight = style.border.right;
            element.style.unitySliceTop = style.border.top;
            element.style.unitySliceBottom = style.border.bottom;
            element.style.paddingLeft = style.padding.left;
            element.style.paddingRight = style.padding.right;
            element.style.paddingTop = style.padding.top;
            element.style.paddingBottom = style.padding.bottom;
        }

        private static void AddRightCell(
            VisualElement parent,
            string text,
            float width)
        {
            Label label = new Label(text);

            label.style.width = width;
            label.style.unityTextAlign = TextAnchor.MiddleRight;
            label.style.fontSize = 10;

            parent.Add(label);
        }

        #endregion

        #region Utilities

        private static Color MutedColor()
        {
            return EditorGUIUtility.isProSkin ? new Color(0.65f, 0.68f, 0.72f) : new Color(0.38f, 0.4f, 0.43f);
        }

        private static string FormatTime(float seconds)
        {
            if (seconds < 60f)
                return $"{seconds:F1}s";

            if (seconds < 3600f)
            {
                int minutes = Mathf.FloorToInt(seconds / 60f);
                int remainingSeconds =
                    Mathf.FloorToInt(seconds - minutes * 60f);

                return $"{minutes}m {remainingSeconds}s";
            }

            int hours = Mathf.FloorToInt(seconds / 3600f);
            int remainingMinutes =
                Mathf.FloorToInt(
                    (seconds - hours * 3600f) / 60f);

            return $"{hours}h {remainingMinutes}m";
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";

            if (bytes < 1024 * 1024)
                return $"{bytes / 1024f:F1} KB";

            if (bytes < 1024L * 1024L * 1024L)
                return $"{bytes / (1024f * 1024f):F1} MB";

            return $"{bytes / (1024f * 1024f * 1024f):F2} GB";
        }

        #endregion
    }
}

#endif
