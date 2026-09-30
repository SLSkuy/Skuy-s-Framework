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
        private const double RefreshInterval = 0.5d;

        private readonly List<InstantiateManager.DebugGroupStat> _groups = new();
        private readonly List<InstantiateManager.DebugLocationStat> _locations = new();
        private readonly List<InstantiateManager.DebugInstanceInfo> _instances = new();
        private readonly List<GameObject> _unpooled = new();
        private readonly HashSet<string> _expandedLocations = new();

        private ToolbarToggle _autoRefreshToggle;
        private VisualElement _body;
        private ScrollView _locationScroll;
        private TextField _searchField;

        private float _locationScrollOffset;

        private bool _unpooledExpanded;
        private double _nextRefresh;

        [MenuItem("Tools/Framework/Instantiate Pool")]
        public static void ShowWindow()
        {
            InstantiateManagerDebugWindow window =
                GetWindow<InstantiateManagerDebugWindow>("Instantiate Pool");

            window.minSize = new Vector2(760, 500);
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
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

            _nextRefresh = EditorApplication.timeSinceStartup + RefreshInterval;
            RefreshView();
        }

        #region Window

        private void BuildWindow()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.flexDirection = FlexDirection.Column;

            CreateToolbar();

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
        }

        private void CreateToolbar()
        {
            Toolbar toolbar = new Toolbar();

            _searchField = new TextField
            {
                value = string.Empty
            };

            _searchField.style.flexGrow = 1;
            _searchField.style.marginRight = 6;

            _searchField.RegisterValueChangedCallback(_ => RefreshView());

            toolbar.Add(_searchField);

            _autoRefreshToggle = new ToolbarToggle
            {
                text = "Auto",
                value = true
            };
            _autoRefreshToggle.style.marginRight = 4;
            toolbar.Add(_autoRefreshToggle);


            Button refreshButton = new Button(ForceRefresh)
            {
                text = "Refresh"
            };

            refreshButton.style.width = 65;

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

            if (!Application.isPlaying)
            {
                _body.Add(new HelpBox(
                    "Enter Play Mode to inspect InstantiateManager.",
                    HelpBoxMessageType.Info));

                return;
            }

            InstantiateManager manager = Global.Get<InstantiateManager>();

            if (manager == null)
            {
                _body.Add(new HelpBox(
                    "InstantiateManager has not been initialized.",
                    HelpBoxMessageType.Warning));

                return;
            }

            RefreshData(manager);

            CreateOverview(manager);
            CreateLocations(manager);
            CreateGroups();
            CreateUnpooled();
        }

        private void RefreshData(InstantiateManager manager)
        {
            _groups.Clear();
            _locations.Clear();
            _unpooled.Clear();

            manager.CopyDebugGroupStats(_groups);
            manager.CopyDebugLocationStats(_locations);
            manager.CopyDebugUnpooled(_unpooled);

            _locations.Sort(
                (x, y) => string.CompareOrdinal(x.Location, y.Location));
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

            VisualElement section = CreateSection("POOL");
            VisualElement row = CreateRow();

            AddStat(row, "Entries", manager.DebugEntryCount);
            AddStat(row, "Active", active);
            AddStat(row, "Idle", idle);
            AddStat(row, "Pending", manager.DebugPendingCount);
            AddStat(row, "Unpooled", manager.DebugUnpooledCount);

            section.Add(row);
            _body.Add(section);
        }

        private static void AddStat(
            VisualElement parent,
            string name,
            int value)
        {
            VisualElement container = new VisualElement();

            container.style.flexGrow = 1;
            container.style.flexBasis = 0;
            container.style.marginRight = 8;

            Label nameLabel = new Label(name);

            nameLabel.style.fontSize = 10;
            nameLabel.style.color = EditorGUIUtility.isProSkin
                ? new Color(0.65f, 0.65f, 0.65f)
                : new Color(0.35f, 0.35f, 0.35f);

            Label valueLabel = new Label(value.ToString());

            valueLabel.style.fontSize = 16;
            valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            container.Add(nameLabel);
            container.Add(valueLabel);

            parent.Add(container);
        }

        #endregion

        #region Groups

        private void CreateGroups()
        {
            VisualElement section = CreateSection("Groups");

            if (_groups.Count == 0)
            {
                section.Add(new HelpBox("No active groups.", HelpBoxMessageType.Info));
                _body.Add(section);
                return;
            }

            VisualElement header = CreateRow();

            AddHeader(header, "Group", 2.5f, TextAnchor.MiddleLeft);
            AddHeader(header, "Active", 1f, TextAnchor.MiddleRight);
            AddHeader(header, "Idle", 1f, TextAnchor.MiddleRight);

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
            VisualElement section = CreateSection("Resource Locations", true);
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

                shown++;
                list.Add(CreateLocation(manager, location));
            }

            Label title = (Label)section[0];
            title.text = $"Resource Locations ({shown})";

            if (shown == 0)
            {
                section.Add(new HelpBox(
                    string.IsNullOrEmpty(search)
                        ? "No resource entries."
                        : "No matching resources.",
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
                        "No instances.",
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
            row.style.minHeight = 22;

            Foldout foldout = new Foldout
            {
                text = location.Location
            };

            foldout.style.flexGrow = 1;
            foldout.style.flexBasis = 0;
            foldout.style.marginRight = 4;

            row.Add(foldout);

            AddRightCell(
                row,
                location.Group.ToString(),
                70);

            AddRightCell(
                row,
                $"A {location.Lent}",
                55);

            AddRightCell(
                row,
                $"I {location.Idle}",
                55);

            AddRightCell(
                row,
                $"P {location.Pending}",
                60);

            AddRightCell(
                row,
                $"Idle {FormatTime(location.IdleElapsed)}",
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
                info.Active ? "ACTIVE" : "IDLE");

            state.style.width = 55;
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
                return;

            Foldout foldout = new Foldout
            {
                text = $"Unpooled ({_unpooled.Count})",
                value = _unpooledExpanded
            };

            foldout.style.flexShrink = 0;
            foldout.style.marginBottom = 4;

            foldout.RegisterValueChangedCallback(evt =>
            {
                _unpooledExpanded = evt.newValue;
                RefreshView();
            });

            _body.Add(foldout);

            if (!_unpooledExpanded)
                return;

            VisualElement section = CreateSection("Instances");
            ScrollView instances = new ScrollView(ScrollViewMode.Vertical);

            instances.style.maxHeight = 160;
            instances.style.flexShrink = 0;

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
            titleLabel.style.marginBottom = 5;

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
            row.style.minHeight = 24;

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
