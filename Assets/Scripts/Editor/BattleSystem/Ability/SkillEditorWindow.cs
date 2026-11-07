using System;
using System.Linq;
using GamePlay.BattleSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Editor
{
    /// <summary>编辑技能表中的技能效果，在独立场景中预览动画和位移。</summary>
    public class SkillEditorWindow : EditorWindow
    {
        [SerializeField] private SkillTable table;
        [SerializeField] private GameObject modelPrefab;
        [SerializeField] private int selectedSkill = -1;
        [SerializeField] private int tickRate = 60;
        [SerializeField] private bool loopPreview;
        [SerializeField] private float trackHeight = 240;
        private TimelineAsset _timeline;
        private TimelineClip[] _timelineClips = Array.Empty<TimelineClip>();
        private float _resizeMouseY;
        private float _resizeHeight;
        private SerializedObject _serializedTable;
        private Type[] _effectTypes;
        private PreviewRenderUtility _preview;
        private GameObject _model;
        private Transform[] _bones;
        private int[] _animationRootIndices;
        private Vector3[] _positions;
        private Quaternion[] _rotations;
        private Vector3[] _scales;
        private Vector2 _skillScroll;
        private Vector2 _effectScroll;
        private Vector2 _trackScroll;
        private float _pixelsPerTick = 8;
        private int _dragTrack = -1;
        private int _selectedTrack = -1;
        private int _dragEdge;
        private int _trackUndoGroup;
        private float _dragMouseX;
        private long _dragStart;
        private long _dragDuration;
        private bool _scrubbing;
        private string _search = "";
        private float _tick;
        private bool _playing;
        private double _lastTime;
        private Vector3 _viewPivot = Vector3.up;
        private Vector2 _viewAngles = new Vector2(15, -34);
        private float _viewDistance = 7.5f;
        private int _viewButton;
        private Animator _animator;
        private PlayableGraph _graph;
        private AnimationClipPlayable _clipPlayable;
        private AnimationClip _previewClip;
        private readonly System.Collections.Generic.List<Material> _previewMaterials = new();

        [MenuItem("Tools/BattleSystem/技能编辑器")]
        public static void Open()
        {
            GetWindow<SkillEditorWindow>("技能编辑器").Show();
        }

        private void OnEnable()
        {
            minSize = new Vector2(1000, 600);
            _effectTypes = TypeCache.GetTypesDerivedFrom<SkillEffect>()
                .Where(type => !type.IsAbstract && !type.ContainsGenericParameters && type.IsSerializable &&
                    type.GetConstructor(Type.EmptyTypes) != null).OrderBy(type => type.FullName).ToArray();
            if (!table)
            {
                string guid = AssetDatabase.FindAssets("t:SkillTable").FirstOrDefault();
                if (guid != null) table = AssetDatabase.LoadAssetAtPath<SkillTable>(AssetDatabase.GUIDToAssetPath(guid));
            }
            BindTable();
            _preview = new PreviewRenderUtility();
            _preview.camera.fieldOfView = 35;
            _preview.lights[0].intensity = 1.2f;
            _preview.lights[0].transform.rotation = Quaternion.Euler(40, 30, 0);
            CreateEnvironment();
            RebuildModel();
            Undo.undoRedoPerformed += OnUndo;
            EditorApplication.update += OnUpdate;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndo;
            EditorApplication.update -= OnUpdate;
            ClearAnimation();
            ClearTimeline();
            _preview?.Cleanup();
            foreach (Material material in _previewMaterials) DestroyImmediate(material);
            _previewMaterials.Clear();
            _serializedTable?.Dispose();
        }

        private void BindTable()
        {
            _serializedTable?.Dispose();
            _serializedTable = table ? new SerializedObject(table) : null;
            selectedSkill = table && table.skills != null && table.skills.Length > 0
                ? Mathf.Clamp(selectedSkill, 0, table.skills.Length - 1) : -1;
            _tick = 0;
            _playing = false;
        }

        private void OnUndo()
        {
            BindTable();
            Repaint();
        }

        private void OnUpdate()
        {
            double now = EditorApplication.timeSinceStartup;
            if (_playing && table && selectedSkill >= 0 && selectedSkill < table.skills.Length)
            {
                _tick += (float)(now - _lastTime) * tickRate;
                float end = table.skills[selectedSkill].durationTicks;
                if (_tick >= end)
                {
                    if (loopPreview && end > 0) _tick %= end;
                    else { _tick = end; _playing = false; }
                }
                Repaint();
            }
            _lastTime = now;
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUI.BeginChangeCheck();
                table = (SkillTable)EditorGUILayout.ObjectField(table, typeof(SkillTable), false, GUILayout.Width(280));
                if (EditorGUI.EndChangeCheck()) BindTable();
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(!table))
                    if (GUILayout.Button("保存", EditorStyles.toolbarButton)) AssetDatabase.SaveAssetIfDirty(table);
            }
            if (!table) { EditorGUILayout.HelpBox("请选择 SkillTable。", MessageType.Info); return; }
            _serializedTable.Update();
            SerializedProperty skills = _serializedTable.FindProperty("skills");
            if (selectedSkill >= skills.arraySize) selectedSkill = -1;
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawSkills(skills);
                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                        {
                            if (selectedSkill >= 0) DrawPreview(table.skills[selectedSkill]);
                        }
                        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(330)))
                        {
                            if (selectedSkill >= 0) DrawEffects(skills.GetArrayElementAtIndex(selectedSkill));
                        }
                    }
                    if (selectedSkill >= 0) DrawTracks(skills.GetArrayElementAtIndex(selectedSkill));
                }
            }
            _serializedTable.ApplyModifiedProperties();
        }

        private void DrawSkills(SerializedProperty skills)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(230)))
            {
                _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
                _skillScroll = EditorGUILayout.BeginScrollView(_skillScroll);
                for (int i = 0; i < skills.arraySize; i++)
                {
                    SerializedProperty skill = skills.GetArrayElementAtIndex(i);
                    string label = $"{skill.FindPropertyRelative("skillId").longValue}  {skill.FindPropertyRelative("skillName").stringValue}";
                    if (label.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    if (GUILayout.Toggle(selectedSkill == i, label, "Button") && selectedSkill != i)
                    { selectedSkill = i; _tick = 0; _playing = false; }
                }
                EditorGUILayout.EndScrollView();
                if (GUILayout.Button("新建技能"))
                {
                    _serializedTable.ApplyModifiedProperties();
                    uint id = 1;
                    var usedIds = new System.Collections.Generic.HashSet<uint>(
                        (table.skills ?? Array.Empty<SkillConfig>()).Select(item => item.skillId));
                    while (usedIds.Contains(id)) id = checked(id + 1);
                    Undo.RecordObject(table, "新建技能");
                    SkillConfig[] entries = table.skills ?? Array.Empty<SkillConfig>();
                    selectedSkill = entries.Length;
                    Array.Resize(ref entries, entries.Length + 1);
                    entries[selectedSkill] = new SkillConfig { skillId = id, skillName = "新技能", durationTicks = (uint)tickRate };
                    table.skills = entries;
                    EditorUtility.SetDirty(table);
                    _serializedTable.Update();
                    _search = "";
                    _tick = 0;
                    _playing = false;
                }
                using (new EditorGUI.DisabledScope(selectedSkill < 0))
                {
                    if (GUILayout.Button("删除技能"))
                    {
                        skills.DeleteArrayElementAtIndex(selectedSkill);
                        _serializedTable.ApplyModifiedProperties();
                        selectedSkill = Mathf.Min(selectedSkill, skills.arraySize - 1);
                        _tick = 0;
                        _playing = false;
                    }
                }
            }
        }

        private void DrawEffects(SerializedProperty skill)
        {
            _effectScroll = EditorGUILayout.BeginScrollView(_effectScroll);
            EditorGUILayout.PropertyField(skill.FindPropertyRelative("skillId"), new GUIContent("技能 ID"));
            EditorGUILayout.PropertyField(skill.FindPropertyRelative("skillName"), new GUIContent("名称"));
            EditorGUILayout.PropertyField(skill.FindPropertyRelative("cooldown"), new GUIContent("冷却"));
            EditorGUILayout.PropertyField(skill.FindPropertyRelative("durationTicks"), new GUIContent("持续 Tick"));
            SerializedProperty effects = skill.FindPropertyRelative("effects");
            for (int i = 0; i < effects.arraySize; i++)
            {
                SerializedProperty effect = effects.GetArrayElementAtIndex(i);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        string name = effect.managedReferenceValue?.GetType().Name ?? "缺失效果";
                        effect.isExpanded = EditorGUILayout.Foldout(effect.isExpanded, name, true);
                        if (GUILayout.Button("↑", GUILayout.Width(24)) && i > 0)
                        { effects.MoveArrayElement(i, i - 1); break; }
                        if (GUILayout.Button("↓", GUILayout.Width(24)) && i < effects.arraySize - 1)
                        { effects.MoveArrayElement(i, i + 1); break; }
                        if (GUILayout.Button("×", GUILayout.Width(24)))
                        { effects.DeleteArrayElementAtIndex(i); break; }
                    }
                    if (effect.isExpanded)
                    {
                        SerializedProperty child = effect.Copy();
                        SerializedProperty end = child.GetEndProperty();
                        bool enter = true;
                        while (child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end))
                        { EditorGUILayout.PropertyField(child, true); enter = false; }
                    }
                }
            }
            EditorGUILayout.Space();
            if (EditorGUILayout.DropdownButton(new GUIContent("添加效果"), FocusType.Keyboard))
            {
                var menu = new GenericMenu();
                SkillTable target = table;
                int skillIndex = selectedSkill;
                foreach (Type type in _effectTypes)
                    menu.AddItem(new GUIContent(type.Name), false, () => AddEffect(target, skillIndex, type));
                menu.ShowAsContext();
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawTracks(SerializedProperty skill)
        {
            DrawTrackSplitter();
            SyncTimeline(skill);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Height(Mathf.Clamp(trackHeight, 140, Mathf.Max(140, position.height - 220)))))
            {
                DrawPlaybackControls(skill.FindPropertyRelative("durationTicks").longValue);
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    GUILayout.Label("技能编辑轨道", EditorStyles.miniBoldLabel);
                    GUILayout.FlexibleSpace();
                    using (new EditorGUI.DisabledScope(_dragTrack >= 0))
                        _pixelsPerTick = GUILayout.HorizontalSlider(_pixelsPerTick, 2, 24, GUILayout.Width(100));
                    if (GUILayout.Button("添加轨道", EditorStyles.toolbarDropDown, GUILayout.Width(85)))
                    {
                        var menu = new GenericMenu();
                        SkillTable target = table;
                        int index = selectedSkill;
                        foreach (Type type in _effectTypes)
                            menu.AddItem(new GUIContent(type.Name), false, () => AddEffect(target, index, type));
                        menu.ShowAsContext();
                    }
                }
                SerializedProperty effects = skill.FindPropertyRelative("effects");
                long length = Math.Max(1, skill.FindPropertyRelative("durationTicks").longValue);
                for (int i = 0; i < effects.arraySize; i++)
                {
                    SerializedProperty effect = effects.GetArrayElementAtIndex(i);
                    if (effect.managedReferenceValue == null) continue;
                    length = Math.Max(length, effect.FindPropertyRelative("startTick").longValue +
                        effect.FindPropertyRelative("durationTicks").longValue);
                }
                Rect viewport = GUILayoutUtility.GetRect(0, 70, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                Event wheel = Event.current;
                if (wheel.type == EventType.ScrollWheel && wheel.control && viewport.Contains(wheel.mousePosition) && _dragTrack < 0 && !_scrubbing)
                {
                    float anchor = Mathf.Max(0, (wheel.mousePosition.x - viewport.x + _trackScroll.x - 180) / _pixelsPerTick);
                    float previousScale = _pixelsPerTick;
                    _pixelsPerTick = Mathf.Clamp(previousScale * Mathf.Exp(-wheel.delta.y * 0.08f), 2, 24);
                    _trackScroll.x = Mathf.Max(0, _trackScroll.x + anchor * (_pixelsPerTick - previousScale));
                    wheel.Use();
                    Repaint();
                }
                Rect canvas = new Rect(0, 0, Mathf.Max(viewport.width - 16, 180 + length * _pixelsPerTick + 20),
                    Mathf.Max(viewport.height - 16, 28 + effects.arraySize * 38));
                _trackScroll = GUI.BeginScrollView(viewport, _trackScroll, canvas);
                EditorGUI.DrawRect(canvas, new Color(0.13f, 0.13f, 0.13f));
                EditorGUI.DrawRect(new Rect(canvas.x, canvas.y, canvas.width, 24), new Color(0.19f, 0.19f, 0.19f));
                float origin = canvas.x + 180;
                int control = GUIUtility.GetControlID(FocusType.Keyboard);
                Event current = Event.current;
                if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Space &&
                    GUIUtility.keyboardControl == control && !EditorGUIUtility.editingTextField && GUIUtility.hotControl == 0)
                {
                    TogglePlayback(skill.FindPropertyRelative("durationTicks").longValue);
                    current.Use();
                }
                Rect ruler = new Rect(origin, canvas.y, canvas.width - 180, 24);
                if (current.type == EventType.MouseDown && current.button == 0 && ruler.Contains(current.mousePosition))
                {
                    GUIUtility.hotControl = control;
                    GUIUtility.keyboardControl = control;
                    EditorGUIUtility.editingTextField = false;
                    _scrubbing = true;
                    _playing = false;
                    _tick = Mathf.Clamp(Mathf.Round((current.mousePosition.x - origin) / _pixelsPerTick), 0,
                        skill.FindPropertyRelative("durationTicks").longValue);
                    current.Use();
                    Repaint();
                }
                if (GUIUtility.hotControl == control && _scrubbing)
                {
                    if (current.type == EventType.MouseDrag)
                    {
                        _tick = Mathf.Clamp(Mathf.Round((current.mousePosition.x - origin) / _pixelsPerTick), 0,
                            skill.FindPropertyRelative("durationTicks").longValue);
                        current.Use();
                        Repaint();
                    }
                    else if (current.rawType == EventType.MouseUp)
                    {
                        _scrubbing = false;
                        GUIUtility.hotControl = 0;
                        current.Use();
                    }
                }
                int step = Mathf.Max(1, Mathf.CeilToInt(45 / _pixelsPerTick));
                // 仅绘制可见刻度，长技能不会生成与总 Tick 数等量的 GUI 操作。
                long first = Math.Max(0, (long)((_trackScroll.x - 180) / _pixelsPerTick) / step * step);
                long last = Math.Min(length, (long)((_trackScroll.x + position.width) / _pixelsPerTick));
                for (long tick = first; tick <= last; tick += step)
                {
                    float x = origin + tick * _pixelsPerTick;
                    EditorGUI.DrawRect(new Rect(x, canvas.y + 22, 1, canvas.height - 22), new Color(1, 1, 1, 0.08f));
                    GUI.Label(new Rect(x + 2, canvas.y, 55, 20), tick.ToString(), EditorStyles.miniLabel);
                }
                for (int i = 0; i < effects.arraySize; i++)
                {
                    SerializedProperty effect = effects.GetArrayElementAtIndex(i);
                    if (effect.managedReferenceValue == null) continue;
                    TimelineClip clip = _timelineClips[i];
                    long start = (long)Math.Round(clip.start * tickRate);
                    long duration = (long)Math.Round(clip.duration * tickRate);
                    float y = canvas.y + 28 + i * 38;
                    EditorGUI.DrawRect(new Rect(canvas.x, y - 2, canvas.width, 36), new Color(1, 1, 1, i % 2 == 0 ? 0.025f : 0.055f));
                    EditorGUI.DrawRect(new Rect(canvas.x, y - 2, 178, 36), new Color(0.20f, 0.20f, 0.20f));
                    EditorGUI.DrawRect(new Rect(canvas.x, y - 2, 3, 36), new Color(0.28f, 0.55f, 0.72f));
                    GUI.Label(new Rect(canvas.x + 10, y + 2, 165, 24), clip.displayName, EditorStyles.miniBoldLabel);
                    Rect bar = new Rect(origin + start * _pixelsPerTick, y, Mathf.Max(12, duration * _pixelsPerTick), 24);
                    EditorGUI.DrawRect(bar, _selectedTrack == i ? new Color(0.28f, 0.52f, 0.68f) : new Color(0.23f, 0.33f, 0.40f));
                    EditorGUI.DrawRect(new Rect(bar.x, bar.y, bar.width, 3), new Color(0.4f, 0.7f, 0.88f));
                    GUI.Label(new Rect(bar.x + 8, bar.y + 2, Mathf.Max(0, bar.width - 16), 20), new GUIContent($"{start} – {start + duration}", clip.displayName), EditorStyles.whiteMiniLabel);
                    float edgeWidth = Mathf.Min(8, bar.width * 0.5f);
                    Rect left = new Rect(bar.x, bar.y, edgeWidth, bar.height);
                    Rect right = new Rect(bar.xMax - edgeWidth, bar.y, edgeWidth, bar.height);
                    EditorGUI.DrawRect(left, new Color(1, 1, 1, 0.4f));
                    EditorGUI.DrawRect(right, new Color(1, 1, 1, 0.4f));
                    // 光标区域互不重叠，避免整段移动光标覆盖两端的拉伸光标。
                    if (_dragTrack < 0)
                    {
                        Rect middle = new Rect(left.xMax, bar.y, Mathf.Max(0, bar.width - edgeWidth * 2), bar.height);
                        EditorGUIUtility.AddCursorRect(middle, MouseCursor.MoveArrow, control);
                        EditorGUIUtility.AddCursorRect(left, MouseCursor.ResizeHorizontal, control);
                        EditorGUIUtility.AddCursorRect(right, MouseCursor.ResizeHorizontal, control);
                    }
                    if (current.type == EventType.MouseDown && current.button == 0 && bar.Contains(current.mousePosition))
                    {
                        GUIUtility.hotControl = control;
                        GUIUtility.keyboardControl = control;
                        EditorGUIUtility.editingTextField = false;
                        _selectedTrack = i;
                        _dragTrack = i;
                        _dragEdge = left.Contains(current.mousePosition) ? -1 : right.Contains(current.mousePosition) ? 1 : 0;
                        _dragMouseX = current.mousePosition.x;
                        _dragStart = start;
                        _dragDuration = duration;
                        _playing = false;
                        Undo.IncrementCurrentGroup();
                        _trackUndoGroup = Undo.GetCurrentGroup();
                        Undo.SetCurrentGroupName("编辑技能轨道");
                        current.Use();
                    }
                }
                if (GUIUtility.hotControl == control && _dragTrack >= 0)
                {
                    EditorGUIUtility.AddCursorRect(canvas, _dragEdge == 0 ? MouseCursor.MoveArrow : MouseCursor.ResizeHorizontal, control);
                    if (current.type == EventType.MouseDrag)
                    {
                        long delta = (long)Math.Round((current.mousePosition.x - _dragMouseX) / _pixelsPerTick);
                        long end = _dragStart + _dragDuration;
                        long start = _dragStart;
                        long limit = Math.Max(0, skill.FindPropertyRelative("durationTicks").longValue);
                        if (_dragEdge < 0) start = Math.Clamp(_dragStart + delta, 0, end);
                        else if (_dragEdge > 0) end = Math.Clamp(end + delta, start, Math.Max(start, limit));
                        else
                        {
                            start = Math.Clamp(_dragStart + delta, 0, Math.Max(0, limit - _dragDuration));
                            end = start + _dragDuration;
                        }
                        SerializedProperty effect = effects.GetArrayElementAtIndex(_dragTrack);
                        TimelineClip clip = _timelineClips[_dragTrack];
                        clip.start = (double)start / tickRate;
                        clip.duration = Math.Max(0.000001, (double)(end - start) / tickRate);
                        effect.FindPropertyRelative("startTick").longValue = (long)Math.Round(clip.start * tickRate);
                        effect.FindPropertyRelative("durationTicks").longValue = end == start ? 0 : (long)Math.Round(clip.duration * tickRate);
                        current.Use();
                        Repaint();
                    }
                    else if (current.rawType == EventType.MouseUp)
                    {
                        _serializedTable.ApplyModifiedProperties();
                        Undo.CollapseUndoOperations(_trackUndoGroup);
                        _dragTrack = -1;
                        GUIUtility.hotControl = 0;
                        current.Use();
                    }
                }
                if (current.type == EventType.MouseDown && current.button == 0 && canvas.Contains(current.mousePosition))
                {
                    GUIUtility.keyboardControl = control;
                    EditorGUIUtility.editingTextField = false;
                    int row = Mathf.FloorToInt((current.mousePosition.y - canvas.y - 28) / 38);
                    _selectedTrack = row >= 0 && row < effects.arraySize ? row : -1;
                    current.Use();
                    Repaint();
                }
                float playhead = origin + _tick * _pixelsPerTick;
                EditorGUI.DrawRect(new Rect(playhead, canvas.y + 22, 1, canvas.height - 22), new Color(0.9f, 0.9f, 0.9f));
                EditorGUI.DrawRect(new Rect(playhead - 4, canvas.y + 15, 9, 9), new Color(0.4f, 0.7f, 0.88f));
                GUI.EndScrollView();
            }
        }

        private void DrawPlaybackControls(long duration)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button(_playing ? "暂停" : "播放", EditorStyles.toolbarButton, GUILayout.Width(45)))
                {
                    TogglePlayback(duration);
                }
                if (GUILayout.Button("停止", EditorStyles.toolbarButton, GUILayout.Width(45)))
                {
                    _playing = false;
                    _tick = 0;
                    Repaint();
                }
                EditorGUI.BeginChangeCheck();
                float tick = EditorGUILayout.Slider(_tick, 0, duration, GUILayout.MinWidth(100));
                if (EditorGUI.EndChangeCheck())
                {
                    _tick = Mathf.Round(tick);
                    _playing = false;
                    Repaint();
                }
                tickRate = Mathf.Max(1, EditorGUILayout.IntField(tickRate, GUILayout.Width(45)));
                GUILayout.Label("Tick/s", GUILayout.Width(40));
                loopPreview = GUILayout.Toggle(loopPreview, "循环", EditorStyles.toolbarButton, GUILayout.Width(45));
            }
        }

        private void TogglePlayback(long duration)
        {
            if (_tick >= duration) _tick = 0;
            _playing = !_playing;
            _lastTime = EditorApplication.timeSinceStartup;
            Repaint();
        }

        private void DrawTrackSplitter()
        {
            Rect rect = GUILayoutUtility.GetRect(0, 6, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f));
            EditorGUI.DrawRect(new Rect(rect.center.x - 18, rect.y + 2, 36, 2), new Color(0.4f, 0.4f, 0.4f));
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeVertical);
            int control = GUIUtility.GetControlID(FocusType.Passive);
            Event current = Event.current;
            if (current.type == EventType.MouseDown && current.button == 0 && rect.Contains(current.mousePosition))
            {
                GUIUtility.hotControl = control;
                _resizeMouseY = GUIUtility.GUIToScreenPoint(current.mousePosition).y;
                _resizeHeight = trackHeight;
                current.Use();
            }
            else if (GUIUtility.hotControl == control && current.type == EventType.MouseDrag)
            {
                float delta = GUIUtility.GUIToScreenPoint(current.mousePosition).y - _resizeMouseY;
                trackHeight = Mathf.Clamp(_resizeHeight - delta, 140, Mathf.Max(140, position.height - 220));
                current.Use();
                Repaint();
            }
            else if (GUIUtility.hotControl == control && current.rawType == EventType.MouseUp)
            {
                GUIUtility.hotControl = 0;
                current.Use();
            }
        }

        private void ClearTimeline()
        {
            if (!_timeline) return;
            foreach (TrackAsset track in _timeline.GetOutputTracks().ToArray())
            {
                foreach (TimelineClip clip in track.GetClips())
                    if (clip.asset) DestroyImmediate(clip.asset);
                DestroyImmediate(track);
            }
            DestroyImmediate(_timeline);
            _timelineClips = Array.Empty<TimelineClip>();
        }

        private void SyncTimeline(SerializedProperty skill)
        {
            SerializedProperty effects = skill.FindPropertyRelative("effects");
            if (!_timeline || _timelineClips.Length != effects.arraySize)
            {
                ClearTimeline();
                _timeline = CreateInstance<TimelineAsset>();
                _timeline.hideFlags = HideFlags.HideAndDontSave;
                _timelineClips = new TimelineClip[effects.arraySize];
                for (int i = 0; i < effects.arraySize; i++)
                {
                    var track = _timeline.CreateTrack<SkillEffectTimelineTrack>();
                    track.hideFlags = HideFlags.HideAndDontSave;
                    _timelineClips[i] = track.CreateClip<SkillEffectTimelineClip>();
                    _timelineClips[i].asset.hideFlags = HideFlags.HideAndDontSave;
                }
            }
            _timeline.editorSettings.frameRate = tickRate;
            for (int i = 0; i < effects.arraySize; i++)
            {
                SerializedProperty effect = effects.GetArrayElementAtIndex(i);
                if (effect.managedReferenceValue == null) continue;
                TimelineClip clip = _timelineClips[i];
                clip.displayName = effect.managedReferenceValue.GetType().Name;
                clip.GetParentTrack().name = clip.displayName;
                clip.start = (double)effect.FindPropertyRelative("startTick").longValue / tickRate;
                clip.duration = Math.Max(0.000001, (double)effect.FindPropertyRelative("durationTicks").longValue / tickRate);
            }
        }

        private void AddEffect(SkillTable target, int skillIndex, Type type)
        {
            if (!target || skillIndex >= target.skills.Length) return;
            using (var serialized = new SerializedObject(target))
            {
                SerializedProperty skill = serialized.FindProperty("skills").GetArrayElementAtIndex(skillIndex);
                SerializedProperty effects = skill.FindPropertyRelative("effects");
                int index = effects.arraySize++;
                SkillEffect value = (SkillEffect)Activator.CreateInstance(type);
                uint maxId = 0;
                for (int i = 0; i < index; i++)
                    maxId = Math.Max(maxId, (uint)effects.GetArrayElementAtIndex(i).FindPropertyRelative("effectId").longValue);
                value.effectId = maxId + 1;
                value.durationTicks = (uint)skill.FindPropertyRelative("durationTicks").longValue;
                effects.GetArrayElementAtIndex(index).managedReferenceValue = value;
                effects.GetArrayElementAtIndex(index).isExpanded = true;
                serialized.ApplyModifiedProperties();
            }
            Repaint();
        }

        private void RebuildModel()
        {
            ClearAnimation();
            if (_model) DestroyImmediate(_model);
            if (!modelPrefab) return;
            GameObject holder = new GameObject("Skill Preview");
            holder.SetActive(false);
            _preview.AddSingleGO(holder);
            _model = holder;
            GameObject instance = Instantiate(modelPrefab, holder.transform);
            _animator = instance.GetComponentInChildren<Animator>(true);
            foreach (MonoBehaviour behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true)) DestroyImmediate(behaviour);
            foreach (Animator animator in instance.GetComponentsInChildren<Animator>(true))
            {
                animator.runtimeAnimatorController = null;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            // 手动采样骨骼时必须更新蒙皮包围盒，不能沿用导入时的静态范围。
            foreach (SkinnedMeshRenderer renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                renderer.updateWhenOffscreen = true;
            foreach (Camera camera in instance.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
            _preview.camera.useOcclusionCulling = false;
            _bones = instance.GetComponentsInChildren<Transform>(true);
            _animationRootIndices = instance.GetComponentsInChildren<Animator>(true)
                .Select(animator => Array.IndexOf(_bones, animator.transform)).Append(0).Distinct().ToArray();
            _positions = _bones.Select(bone => bone.localPosition).ToArray();
            _rotations = _bones.Select(bone => bone.localRotation).ToArray();
            _scales = _bones.Select(bone => bone.localScale).ToArray();
            holder.SetActive(true);
            FrameModel();
        }

        private void DrawPreview(SkillConfig skill)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                modelPrefab = (GameObject)EditorGUILayout.ObjectField("预览模型", modelPrefab, typeof(GameObject), false);
                if (EditorGUI.EndChangeCheck()) RebuildModel();
                if (GUILayout.Button("居中", GUILayout.Width(45))) FrameModel();
            }
            Rect rect = GUILayoutUtility.GetRect(100, 100, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            OnPreviewInput(rect);
            if (Event.current.type != EventType.Repaint || !_model || rect.width <= 0 || rect.height <= 0) return;
            for (int i = 0; i < _bones.Length; i++)
            { _bones[i].localPosition = _positions[i]; _bones[i].localRotation = _rotations[i]; _bones[i].localScale = _scales[i]; }
            float distance = 0;
            foreach (SkillEffect effect in skill.effects ?? Array.Empty<SkillEffect>())
            {
                if (effect == null) continue;
                float elapsed = Mathf.Clamp(_tick - effect.startTick, 0, effect.durationTicks) / tickRate;
                if (effect is MoveSkillEffect move) distance += elapsed * move.speed;
                if (effect is AnimationSkillEffect animation && animation.clip && _tick >= effect.startTick)
                    SamplePose(animation.clip, animation.sampleOffset + elapsed);
            }
            // Playables 经 Animator 提取根运动；根节点保持初始变换，只有移动效果驱动容器。
            foreach (int index in _animationRootIndices)
            {
                _bones[index].localPosition = _positions[index];
                _bones[index].localRotation = _rotations[index];
            }
            _model.transform.position = Vector3.forward * distance;
            _preview.BeginPreview(rect, GUIStyle.none);
            Quaternion rotation = Quaternion.Euler(_viewAngles.x, _viewAngles.y, 0);
            _preview.camera.transform.SetPositionAndRotation(_viewPivot - rotation * Vector3.forward * _viewDistance, rotation);
            _preview.camera.nearClipPlane = 0.01f;
            _preview.camera.farClipPlane = 1000;
            _preview.Render(true);
            GUI.DrawTexture(rect, _preview.EndPreview(), ScaleMode.StretchToFill, false);
        }

        private void ClearAnimation()
        {
            if (_graph.IsValid()) _graph.Destroy();
            _previewClip = null;
        }

        private void SamplePose(AnimationClip clip, float time)
        {
            if (!_animator) return;
            if (_previewClip != clip)
            {
                ClearAnimation();
                _graph = PlayableGraph.Create("Skill Pose Preview");
                _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                _clipPlayable = AnimationClipPlayable.Create(_graph, clip);
                _clipPlayable.SetApplyFootIK(false);
                _clipPlayable.SetApplyPlayableIK(false);
                AnimationPlayableOutput output = AnimationPlayableOutput.Create(_graph, "Pose", _animator);
                output.SetSourcePlayable(_clipPlayable);
                _graph.Play();
                _previewClip = clip;
            }
            _clipPlayable.SetTime(time);
            _graph.Evaluate(0);
        }

        private void FrameModel()
        {
            Bounds bounds = new Bounds(_model ? _model.transform.position + Vector3.up : Vector3.up, Vector3.one);
            if (_model)
            {
                Renderer[] renderers = _model.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    bounds = renderers[0].bounds;
                    foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
                }
            }
            _viewPivot = bounds.center;
            _viewAngles = new Vector2(15, -34);
            _viewDistance = Mathf.Max(1, bounds.extents.magnitude * 1.3f / Mathf.Sin(17.5f * Mathf.Deg2Rad));
            Repaint();
        }

        private void CreateEnvironment()
        {
            var sky = new Material(Shader.Find("Skybox/Procedural")) { hideFlags = HideFlags.HideAndDontSave };
            _previewMaterials.Add(sky);
            _preview.camera.gameObject.AddComponent<Skybox>().material = sky;
            _preview.camera.clearFlags = CameraClearFlags.Skybox;
            var grid = new GameObject("Preview Coordinates");
            _preview.AddSingleGO(grid);
            for (int i = -10; i <= 10; i++)
            {
                if (i == 0) continue;
                CreateLine(grid.transform, new Vector3(i, 0, -10), new Vector3(i, 0, 10), Color.gray, 0.008f);
                CreateLine(grid.transform, new Vector3(-10, 0, i), new Vector3(10, 0, i), Color.gray, 0.008f);
            }
            CreateLine(grid.transform, Vector3.left * 10, Vector3.right * 10, Color.red, 0.02f);
            CreateLine(grid.transform, Vector3.zero, Vector3.up * 3, Color.green, 0.02f);
            CreateLine(grid.transform, Vector3.back * 10, Vector3.forward * 10, Color.blue, 0.02f);
        }

        private void CreateLine(Transform parent, Vector3 start, Vector3 end, Color color, float width)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { hideFlags = HideFlags.HideAndDontSave };
            material.SetColor("_BaseColor", color);
            _previewMaterials.Add(material);
            var lineObject = new GameObject("Grid Line");
            lineObject.transform.SetParent(parent, false);
            var line = lineObject.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.positionCount = 2;
            line.SetPositions(new[] { start, end });
            line.startWidth = line.endWidth = width;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
        }

        private void OnPreviewInput(Rect rect)
        {
            int control = GUIUtility.GetControlID(FocusType.Passive);
            Event current = Event.current;
            EventType eventType = current.GetTypeForControl(control);
            if (eventType == EventType.MouseDown && rect.Contains(current.mousePosition) &&
                (current.button == 1 || current.button == 2 || (current.button == 0 && current.alt)))
            {
                GUIUtility.hotControl = control;
                _viewButton = current.button;
                current.Use();
            }
            else if (eventType == EventType.MouseDrag && GUIUtility.hotControl == control)
            {
                Quaternion rotation = Quaternion.Euler(_viewAngles.x, _viewAngles.y, 0);
                if (_viewButton == 2)
                {
                    float scale = 2 * _viewDistance * Mathf.Tan(_preview.camera.fieldOfView * Mathf.Deg2Rad * 0.5f) / Mathf.Max(1, rect.height);
                    _viewPivot += rotation * new Vector3(-current.delta.x, current.delta.y, 0) * scale;
                }
                else if (_viewButton == 1 && current.alt)
                    _viewDistance = Mathf.Clamp(_viewDistance * Mathf.Exp(current.delta.x * 0.01f), 0.05f, 500);
                else
                {
                    Vector3 position = _viewPivot - rotation * Vector3.forward * _viewDistance;
                    _viewAngles += new Vector2(current.delta.y, current.delta.x) * 0.4f;
                    _viewAngles.x = Mathf.Clamp(_viewAngles.x, -89, 89);
                    if (_viewButton == 1)
                        _viewPivot = position + Quaternion.Euler(_viewAngles.x, _viewAngles.y, 0) * Vector3.forward * _viewDistance;
                }
                current.Use();
                Repaint();
            }
            else if (eventType == EventType.MouseUp && GUIUtility.hotControl == control)
            {
                GUIUtility.hotControl = 0;
                current.Use();
            }
            else if (eventType == EventType.ScrollWheel && rect.Contains(current.mousePosition))
            {
                _viewDistance = Mathf.Clamp(_viewDistance * Mathf.Exp(current.delta.y * 0.08f), 0.05f, 500);
                current.Use();
                Repaint();
            }
        }
    }
}
