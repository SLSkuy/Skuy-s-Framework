using GamePlay.BattleSystem;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    /// <summary>技能表 Inspector 的编辑器入口。</summary>
    [CustomEditor(typeof(SkillTable))]
    public sealed class SkillTableEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
         
            EditorGUILayout.Space();
            
            if (GUILayout.Button("打开技能编辑器")) SkillEditorWindow.Open();
        }
    }
}
