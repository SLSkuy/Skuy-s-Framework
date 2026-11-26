using System.Collections.Generic;
using System.Linq;
using GamePlay.BattleSystem;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    /// <summary>只读查看连招节点及转换，并打开连招编辑器。</summary>
    [CustomEditor(typeof(CombatGraphConfig))]
    public sealed class CombatGraphConfigEditor : UnityEditor.Editor
    {
        private readonly HashSet<uint> _expandedNodes = new() { 0, uint.MaxValue };

        public override void OnInspectorGUI()
        {
            var config = (CombatGraphConfig)target;
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("技能表", config.skillTable, typeof(SkillTable), false);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"节点与转换（{config.nodes.Count} 个技能节点）", EditorStyles.boldLabel);
            DrawNode(config, 0, "入口", config.rootTransitions);
            DrawNode(config, uint.MaxValue, "任意状态", config.anyTransitions);
            foreach (var node in config.nodes)
                DrawNode(config, node.nodeId, GetNodeLabel(config, node), node.transitions);

            EditorGUILayout.Space();
            if (GUILayout.Button("打开连招编辑器")) CombatGraphEditorWindow.Open(config);
        }

        private void DrawNode(CombatGraphConfig config, uint id, string label, List<CombatGraphConfig.TransitionData> transitions)
        {
            string summary = $"{label}（{transitions.Count} 条转换）";
            if (transitions.Count == 0)
            {
                EditorGUILayout.LabelField(summary);
                return;
            }

            bool expanded = EditorGUILayout.Foldout(_expandedNodes.Contains(id), summary, true);
            if (!expanded)
            {
                _expandedNodes.Remove(id);
                return;
            }
            _expandedNodes.Add(id);

            using (new EditorGUI.IndentLevelScope())
            {
                for (int i = 0; i < transitions.Count; i++)
                    DrawTransition(config, transitions[i], i + 1);
            }
        }

        private static void DrawTransition(CombatGraphConfig config, CombatGraphConfig.TransitionData transition, int priority)
        {
            var target = config.nodes.Find(node => node.nodeId == transition.targetNodeId);
            string label = target != null ? GetNodeLabel(config, target) : $"缺失节点 {transition.targetNodeId}";
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"转换 {priority}", $"{transition.command} · {transition.commandState}");
                EditorGUILayout.LabelField("源视图", GetViewLabel(transition.sourceView));
                EditorGUILayout.LabelField("目标节点", $"{label} · {GetViewLabel(transition.targetView)}", EditorStyles.wordWrappedLabel);
            }
        }

        private static string GetNodeLabel(CombatGraphConfig config, CombatGraphConfig.NodeData node)
        {
            var skill = config.skillTable ? config.skillTable.skills?.FirstOrDefault(item => item && item.skillId == node.skillId) : null;
            string name = skill ? skill.skillName : "缺失技能";
            return $"节点 {node.nodeId} · 技能 {node.skillId} {name}";
        }

        private static string GetViewLabel(int view) => view == 0 ? "主节点" : $"副本 {view}";
    }
}
