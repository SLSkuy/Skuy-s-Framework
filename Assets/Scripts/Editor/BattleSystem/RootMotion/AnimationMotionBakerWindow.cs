using System;
using GamePlay.BattleSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Editor
{
    /// <summary>在独立预览场景中烘焙并查看单个动画的根运动。</summary>
    public class AnimationMotionBakerWindow : EditorWindow
    {
        [SerializeField] private GameObject modelPrefab;
        [SerializeField] private AnimationClip clip;
        [SerializeField] private int sampleRate = 60;
        [SerializeField] private bool loopPreview;

        private BattlePreviewViewport _viewport;
        private BakedAnimMotionData _preview;
        private GameObject _holder;
        private Transform _modelRoot;
        private Animator _animator;
        private LineRenderer _trajectory;
        private (Transform bone, Vector3 position, Quaternion rotation, Vector3 scale)[] _restPose;
        private PlayableGraph _graph;
        private AnimationClipPlayable _playable;
        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private Bounds _trajectoryBounds;
        private Vector2 _infoScroll;
        private float _time;
        private float _sampledTime = -1f;
        private bool _playing;
        private double _lastTime;

        [MenuItem("Tools/BattleSystem/根运动烘培编辑器")]
        public static void Open()
        {
            GetWindow<AnimationMotionBakerWindow>("根运动烘焙").Show();
        }

        private void BakePreview()
        {
            ClearPreview();
            BakedAnimMotionData data = CreateInstance<BakedAnimMotionData>();
            data.hideFlags = HideFlags.HideAndDontSave;
            _preview = data;
            try
            {
                CreatePreviewModel();
                Animator animator = _animator;
                _graph.Evaluate(0f);

                _startRotation = animator.transform.rotation;
                Quaternion inverseStartRotation = Quaternion.Inverse(_startRotation);
                Quaternion rotation = _startRotation;
                _startPosition = animator.transform.position;
                Vector3 worldPosition = _startPosition;
                Vector3 position = Vector3.zero;
                float yaw = 0f;
                float previousTime = 0f;
                data.sourceClip = clip;
                data.sourceAvatar = animator.avatar;
                data.duration = clip.length;
                data.sampleRate = (uint)sampleRate;
                data.positionX.AddKey(0f, 0f);
                data.positionY.AddKey(0f, 0f);
                data.positionZ.AddKey(0f, 0f);
                data.yaw.AddKey(0f, 0f);
                int steps = Mathf.CeilToInt(clip.length * sampleRate);
                for (int i = 1; i <= steps; i++)
                {
                    float time = Mathf.Min((float)i / sampleRate, clip.length);
                    animator.transform.SetPositionAndRotation(worldPosition, rotation);
                    _graph.Evaluate(time - previousTime);
                    Vector3 delta = animator.deltaPosition;
                    Vector3 localDelta = inverseStartRotation * delta;
                    position += localDelta;
                    data.planarDistance += new Vector2(localDelta.x, localDelta.z).magnitude;
                    Quaternion nextRotation = rotation * animator.deltaRotation;
                    Vector3 previousForward = Vector3.ProjectOnPlane(rotation * Vector3.forward, Vector3.up);
                    Vector3 nextForward = Vector3.ProjectOnPlane(nextRotation * Vector3.forward, Vector3.up);
                    yaw += Vector3.SignedAngle(previousForward, nextForward, Vector3.up);
                    data.positionX.AddKey(time, position.x);
                    data.positionY.AddKey(time, position.y);
                    data.positionZ.AddKey(time, position.z);
                    data.yaw.AddKey(time, yaw);
                    worldPosition += delta;
                    rotation = nextRotation;
                    previousTime = time;
                }
                SetLinearTangents(data.positionX);
                SetLinearTangents(data.positionY);
                SetLinearTangents(data.positionZ);
                SetLinearTangents(data.yaw);
                _playable.SetSpeed(0);
                SetPreviewTime(0f);
                BuildTrajectory();
                FramePreview();
            }
            catch (Exception exception)
            {
                ClearPreview();
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("采样失败", exception.Message, "确定");
            }
        }

        private void CreatePreviewModel()
        {
            // 先停用再实例化，移除临时实例上的游戏脚本后才激活。
            _holder = new GameObject("Motion Bake Preview");
            _holder.SetActive(false);
            _viewport.AddSingleGO(_holder);
            GameObject instance = Instantiate(modelPrefab, _holder.transform);
            _modelRoot = instance.transform;
            _modelRoot.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            foreach (MonoBehaviour behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
                DestroyImmediate(behaviour);

            Animator[] animators = instance.GetComponentsInChildren<Animator>(true);
            if (animators.Length != 1)
                throw new InvalidOperationException("请选择包含且仅包含一个 Animator 的角色模型 Prefab。");
            _animator = animators[0];
            if (clip.isHumanMotion && (!_animator.avatar || !_animator.avatar.isValid || !_animator.avatar.isHuman))
                throw new InvalidOperationException("Humanoid 动画需要模型包含有效的 Humanoid Avatar。");

            for (Transform node = _animator.transform; node != _holder.transform; node = node.parent)
                node.gameObject.SetActive(true);
            _animator.enabled = true;
            _animator.runtimeAnimatorController = null;
            _animator.applyRootMotion = true;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            foreach (SkinnedMeshRenderer renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                renderer.updateWhenOffscreen = true;
            foreach (Camera camera in instance.GetComponentsInChildren<Camera>(true))
                camera.enabled = false;
            _holder.SetActive(true);

            Transform[] bones = instance.GetComponentsInChildren<Transform>(true);
            _restPose = new (Transform, Vector3, Quaternion, Vector3)[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                Transform bone = bones[i];
                _restPose[i] = (bone, bone.localPosition, bone.localRotation, bone.localScale);
            }

            _graph = PlayableGraph.Create("Motion Bake");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            _playable = AnimationClipPlayable.Create(_graph, clip);
            _playable.SetApplyFootIK(false);
            _playable.SetApplyPlayableIK(false);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(_graph, "Motion", _animator);
            output.SetSourcePlayable(_playable);
            _graph.Play();
        }

        private static void SetLinearTangents(AnimationCurve curve)
        {
            for (int i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            }

            curve.preWrapMode = WrapMode.ClampForever;
            curve.postWrapMode = WrapMode.ClampForever;
        }

        private void SetPreviewTime(float time)
        {
            _time = time;
            if (_sampledTime == time)
                return;

            foreach (var (bone, position, rotation, scale) in _restPose)
            {
                bone.SetLocalPositionAndRotation(position, rotation);
                bone.localScale = scale;
            }

            _playable.SetTime(time);
            _graph.Evaluate(0f);

            // 角色根节点由烘焙曲线驱动，随机拖动时间时也与轨迹保持一致。
            if (_modelRoot != _animator.transform)
                _modelRoot.SetLocalPositionAndRotation(_restPose[0].position, _restPose[0].rotation);

            Vector3 positionOnPath = GetTrajectoryPosition(time);
            Quaternion rotationOnPath = Quaternion.AngleAxis(_preview.yaw.Evaluate(time), Vector3.up) * _startRotation;
            _animator.transform.SetPositionAndRotation(positionOnPath, rotationOnPath);
            _sampledTime = time;
        }

        private Vector3 GetTrajectoryPosition(float time)
        {
            return _startPosition + _startRotation * _preview.EvaluatePosition(time);
        }

        private void BuildTrajectory()
        {
            Keyframe[] samples = _preview.positionX.keys;
            var points = new Vector3[samples.Length];
            _trajectoryBounds = new Bounds(_startPosition, Vector3.zero);
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = GetTrajectoryPosition(samples[i].time);
                _trajectoryBounds.Encapsulate(points[i]);
            }

            _trajectory.positionCount = points.Length;
            _trajectory.useWorldSpace = true;
            _trajectory.SetPositions(points);
        }

        private void FramePreview()
        {
            Bounds bounds = new Bounds(_animator.transform.position + Vector3.up, Vector3.one);
            Renderer[] renderers = _modelRoot.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                bounds = renderers[0].bounds;
                foreach (Renderer renderer in renderers)
                    bounds.Encapsulate(renderer.bounds);
            }

            Vector3 currentPosition = GetTrajectoryPosition(_time);
            bounds.SetMinMax(bounds.min + _trajectoryBounds.min - currentPosition,
                bounds.max + _trajectoryBounds.max - currentPosition);
            bounds.Encapsulate(_trajectoryBounds);
            _viewport.Frame(bounds);
            Repaint();
        }

        private void DrawAnimationPanel()
        {
            float width = Mathf.Min(290f, position.width * 0.4f);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(width), GUILayout.ExpandHeight(true)))
            {
                using (var scroll = new EditorGUILayout.ScrollViewScope(_infoScroll, GUILayout.ExpandHeight(true)))
                {
                    _infoScroll = scroll.scrollPosition;
                    DrawInputs();
                    GUILayout.Space(12f);
                    DrawAnimationInfo();
                }

                DrawBakeActions();
            }
        }

        private void DrawInputs()
        {
            EditorGUILayout.LabelField("资源与采样", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 78f;
            modelPrefab = (GameObject)EditorGUILayout.ObjectField("角色模型", modelPrefab, typeof(GameObject), false);
            clip = (AnimationClip)EditorGUILayout.ObjectField("动画片段", clip, typeof(AnimationClip), false);
            sampleRate = EditorGUILayout.IntSlider("采样率 (Hz)", sampleRate, 15, 240);
            EditorGUIUtility.labelWidth = previousLabelWidth;
            if (EditorGUI.EndChangeCheck())
                ClearPreview();
        }

        private void DrawAnimationInfo()
        {
            EditorGUILayout.LabelField("动画信息", EditorStyles.boldLabel);
            DrawInfoRow("总时长", clip ? $"{clip.length:0.###} s" : "—");
            DrawInfoRow("源帧率", clip ? $"{clip.frameRate:0.##} fps" : "—");
            GUILayout.Space(12f);
            EditorGUILayout.LabelField("烘焙统计", EditorStyles.boldLabel);
            if (!_preview)
            {
                EditorGUILayout.LabelField("采样后显示 Tick 和位移统计。", EditorStyles.wordWrappedMiniLabel);
                return;
            }

            int totalTicks = _preview.positionX.length - 1;
            DrawInfoRow("总 Tick", totalTicks.ToString());
            DrawInfoRow("水平路径长度", $"{_preview.planarDistance:0.###} m");
            DrawInfoRow("平均水平速度", $"{_preview.AveragePlanarSpeed:0.###} m/s");
            float minHeight = _trajectoryBounds.min.y - _startPosition.y;
            float maxHeight = _trajectoryBounds.max.y - _startPosition.y;
            DrawInfoRow("竖直位移范围", $"{minHeight:0.###} ～ {maxHeight:0.###} m");
            DrawInfoRow("累计转角", $"{_preview.TotalYaw:0.##}°");
            DrawInfoRow("平均转向速度", $"{_preview.AverageYawSpeed:0.##}°/s");
        }

        private static void DrawInfoRow(string label, string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(label, EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label(value, EditorStyles.label);
            }
        }

        private void DrawBakeActions()
        {
            string issue = GetInputIssue();
            if (issue != null)
                EditorGUILayout.HelpBox(issue, MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(issue != null))
                {
                    if (GUILayout.Button(_preview ? "重新采样" : "采样并预览"))
                        BakePreview();
                }

                using (new EditorGUI.DisabledScope(!_preview || issue != null))
                {
                    if (GUILayout.Button("保存运动数据…"))
                        SavePreview();
                }
            }
        }

        private void DrawPreview()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(new GUIContent("动画预览", "点击预览后按空格播放或暂停。"), EditorStyles.miniBoldLabel);
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(!_preview))
                {
                    if (GUILayout.Button("居中", EditorStyles.toolbarButton, GUILayout.Width(45)))
                        FramePreview();
                }
            }

            Rect rect = GUILayoutUtility.GetRect(100, 220, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            int control = GUIUtility.GetControlID(FocusType.Keyboard, rect);
            Event current = Event.current;
            EventType eventType = current.GetTypeForControl(control);
            if (eventType == EventType.MouseDown && current.button == 0 && !current.alt && rect.Contains(current.mousePosition))
            {
                GUIUtility.keyboardControl = control;
                EditorGUIUtility.editingTextField = false;
                current.Use();
            }
            else if (eventType == EventType.KeyDown && current.keyCode == KeyCode.Space && _preview &&
                GUIUtility.keyboardControl == control && GUIUtility.hotControl == 0)
            {
                TogglePlayback();
                current.Use();
            }
            _viewport.Draw(rect, this);
        }

        private void DrawPlaybackControls()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            using (new EditorGUI.DisabledScope(!_preview))
            {
                float duration = _preview ? _preview.duration : 0f;
                if (GUILayout.Button(_playing ? "暂停" : "播放", EditorStyles.toolbarButton, GUILayout.Width(45)))
                    TogglePlayback();

                if (GUILayout.Button("停止", EditorStyles.toolbarButton, GUILayout.Width(45)))
                {
                    _playing = false;
                    SetPreviewTime(0f);
                }

                EditorGUI.BeginChangeCheck();
                float time = EditorGUILayout.Slider(_time, 0f, duration, GUILayout.MinWidth(100));
                if (EditorGUI.EndChangeCheck())
                {
                    _playing = false;
                    SetPreviewTime(time);
                }

                GUILayout.Label($"{_time:F2} / {duration:F2} s", EditorStyles.miniLabel, GUILayout.Width(100));
                loopPreview = GUILayout.Toggle(loopPreview, "循环", EditorStyles.toolbarButton, GUILayout.Width(45));
            }
        }

        private void TogglePlayback()
        {
            if (_time >= _preview.duration)
                SetPreviewTime(0f);
            _playing = !_playing;
            _lastTime = EditorApplication.timeSinceStartup;
            Repaint();
        }

        private string GetInputIssue()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return "请退出播放模式后采样。";
            if (!modelPrefab)
                return "请选择包含 Animator 和骨骼的角色模型 Prefab。";
            if (!clip)
                return "请选择动画片段。";
            if (clip.legacy || clip.length <= 0f)
                return "请选择时长大于零的非 Legacy 动画。";
            return null;
        }

        private void SavePreview()
        {
            string path = EditorUtility.SaveFilePanelInProject("保存运动数据", clip.name + "_Motion", "asset", "选择新的数据资产路径");
            if (string.IsNullOrEmpty(path))
                return;

            path = AssetDatabase.GenerateUniqueAssetPath(path);
            BakedAnimMotionData asset = Instantiate(_preview);
            asset.hideFlags = HideFlags.None;
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(asset);
            ShowNotification(new GUIContent("运动数据已保存"));
        }

        private void ClearPreview()
        {
            _playing = false;
            _time = 0f;
            _sampledTime = -1f;
            if (_graph.IsValid())
                _graph.Destroy();
            if (_holder)
                DestroyImmediate(_holder);
            if (_preview)
                DestroyImmediate(_preview);

            _holder = null;
            _preview = null;
            _modelRoot = null;
            _animator = null;
            _restPose = null;
            _trajectory.positionCount = 0;
        }

        #region 生命周期
        private void OnEnable()
        {
            minSize = new Vector2(640f, 420f);
            _viewport = new BattlePreviewViewport(showAxes: false);
            var trajectoryRoot = new GameObject("Baked Motion Trajectory");
            _viewport.AddSingleGO(trajectoryRoot);
            _trajectory = _viewport.CreateLine(trajectoryRoot.transform, "Trajectory", new Color(1f, 0.65f, 0.15f), 0.04f);
            EditorApplication.update += OnUpdate;
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawAnimationPanel();
                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
                {
                    DrawPreview();
                    DrawPlaybackControls();
                }
            }
        }

        private void OnUpdate()
        {
            if (!_playing)
                return;

            double now = EditorApplication.timeSinceStartup;
            float time = _time + (float)(now - _lastTime);
            _lastTime = now;
            float duration = _preview.duration;
            if (time >= duration)
            {
                if (loopPreview)
                    time %= duration;
                else
                {
                    time = duration;
                    _playing = false;
                }
            }

            SetPreviewTime(time);
            Repaint();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnUpdate;
            ClearPreview();
            _viewport.Dispose();
        }
        #endregion
    }
}
