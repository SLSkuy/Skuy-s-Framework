using System;
using System.Linq;
using GamePlay;
using GamePlay.BattleSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace Editor
{
    /// <summary>在独立预览场景中按时间采样连招转换的动作混合。</summary>
    internal sealed class CombatTransitionPreview : IDisposable
    {
        private readonly EditorWindow _window;
        private readonly CombatTransitionTimeline _timeline;
        private readonly int _tickRate;
        private BattlePreviewViewport _viewport;
        private GameObject _prefab;
        private Animator _animator;
        private AnimationClip _sourceClip;
        private AnimationClip _targetClip;
        private PlayableGraph _graph;
        private AnimationMixerPlayable _mixer;
        private AnimationClipPlayable _sourcePlayable;
        private AnimationClipPlayable _targetPlayable;
        private Vector3 _rootPosition;
        private Quaternion _rootRotation;
        private Bounds _modelBounds;
        private float? _switchTick;
        private float? _blendDuration;
        private float _time;
        private float _duration;
        private double _lastUpdate;
        private bool _playing;
        private bool _loop;

        public CombatTransitionPreview(EditorWindow window, int tickRate)
        {
            _window = window;
            _tickRate = Mathf.Max(1, tickRate);
            _timeline = new CombatTransitionTimeline(_tickRate);
        }

        public void Reset()
        {
            _switchTick = null;
            _blendDuration = null;
            _time = 0f;
            _playing = false;
            _timeline.Fit();
            StopSampling();
        }

        public void Tick()
        {
            if (!_playing) return;
            double now = EditorApplication.timeSinceStartup;
            _time += (float)(now - _lastUpdate);
            _lastUpdate = now;
            if (_time >= _duration)
            {
                if (_loop)
                    _time %= _duration;
                else
                {
                    _time = _duration;
                    _playing = false;
                }
            }
            _window.Repaint();
        }

        public void Draw(ref GameObject model, SkillConfig source, SkillConfig target, bool hasSource,
            SkillInterruptReason reason, float paneHeight)
        {
            GUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("转换动画预览", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                _loop = GUILayout.Toggle(_loop, "循环播放", EditorStyles.miniButton, GUILayout.Width(64));
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                model = (GameObject)EditorGUILayout.ObjectField("预览模型", model, typeof(GameObject), false);
                if (_prefab != model) BuildModel(model);
                using (new EditorGUI.DisabledScope(!_animator))
                {
                    if (GUILayout.Button(new GUIContent("居中", "在预览画面内滚动鼠标滚轮可缩放模型。"),
                            EditorStyles.miniButton, GUILayout.Width(42)))
                        _viewport.Frame(_modelBounds);
                }
            }

            var targetEffect = target?.effects?.OfType<AnimationSkillEffect>()
                .Where(IsPreviewable).OrderBy(effect => effect.startTick).FirstOrDefault();
            if (targetEffect == null || hasSource && source == null)
            {
                DrawUnavailable("请检查源技能，并为目标技能配置有效的动画效果。");
                return;
            }

            AnimationSkillEffect sourceEffect = null;
            AnimationSkillEffect sourceTimingEffect = null;
            float longestBlendOut = GlobalConst.DEFAULT_BLEND_OUT;
            float targetDelay = (float)targetEffect.startTick / _tickRate;
            var startRange = new Vector2(targetDelay, targetDelay);
            float switchTick = 0f;
            if (hasSource)
            {
                var sourceEffects = source.effects?.OfType<AnimationSkillEffect>().ToArray() ?? Array.Empty<AnimationSkillEffect>();
                foreach (var effect in sourceEffects)
                    longestBlendOut = Mathf.Max(longestBlendOut, effect.blendOut);
                var playableEffects = sourceEffects.Where(IsPreviewable).ToArray();
                float minTick = 0f;
                float maxTick = source.durationTicks;
                if (playableEffects.Length > 0)
                {
                    minTick = playableEffects.Min(effect => (float)effect.startTick);
                    maxTick = playableEffects.Max(effect => (float)effect.startTick + effect.durationTicks);
                }
                var interrupt = source.interruptRules?.Where(rule => rule != null && rule.reason == reason)
                    .OrderBy(rule => rule.startTick).FirstOrDefault();
                switchTick = Mathf.Clamp(Mathf.Round(_switchTick ?? interrupt?.startTick ?? (minTick + maxTick) * 0.5f),
                    minTick, maxTick);
                startRange = new Vector2(minTick / _tickRate + targetDelay, maxTick / _tickRate + targetDelay);
                // 在相邻动画有空档时仍可连续拖动，预览保持此前最后一个动作。
                sourceEffect = FindAnimationAtTick(playableEffects, switchTick);
                sourceTimingEffect = sourceEffect ?? FindAnimationAtTick(sourceEffects, switchTick);
            }

            float transitionStart = switchTick / _tickRate + targetDelay;
            float targetDuration = (float)targetEffect.durationTicks / _tickRate;
            float blend = hasSource
                ? Mathf.Max(0f, _blendDuration ?? sourceTimingEffect?.blendOut ?? GlobalConst.DEFAULT_BLEND_OUT)
                : 0f;
            float maxBlend = hasSource ? Mathf.Max(targetDuration, Mathf.Max(longestBlendOut, blend)) : 0f;
            _duration = transitionStart + Mathf.Max(targetDuration, blend);
            _time = Mathf.Clamp(_time, 0f, _duration);

            string timeLabel = hasSource
                ? $"{_time:F2}/{_duration:F2}s   切换 Tick {switchTick:F0}   混合 {blend:F2}s"
                : $"{_time:F2} / {_duration:F2} 秒";
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!_animator))
                {
                    if (GUILayout.Button(new GUIContent(_playing ? "暂停" : "播放", "点击时间轴后，可用空格切换播放和暂停。"),
                            EditorStyles.miniButton, GUILayout.Width(44)))
                        TogglePlayback();
                }
                GUILayout.Label(timeLabel, EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("适配时间轴", "滚轮缩放，Shift + 滚轮或鼠标中键拖动平移。"),
                        EditorStyles.miniButton))
                    _timeline.Fit();
            }
            Rect timeline = GUILayoutUtility.GetRect(1, CombatTransitionTimeline.HEIGHT, GUILayout.ExpandWidth(true));
            float editedStart = transitionStart;
            float editedBlend = blend;
            if (_timeline.Draw(timeline, ref _time, _duration, startRange, maxBlend, ref editedStart, ref editedBlend,
                    sourceEffect, targetEffect, hasSource, TogglePlayback))
            {
                _playing = false;
                if (editedStart != transitionStart)
                    _switchTick = Mathf.Round((editedStart - targetDelay) * _tickRate);
                if (editedBlend != blend)
                    _blendDuration = editedBlend;
                _window.Repaint();
            }

            if (!_animator)
            {
                DrawUnavailable(model ? "请选择带 Animator 的预览模型。" : "请选择预览模型。");
                return;
            }

            float viewHeight = Mathf.Max(105f, paneHeight - CombatTransitionTimeline.HEIGHT - 73f);
            Rect rect = GUILayoutUtility.GetRect(1, viewHeight, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
                Sample(sourceEffect, targetEffect, transitionStart, targetDuration, blend, hasSource);
            _viewport.Draw(rect, _window);
        }

        public void Dispose()
        {
            _playing = false;
            StopSampling();
            _viewport?.Dispose();
            _viewport = null;
        }

        private void TogglePlayback()
        {
            if (!_animator) return;
            _playing = !_playing;
            if (_playing && _time >= _duration) _time = 0f;
            _lastUpdate = EditorApplication.timeSinceStartup;
            _window.Repaint();
        }

        private static bool IsPreviewable(AnimationSkillEffect effect)
        {
            return effect.clip && !effect.clip.legacy && effect.durationTicks > 0;
        }

        private static AnimationSkillEffect FindAnimationAtTick(AnimationSkillEffect[] effects, float tick)
        {
            return effects.Where(effect => effect.startTick <= tick)
                .OrderBy(effect => tick <= (float)effect.startTick + effect.durationTicks)
                .ThenBy(effect => effect.startTick).LastOrDefault()
                ?? effects.OrderBy(effect => effect.startTick).FirstOrDefault();
        }

        private void DrawUnavailable(string message)
        {
            _playing = false;
            StopSampling();
            EditorGUILayout.HelpBox(message, MessageType.Info);
        }

        private void BuildModel(GameObject model)
        {
            Dispose();
            _prefab = model;
            _animator = null;
            if (!model || !model.GetComponentInChildren<Animator>(true)) return;

            _viewport = new BattlePreviewViewport(showAxes: false);
            var holder = new GameObject("Combat Transition Preview");
            holder.SetActive(false);
            _viewport.AddSingleGO(holder);
            var instance = Object.Instantiate(model, holder.transform);
            foreach (var behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
                Object.DestroyImmediate(behaviour);
            foreach (var camera in instance.GetComponentsInChildren<Camera>(true))
                camera.enabled = false;
            foreach (var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                renderer.updateWhenOffscreen = true;
            foreach (var animator in instance.GetComponentsInChildren<Animator>(true))
            {
                animator.runtimeAnimatorController = null;
                animator.applyRootMotion = false;
                animator.fireEvents = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            _animator = instance.GetComponentInChildren<Animator>(true);
            _rootPosition = _animator.transform.localPosition;
            _rootRotation = _animator.transform.localRotation;
            holder.SetActive(true);

            var bounds = new Bounds(Vector3.up, Vector3.one);
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                bounds = renderers[0].bounds;
                foreach (var renderer in renderers)
                    bounds.Encapsulate(renderer.bounds);
            }
            _modelBounds = bounds;
            _viewport.Frame(_modelBounds);
        }

        private void StopSampling()
        {
            if (_graph.IsValid()) _graph.Destroy();
            _sourceClip = null;
            _targetClip = null;
        }

        private void Sample(AnimationSkillEffect source, AnimationSkillEffect target, float transitionStart,
            float targetDuration, float blend, bool hasSource)
        {
            if (_sourceClip != source?.clip || _targetClip != target.clip)
            {
                StopSampling();
                _sourceClip = source?.clip;
                _targetClip = target.clip;
                _graph = PlayableGraph.Create("Combat Transition Preview");
                _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                _mixer = AnimationMixerPlayable.Create(_graph, 2);
                if (_sourceClip) _sourcePlayable = ConnectClip(_sourceClip, 0);
                _targetPlayable = ConnectClip(_targetClip, 1);
                var output = AnimationPlayableOutput.Create(_graph, "Transition", _animator);
                output.SetSourcePlayable(_mixer);
                _graph.Play();
            }

            float targetTime = _time - transitionStart;
            // 与 AnimationModule 的动作交叉混合一致。
            float weight = !hasSource ? 1f : targetTime < 0f ? 0f :
                blend > 0f ? Mathf.SmoothStep(0f, 1f, targetTime / blend) : 1f;
            if (source != null)
                _sourcePlayable.SetTime(source.sampleOffset + Mathf.Max(0f, _time - (float)source.startTick / _tickRate));
            _targetPlayable.SetTime(target.sampleOffset + Mathf.Clamp(targetTime, 0f, targetDuration));
            _mixer.SetInputWeight(0, 1f - weight);
            _mixer.SetInputWeight(1, weight);
            _animator.transform.SetLocalPositionAndRotation(_rootPosition, _rootRotation);
            _graph.Evaluate(0f);
            _animator.transform.SetLocalPositionAndRotation(_rootPosition, _rootRotation);
        }

        private AnimationClipPlayable ConnectClip(AnimationClip clip, int port)
        {
            var playable = AnimationClipPlayable.Create(_graph, clip);
            playable.SetApplyFootIK(false);
            playable.SetApplyPlayableIK(false);
            _mixer.ConnectInput(port, playable, 0);
            return playable;
        }
    }
}
