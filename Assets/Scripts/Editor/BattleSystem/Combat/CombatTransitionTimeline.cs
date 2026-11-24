using System;
using GamePlay.BattleSystem;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    /// <summary>显示并调整源动画与目标动画之间的预览过渡。</summary>
    internal sealed class CombatTransitionTimeline
    {
        public const float HEIGHT = 92f;
        private const float LABEL_WIDTH = 64f;
        private const float HEADER_HEIGHT = 22f;
        private const float ROW_HEIGHT = 26f;
        private const float HANDLE_WIDTH = 12f;
        private static readonly Color SourceColor = new(.23f, .45f, .68f);
        private static readonly Color TargetColor = new(.22f, .55f, .43f);
        private static readonly Color BlendColor = new(.92f, .66f, .27f);
        private static readonly Color EventColor = new(.77f, .59f, .92f);

        private readonly int _tickRate;
        private float _zoom = 1f;
        private float _start;
        private float _visibleWidth;
        private float _dragOffset;
        private DragMode _drag;
        private bool _spaceHeld;

        private enum DragMode
        {
            None,
            Seek,
            Pan,
            TransitionStart,
            BlendEnd
        }

        public CombatTransitionTimeline(int tickRate)
        {
            _tickRate = tickRate;
        }

        public void Fit()
        {
            _zoom = 1f;
            _start = 0f;
            _drag = DragMode.None;
        }

        public bool Draw(Rect rect, ref float time, float duration, Vector2 startRange, float maxBlend, ref float transitionStart,
            ref float blend, AnimationSkillEffect source, AnimationSkillEffect target, bool hasSource, Action togglePlayback)
        {
            if (rect.width <= LABEL_WIDTH) return false;
            var plot = new Rect(rect.x + LABEL_WIDTH, rect.y, rect.width - LABEL_WIDTH, HEADER_HEIGHT + 2 * ROW_HEIGHT);
            _visibleWidth = plot.width;
            float targetDuration = (float)target.durationTicks / _tickRate;
            float total = startRange.y + Mathf.Max(targetDuration, maxBlend);
            float span = total / _zoom;
            _start = Mathf.Clamp(_start, 0f, total - span);
            float scale = plot.width / span;
            Rect startHandle = HandleRect(plot, transitionStart, scale, 1);
            Rect blendHandle = HandleRect(plot, transitionStart + blend, scale, 0);
            if (hasSource)
            {
                if (startHandle.width > 0f) EditorGUIUtility.AddCursorRect(startHandle, MouseCursor.ResizeHorizontal);
                if (blendHandle.width > 0f) EditorGUIUtility.AddCursorRect(blendHandle, MouseCursor.ResizeHorizontal);
            }
            bool changed = ProcessInput(plot, startHandle, blendHandle, hasSource, duration, total,
                startRange, maxBlend, ref span, ref time, ref transitionStart, ref blend, togglePlayback);
            if (Event.current.type == EventType.Repaint)
                DrawTracks(rect, plot, span, time, transitionStart, blend, source, target, hasSource);

            var bar = new Rect(plot.x, plot.yMax + 3f, plot.width, 13f);
            using (new EditorGUI.DisabledScope(_zoom <= 1f))
                _start = GUI.HorizontalScrollbar(bar, _start, span, 0f, total);
            return changed;
        }

        private bool ProcessInput(Rect plot, Rect startHandle, Rect blendHandle, bool editable, float duration,
            float total, Vector2 startRange, float maxBlend, ref float span, ref float time, ref float transitionStart,
            ref float blend, Action togglePlayback)
        {
            var current = Event.current;
            int control = GUIUtility.GetControlID(FocusType.Keyboard, plot);
            bool focused = GUIUtility.keyboardControl == control;
            if (!focused) _spaceHeld = false;
            float relative = Mathf.Clamp01((current.mousePosition.x - plot.x) / plot.width);
            switch (current.GetTypeForControl(control))
            {
                case EventType.KeyDown when focused && current.keyCode == KeyCode.Space:
                    if (!_spaceHeld) togglePlayback();
                    _spaceHeld = true;
                    current.Use();
                    break;
                case EventType.KeyUp when focused && current.keyCode == KeyCode.Space:
                    _spaceHeld = false;
                    current.Use();
                    break;
                case EventType.ScrollWheel when plot.Contains(current.mousePosition):
                    if (current.shift)
                        _start += current.delta.y * span * 0.05f;
                    else
                    {
                        float anchor = _start + relative * span;
                        _zoom = Mathf.Clamp(_zoom * Mathf.Exp(-current.delta.y * 0.12f), 1f, 32f);
                        span = total / _zoom;
                        _start = anchor - relative * span;
                    }
                    _start = Mathf.Clamp(_start, 0f, total - span);
                    GUI.changed = true;
                    current.Use();
                    break;
                case EventType.MouseDown when plot.Contains(current.mousePosition) && (current.button == 0 || current.button == 2):
                    GUIUtility.hotControl = control;
                    GUIUtility.keyboardControl = control;
                    _spaceHeld = false;
                    float mouseTime = _start + span * relative;
                    if (current.button == 2 || current.alt)
                        _drag = DragMode.Pan;
                    else if (editable && startHandle.Contains(current.mousePosition))
                    {
                        _drag = DragMode.TransitionStart;
                        _dragOffset = mouseTime - transitionStart;
                    }
                    else if (editable && blendHandle.Contains(current.mousePosition))
                    {
                        _drag = DragMode.BlendEnd;
                        _dragOffset = mouseTime - transitionStart - blend;
                    }
                    else
                    {
                        _drag = DragMode.Seek;
                        time = Mathf.Min(duration, mouseTime);
                    }
                    current.Use();
                    return _drag != DragMode.Pan;
                case EventType.MouseDrag when GUIUtility.hotControl == control:
                    current.Use();
                    float dragTime = _start + span * relative - _dragOffset;
                    switch (_drag)
                    {
                        case DragMode.Pan:
                            _start = Mathf.Clamp(_start - current.delta.x / plot.width * span, 0f, total - span);
                            GUI.changed = true;
                            return false;
                        case DragMode.TransitionStart:
                            transitionStart = Mathf.Clamp(Mathf.Round(dragTime * _tickRate) / _tickRate,
                                startRange.x, startRange.y);
                            time = transitionStart;
                            return true;
                        case DragMode.BlendEnd:
                            blend = Mathf.Clamp(dragTime - transitionStart, 0f, maxBlend);
                            time = transitionStart + blend * 0.5f;
                            return true;
                        case DragMode.Seek:
                            time = Mathf.Min(duration, _start + span * relative);
                            return true;
                    }
                    break;
                case EventType.MouseUp when GUIUtility.hotControl == control:
                    GUIUtility.hotControl = 0;
                    _drag = DragMode.None;
                    current.Use();
                    break;
            }
            return false;
        }

        private void DrawTracks(Rect rect, Rect plot, float span, float time, float transitionStart, float blend,
            AnimationSkillEffect source, AnimationSkillEffect target, bool hasSource)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, plot.height), new Color(.14f, .14f, .14f));
            DrawLabel(rect, 0, "源动画");
            DrawLabel(rect, 1, "目标动画");

            GUI.BeginGroup(plot);
            var area = new Rect(0f, 0f, plot.width, plot.height);
            float scale = area.width / span;
            float StepX(float seconds) => (seconds - _start) * scale;
            float minimumStep = 58f / scale;
            float stepUnit = Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(minimumStep)));
            float ratio = minimumStep / stepUnit;
            float step = stepUnit * (ratio <= 2f ? 2f : ratio <= 5f ? 5f : 10f);
            int decimals = Mathf.Clamp(Mathf.CeilToInt(-Mathf.Log10(step)), 0, 4);
            for (float tick = Mathf.Ceil(_start / step) * step; tick <= _start + span; tick += step)
            {
                float x = StepX(tick);
                EditorGUI.DrawRect(new Rect(x, 0f, 1f, area.height), new Color(1f, 1f, 1f, .14f));
                GUI.Label(new Rect(x + 3f, 0f, 56f, HEADER_HEIGHT), tick.ToString("F" + decimals) + "s", EditorStyles.miniLabel);
            }
            for (int i = 0; i < 2; i++)
                EditorGUI.DrawRect(new Rect(0f, RowY(i), area.width, 1f), new Color(1f, 1f, 1f, .1f));

            if (source != null)
                DrawClip(0, source, (float)source.startTick / _tickRate, transitionStart + blend, SourceColor, scale);
            float targetEnd = transitionStart + target.durationTicks / (float)_tickRate;
            DrawClip(1, target, transitionStart, targetEnd, TargetColor, scale);

            if (hasSource)
            {
                DrawSpan(RowY(0), 2 * ROW_HEIGHT, transitionStart, transitionStart + blend, scale,
                    new Color(BlendColor.r, BlendColor.g, BlendColor.b, .22f), "混合区间", "");
                DrawLine(transitionStart, scale, area.height, BlendColor);
            }
            DrawLine(time, scale, area.height, Color.white);
            if (hasSource)
            {
                DrawHandle(area, transitionStart, scale, 1, "拖动目标动画起点调整切换 Tick");
                DrawHandle(area, transitionStart + blend, scale, 0, "拖动过渡终点调整混合时长");
            }
            GUI.EndGroup();
        }

        private void DrawClip(int row, AnimationSkillEffect effect, float start, float end, Color color, float scale)
        {
            DrawSpan(RowY(row) + 3f, ROW_HEIGHT - 5f, start, end, scale, color, effect.clip.name, effect.clip.name);
            foreach (var animationEvent in effect.clip.events)
            {
                float marker = start + animationEvent.time - effect.sampleOffset;
                if (effect.clip.isLooping && effect.clip.length > 0f)
                {
                    float firstVisible = Mathf.Max(start, _start - 5f / scale);
                    marker += Mathf.Ceil((firstVisible - marker) / effect.clip.length) * effect.clip.length;
                    float period = Mathf.Max(1f, Mathf.Ceil(5f / (scale * effect.clip.length))) * effect.clip.length;
                    for (; marker <= Mathf.Min(end, _start + _visibleWidth / scale); marker += period)
                        DrawMarker(row, marker, scale, animationEvent.functionName);
                }
                else if (marker >= start && marker <= end)
                    DrawMarker(row, marker, scale, animationEvent.functionName);
            }
        }

        private Rect HandleRect(Rect plot, float seconds, float scale, int row)
        {
            float x = plot.x + (seconds - _start) * scale;
            return Rect.MinMaxRect(Mathf.Max(plot.x, x - HANDLE_WIDTH * 0.5f), plot.y + RowY(row) + 2f,
                Mathf.Min(plot.xMax, x + HANDLE_WIDTH * 0.5f), plot.y + RowY(row + 1) - 2f);
        }

        private void DrawHandle(Rect plot, float seconds, float scale, int row, string tooltip)
        {
            Rect handle = HandleRect(plot, seconds, scale, row);
            if (handle.width <= 0f) return;
            EditorGUI.DrawRect(handle, BlendColor);
            EditorGUI.DrawRect(new Rect(handle.center.x, handle.y + 5f, 1f, handle.height - 10f), new Color(.23f, .15f, .06f));
            GUI.Label(handle, new GUIContent("", tooltip));
        }

        private void DrawMarker(int row, float seconds, float scale, string tooltip)
        {
            float x = (seconds - _start) * scale;
            float y = RowY(row) + 3f;
            EditorGUI.DrawRect(new Rect(x - 2f, y, 5f, 6f), EventColor);
            EditorGUI.DrawRect(new Rect(x, y + 6f, 1f, ROW_HEIGHT - 10f), EventColor);
            GUI.Label(new Rect(x - 4f, y, 9f, ROW_HEIGHT - 4f), new GUIContent("", tooltip));
        }

        private void DrawSpan(float y, float height, float start, float end, float scale, Color color, string tooltip, string label)
        {
            float x = Mathf.Clamp((start - _start) * scale, 0f, _visibleWidth);
            float right = Mathf.Clamp((end - _start) * scale, 0f, _visibleWidth);
            if (right <= x) return;
            var bar = new Rect(x, y, right - x, height);
            EditorGUI.DrawRect(bar, color);
            GUI.Label(bar, new GUIContent(bar.width > 28f ? label : "", tooltip), EditorStyles.miniLabel);
        }

        private void DrawLine(float seconds, float scale, float height, Color color)
        {
            float x = (seconds - _start) * scale;
            EditorGUI.DrawRect(new Rect(x, 0f, 1f, height), color);
        }

        private static float RowY(int row) => HEADER_HEIGHT + row * ROW_HEIGHT;

        private static void DrawLabel(Rect rect, int row, string label)
        {
            GUI.Label(new Rect(rect.x + 4f, rect.y + RowY(row), LABEL_WIDTH - 4f, ROW_HEIGHT), label, EditorStyles.miniLabel);
        }
    }
}
