using GamePlay.BattleSystem;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(BakedAnimMotionData))]
    internal sealed class BakedAnimMotionDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            if (GUILayout.Button("打开根运动烘焙窗口"))
                AnimationMotionBakerWindow.Open((BakedAnimMotionData)target);
        }
    }
}
