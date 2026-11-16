using GamePlay.BattleSystem;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    /// <summary>连招资产的编辑入口。</summary>
    [CustomEditor(typeof(CombatGraphConfig))]
    public sealed class CombatGraphConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var config = (CombatGraphConfig)target;
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("技能表", config.skillTable, typeof(SkillTable), false);
                EditorGUILayout.IntField("节点数", config.nodes.Count);
            }
            if (GUILayout.Button("打开连招编辑器")) CombatGraphEditorWindow.Open(config);
        }
    }
}