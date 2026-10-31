using System;
using GamePlay.BattleSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace EditorTools
{
    /// <summary>在独立预览场景中烘焙单个动画的根运动，不依赖角色模拟或网络模块。</summary>
    public class AnimationMotionBakerWindow : EditorWindow
    {
        [SerializeField] private GameObject modelPrefab;
        [SerializeField] private AnimationClip clip;
        [SerializeField] private int sampleRate = 60;

        private AnimationMotionData _preview;
        private Vector2 _scroll;
        private float _previewTime;
        private int _previewTab;
        private string _savedPath;
        private Vector2[] _trajectory;
        private Vector2 _trajectoryMin;
        private Vector2 _trajectoryMax;
        private GUIStyle _subtitleStyle;
        private GUIStyle _metricStyle;
        private GUIStyle _hintStyle;
        private static readonly Color Accent = new Color(0.25f, 0.75f, 0.95f);
        private static readonly Color StartColor = new Color(0.35f, 0.85f, 0.55f);
        private static readonly Color EndColor = new Color(1f, 0.65f, 0.3f);

        [MenuItem("Tools/Framework/Root Motion Baker")]
        public static void Open()
        {
            AnimationMotionBakerWindow window = GetWindow<AnimationMotionBakerWindow>("根运动烘焙");
            window.minSize = new Vector2(460f, 540f);
        }

        private void BakePreview()
        {
            ClearPreview();
            if (clip.legacy || clip.length <= 0f)
            {
                EditorUtility.DisplayDialog("无法采样", "请选择时长大于零的非 Legacy 动画。", "确定");
                return;
            }
            Scene scene = EditorSceneManager.NewPreviewScene();
            PlayableGraph graph = default;
            AnimationMotionData data = CreateInstance<AnimationMotionData>();
            try
            {
                // 先停用再实例化，移除临时实例上的游戏脚本后才激活。
                GameObject holder = new GameObject("MotionBakePreview");
                SceneManager.MoveGameObjectToScene(holder, scene);
                holder.SetActive(false);
                GameObject instance = Instantiate(modelPrefab, holder.transform);
                instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                foreach (MonoBehaviour behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
                    DestroyImmediate(behaviour);
                Animator[] animators = instance.GetComponentsInChildren<Animator>(true);
                if (animators.Length != 1)
                    throw new InvalidOperationException("请选择包含且仅包含一个 Animator 的角色模型 Prefab。");
                Animator animator = animators[0];
                if (clip.isHumanMotion && (!animator.avatar || !animator.avatar.isValid || !animator.avatar.isHuman))
                    throw new InvalidOperationException("Humanoid 动画需要模型包含有效的 Humanoid Avatar。");
                for (Transform node = animator.transform; node != holder.transform; node = node.parent)
                    node.gameObject.SetActive(true);
                animator.enabled = true;
                animator.runtimeAnimatorController = null;
                animator.applyRootMotion = true;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                holder.SetActive(true);

                graph = PlayableGraph.Create("MotionBake");
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                AnimationClipPlayable playable = AnimationClipPlayable.Create(graph, clip);
                playable.SetApplyFootIK(false);
                playable.SetApplyPlayableIK(false);
                AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Motion", animator);
                output.SetSourcePlayable(playable);
                graph.Play();
                graph.Evaluate(0f);

                Quaternion startRotation = animator.transform.rotation;
                Quaternion rotation = startRotation;
                Vector3 worldPosition = animator.transform.position;
                Vector3 position = Vector3.zero;
                float yaw = 0f;
                float previousTime = 0f;
                data.sourceClip = clip;
                data.sourceAvatar = animator.avatar;
                data.duration = clip.length;
                data.sampleRate = sampleRate;
                data.positionX.AddKey(0f, 0f);
                data.positionZ.AddKey(0f, 0f);
                data.yaw.AddKey(0f, 0f);
                int steps = Mathf.CeilToInt(clip.length * sampleRate);
                for (int i = 1; i <= steps; i++)
                {
                    float time = Mathf.Min((float)i / sampleRate, clip.length);
                    animator.transform.SetPositionAndRotation(worldPosition, rotation);
                    graph.Evaluate(time - previousTime);
                    Vector3 delta = animator.deltaPosition;
                    Vector3 localDelta = Quaternion.Inverse(startRotation) * delta;
                    position += localDelta;
                    data.planarDistance += new Vector2(localDelta.x, localDelta.z).magnitude;
                    Quaternion nextRotation = rotation * animator.deltaRotation;
                    Vector3 previousForward = Vector3.ProjectOnPlane(rotation * Vector3.forward, Vector3.up);
                    Vector3 nextForward = Vector3.ProjectOnPlane(nextRotation * Vector3.forward, Vector3.up);
                    yaw += Vector3.SignedAngle(previousForward, nextForward, Vector3.up);
                    data.positionX.AddKey(time, position.x);
                    data.positionZ.AddKey(time, position.z);
                    data.yaw.AddKey(time, yaw);
                    worldPosition += delta;
                    rotation = nextRotation;
                    previousTime = time;
                }
                SetLinearTangents(data.positionX);
                SetLinearTangents(data.positionZ);
                SetLinearTangents(data.yaw);
                data.hideFlags = HideFlags.HideAndDontSave;
                _preview = data;
                CacheTrajectory();
                _previewTime = 0f;
            }
            catch (Exception exception)
            {
                DestroyImmediate(data);
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("采样失败", exception.Message, "确定");
            }
            finally
            {
                if (graph.IsValid()) graph.Destroy();
                EditorSceneManager.ClosePreviewScene(scene);
            }
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

        private void CacheTrajectory()
        {
            Keyframe[] xs = _preview.positionX.keys;
            Keyframe[] zs = _preview.positionZ.keys;
            _trajectory = new Vector2[xs.Length];
            _trajectoryMin = Vector2.zero;
            _trajectoryMax = Vector2.zero;
            for (int i = 0; i < xs.Length; i++)
            {
                _trajectory[i] = new Vector2(xs[i].value, zs[i].value);
                _trajectoryMin = Vector2.Min(_trajectoryMin, _trajectory[i]);
                _trajectoryMax = Vector2.Max(_trajectoryMax, _trajectory[i]);
            }
        }

        private void DrawTrajectory()
        {
            EditorGUILayout.LabelField("水平轨迹", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("X 向右 · Z 向上 · 绿色起点 / 橙色终点", _hintStyle);
            Rect rect = GUILayoutUtility.GetRect(100f, 240f, GUILayout.ExpandWidth(true));
            if (Event.current.type != EventType.Repaint) return;
            DrawPlotBackground(rect);
            Vector2 center = (_trajectoryMin + _trajectoryMax) * 0.5f;
            float scale = Mathf.Min((rect.width - 48f) / Mathf.Max(_trajectoryMax.x - _trajectoryMin.x, 0.1f),
                (rect.height - 48f) / Mathf.Max(_trajectoryMax.y - _trajectoryMin.y, 0.1f));
            Vector3 Map(Vector2 point)
            {
                Vector2 offset = (point - center) * scale;
                return new Vector3(rect.center.x + offset.x, rect.center.y - offset.y);
            }
            Vector3[] points = new Vector3[_trajectory.Length];
            for (int i = 0; i < points.Length; i++) points[i] = Map(_trajectory[i]);
            Handles.BeginGUI();
            Handles.color = Accent;
            Handles.DrawAAPolyLine(2.5f, points);
            Handles.color = StartColor;
            Handles.DrawSolidDisc(points[0], Vector3.forward, 4f);
            Handles.color = EndColor;
            Handles.DrawSolidDisc(points[points.Length - 1], Vector3.forward, 4f);
            Vector3 position = _preview.EvaluatePosition(_previewTime);
            Vector3 cursor = Map(new Vector2(position.x, position.z));
            Vector3 direction = Quaternion.Euler(0f, _preview.yaw.Evaluate(_previewTime), 0f) * Vector3.forward;
            Handles.color = EditorGUIUtility.isProSkin ? Color.white : new Color(0.15f, 0.2f, 0.25f);
            Handles.DrawSolidDisc(cursor, Vector3.forward, 4f);
            Vector3 tip = cursor + new Vector3(direction.x, -direction.z) * 18f;
            Handles.DrawAAPolyLine(2f, cursor, tip);
            Handles.EndGUI();
            GUI.Label(new Rect(rect.x + 8f, rect.y + 6f, rect.width - 16f, 20f),
                $"范围 X {_trajectoryMin.x:F2} … {_trajectoryMax.x:F2} m    Z {_trajectoryMin.y:F2} … {_trajectoryMax.y:F2} m", _hintStyle);
        }

        private static void DrawPlotBackground(Rect rect)
        {
            EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin
                ? new Color(0.1f, 0.11f, 0.13f) : new Color(0.9f, 0.92f, 0.94f));
            Color grid = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.06f) : new Color(0f, 0f, 0f, 0.07f);
            for (int i = 1; i < 6; i++)
            {
                EditorGUI.DrawRect(new Rect(rect.x + rect.width * i / 6f, rect.y, 1f, rect.height), grid);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y + rect.height * i / 6f, rect.width, 1f), grid);
            }
        }

        private void DrawCurve(string title, AnimationCurve curve, Color color, string unit)
        {
            EditorGUILayout.LabelField(title, $"{curve.Evaluate(_previewTime):F3} {unit}");
            Rect rect = GUILayoutUtility.GetRect(100f, 105f, GUILayout.ExpandWidth(true));
            if (Event.current.type != EventType.Repaint) return;
            DrawPlotBackground(rect);
            float min = 0f;
            float max = 0f;
            const int count = 160;
            for (int i = 0; i <= count; i++)
            {
                float value = curve.Evaluate(_preview.duration * i / count);
                min = Mathf.Min(min, value);
                max = Mathf.Max(max, value);
            }
            float range = Mathf.Max(max - min, 0.01f);
            min -= range * 0.15f;
            max += range * 0.15f;
            range = max - min;
            Vector3[] points = new Vector3[count + 1];
            for (int i = 0; i <= count; i++)
            {
                float value = curve.Evaluate(_preview.duration * i / count);
                points[i] = new Vector3(rect.x + rect.width * i / count,
                    rect.yMax - 8f - (rect.height - 16f) * (value - min) / range);
            }
            float cursorX = rect.x + rect.width * _previewTime / _preview.duration;
            Color cursorColor = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.5f) : new Color(0f, 0f, 0f, 0.45f);
            EditorGUI.DrawRect(new Rect(cursorX, rect.y, 1f, rect.height), cursorColor);
            float zeroY = rect.yMax - 8f - (rect.height - 16f) * -min / range;
            EditorGUI.DrawRect(new Rect(rect.x, zeroY, rect.width, 1f), cursorColor * new Color(1f, 1f, 1f, 0.4f));
            Handles.BeginGUI();
            Handles.color = color;
            Handles.DrawAAPolyLine(2f, points);
            Handles.EndGUI();
            GUI.Label(new Rect(rect.x + 5f, rect.y + 2f, 100f, 18f), $"{max:F2} {unit}", _hintStyle);
            GUI.Label(new Rect(rect.x + 5f, rect.yMax - 18f, 100f, 18f), $"{min:F2} {unit}", _hintStyle);
        }

        private void DrawMetric(string label, string value)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.ExpandWidth(true)))
            {
                EditorGUILayout.LabelField(label, _hintStyle);
                EditorGUILayout.LabelField(value, _metricStyle);
            }
        }

        private string GetInputIssue()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return "请退出播放模式后采样。";
            if (!modelPrefab) return "选择包含 Animator 和骨骼的角色模型 Prefab。";
            if (!clip) return "选择需要提取根运动的 AnimationClip。";
            if (clip.legacy || clip.length <= 0f) return "请选择时长大于零的非 Legacy 动画。";
            return null;
        }

        private void SavePreview()
        {
            string path = EditorUtility.SaveFilePanelInProject("保存运动数据", clip.name + "_Motion", "asset", "选择新的数据资产路径");
            if (string.IsNullOrEmpty(path)) return;
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            AnimationMotionData asset = Instantiate(_preview);
            asset.hideFlags = HideFlags.None;
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            _savedPath = path;
            EditorGUIUtility.PingObject(asset);
            ShowNotification(new GUIContent("运动数据已保存"));
        }

        private void ClearPreview()
        {
            if (_preview) DestroyImmediate(_preview);
            _preview = null;
            _trajectory = null;
            _previewTime = 0f;
            _savedPath = null;
        }

        #region 生命周期
        private void OnEnable()
        {
            minSize = new Vector2(460f, 540f);
        }

        private void OnGUI()
        {
            _subtitleStyle ??= new GUIStyle(EditorStyles.wordWrappedMiniLabel) { fontSize = 11 };
            _metricStyle ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 17, fixedHeight = 26f };
            _hintStyle ??= new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Space(6f);
                EditorGUILayout.LabelField("根运动曲线烘焙", EditorStyles.largeLabel);
                EditorGUILayout.LabelField("提取动画的水平轨迹与转向数据，预览后导出为独立资产。", _subtitleStyle);
                GUILayout.Space(6f);
            }
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("资源与采样", EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                modelPrefab = (GameObject)EditorGUILayout.ObjectField(new GUIContent("角色模型", "包含单个 Animator 的模型 Prefab"), modelPrefab, typeof(GameObject), false);
                clip = (AnimationClip)EditorGUILayout.ObjectField("动画片段", clip, typeof(AnimationClip), false);
                sampleRate = EditorGUILayout.IntSlider("采样频率 / Hz", sampleRate, 15, 240);
                if (EditorGUI.EndChangeCheck()) ClearPreview();
                if (clip)
                    EditorGUILayout.LabelField($"时长 {clip.length:F3} s   ·   原始帧率 {clip.frameRate:F0} fps   ·   预计 {Mathf.CeilToInt(clip.length * sampleRate) + 1} 个采样点", _hintStyle);
                string issue = GetInputIssue();
                if (issue != null) EditorGUILayout.HelpBox(issue, MessageType.Info);
                using (new EditorGUI.DisabledScope(issue != null))
                {
                    if (GUILayout.Button(_preview ? "重新采样" : "采样并预览", GUILayout.Height(30f))) BakePreview();
                }
            }
            if (_preview)
            {
                GUILayout.Space(6f);
                EditorGUILayout.LabelField("采样结果", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawMetric("平均水平速度", $"{_preview.AveragePlanarSpeed:F3} m/s");
                    DrawMetric("水平路径长度", $"{_preview.planarDistance:F3} m");
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawMetric("累计转角", $"{_preview.TotalYaw:F2}°");
                    DrawMetric("平均转向速度", $"{_preview.AverageYawSpeed:F2}°/s");
                }
                EditorGUILayout.LabelField($"{_preview.duration:F3} 秒 · {_preview.positionX.length} 个采样点 · {_preview.sampleRate} Hz", _hintStyle);
                if (_preview.planarDistance < 0.001f && Mathf.Abs(_preview.TotalYaw) < 0.01f)
                    EditorGUILayout.HelpBox("未检测到明显根运动。请检查动画是否为原地动作、Bake Into Pose 设置及 Avatar。", MessageType.Warning);
                GUILayout.Space(6f);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    _previewTab = GUILayout.Toolbar(_previewTab, new[] { "水平轨迹", "运动曲线" });
                    _previewTime = EditorGUILayout.Slider("查看时间 / s", _previewTime, 0f, _preview.duration);
                    if (_previewTab == 0) DrawTrajectory();
                    else
                    {
                        DrawCurve("X 位移", _preview.positionX, Accent, "m");
                        DrawCurve("Z 位移", _preview.positionZ, StartColor, "m");
                        DrawCurve("Yaw 转角", _preview.yaw, EndColor, "°");
                    }
                    Vector3 position = _preview.EvaluatePosition(_previewTime);
                    EditorGUILayout.LabelField($"当前采样  X {position.x:F3} m   Z {position.z:F3} m   Yaw {_preview.yaw.Evaluate(_previewTime):F2}°", _hintStyle);
                    EditorGUILayout.LabelField("曲线横轴：0 → 动画时长（秒），拖动时间滑块检查对应轨迹位置。", _hintStyle);
                }
            }
            else
            {
                GUILayout.Space(16f);
                EditorGUILayout.HelpBox("采样后显示速度统计、俯视轨迹和位移 / 转角曲线。采样在独立预览场景中完成。", MessageType.None);
            }
            GUILayout.Space(6f);
            EditorGUILayout.LabelField("XZ 位移或 Rotation 已 Bake Into Pose 时，对应运动可能为零。", _hintStyle);
            EditorGUILayout.EndScrollView();
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUI.DisabledScope(!_preview))
                {
                    if (GUILayout.Button("保存运动数据…", GUILayout.Height(30f))) SavePreview();
                }
                if (!string.IsNullOrEmpty(_savedPath)) EditorGUILayout.SelectableLabel(_savedPath, EditorStyles.miniLabel, GUILayout.Height(18f));
            }
        }

        private void OnDisable()
        {
            ClearPreview();
        }
        #endregion
    }
}
