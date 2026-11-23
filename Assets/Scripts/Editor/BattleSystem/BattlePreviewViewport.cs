using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Editor
{
    /// <summary>战斗编辑器共用的预览场景、取景和鼠标操作。</summary>
    internal sealed class BattlePreviewViewport : IDisposable
    {
        private readonly PreviewRenderUtility _preview = new();
        private readonly List<Material> _materials = new();
        private Vector3 _viewPivot = Vector3.up;
        private Vector2 _viewAngles = new Vector2(15, -34);
        private float _viewDistance = 7.5f;
        private int _viewButton;

        public BattlePreviewViewport(bool showAxes = true)
        {
            _preview.camera.fieldOfView = 35;
            _preview.camera.nearClipPlane = 0.01f;
            _preview.camera.farClipPlane = 1000;
            _preview.camera.useOcclusionCulling = false;
            _preview.lights[0].intensity = 1.2f;
            _preview.lights[0].transform.rotation = Quaternion.Euler(40, 30, 0);
            CreateEnvironment(showAxes);
        }

        public void AddSingleGO(GameObject instance)
        {
            _preview.AddSingleGO(instance);
        }

        public void Frame(Bounds bounds)
        {
            _viewPivot = bounds.center;
            _viewAngles = new Vector2(15, -34);
            float halfFov = _preview.camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            _viewDistance = Mathf.Max(1, bounds.extents.magnitude * 1.3f / Mathf.Sin(halfFov));
        }

        public void Draw(Rect rect, EditorWindow window)
        {
            HandleInput(rect, window);
            if (Event.current.type != EventType.Repaint || rect.width <= 0 || rect.height <= 0)
                return;

            _preview.BeginPreview(rect, GUIStyle.none);
            Quaternion rotation = Quaternion.Euler(_viewAngles.x, _viewAngles.y, 0);
            _preview.camera.transform.SetPositionAndRotation(_viewPivot - rotation * Vector3.forward * _viewDistance, rotation);
            _preview.Render(true);
            GUI.DrawTexture(rect, _preview.EndPreview(), ScaleMode.StretchToFill, false);
        }

        public void Dispose()
        {
            _preview.Cleanup();
            foreach (Material material in _materials)
                UnityEngine.Object.DestroyImmediate(material);
            _materials.Clear();
        }

        public LineRenderer CreateLine(Transform parent, string name, Color color, float width)
        {
            var instance = new GameObject(name);
            instance.transform.SetParent(parent, false);
            var line = instance.AddComponent<LineRenderer>();
            line.sharedMaterial = CreateMaterial("Universal Render Pipeline/Unlit", color);
            line.positionCount = 0;
            line.startWidth = line.endWidth = width;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        private void CreateEnvironment(bool showAxes)
        {
            _preview.camera.gameObject.AddComponent<Skybox>().material = CreateMaterial("Skybox/Procedural");
            _preview.camera.clearFlags = CameraClearFlags.Skybox;
            var grid = new GameObject("Preview Coordinates");
            AddSingleGO(grid);
            for (int i = -10; i <= 10; i++)
            {
                if (i == 0 && showAxes)
                    continue;
                CreateGridLine(grid.transform, new Vector3(i, 0, -10), new Vector3(i, 0, 10), Color.gray, 0.008f);
                CreateGridLine(grid.transform, new Vector3(-10, 0, i), new Vector3(10, 0, i), Color.gray, 0.008f);
            }

            if (!showAxes)
                return;

            CreateGridLine(grid.transform, Vector3.left * 10, Vector3.right * 10, Color.red, 0.02f);
            CreateGridLine(grid.transform, Vector3.zero, Vector3.up * 3, Color.green, 0.02f);
            CreateGridLine(grid.transform, Vector3.back * 10, Vector3.forward * 10, Color.blue, 0.02f);
        }

        private void CreateGridLine(Transform grid, Vector3 start, Vector3 end, Color color, float width)
        {
            LineRenderer line = CreateLine(grid, "Grid Line", color, width);
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
        }

        private Material CreateMaterial(string shaderName, Color? color = null)
        {
            var material = new Material(Shader.Find(shaderName)) { hideFlags = HideFlags.HideAndDontSave };
            if (color.HasValue)
                material.SetColor("_BaseColor", color.Value);
            _materials.Add(material);
            return material;
        }

        private void HandleInput(Rect rect, EditorWindow window)
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
                window.Repaint();
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
                window.Repaint();
            }
        }
    }
}
