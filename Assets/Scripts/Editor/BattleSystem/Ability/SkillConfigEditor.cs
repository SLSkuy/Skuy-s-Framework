using GamePlay.BattleSystem;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(SkillConfig))]
    internal sealed class SkillConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var skill = (SkillConfig)target;
            EditorGUILayout.LabelField("技能 ID", skill.skillId.ToString());
            EditorGUILayout.LabelField("名称", skill.skillName);
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            DrawPropertiesExcluding(serializedObject, "m_Script", nameof(SkillConfig.skillId), nameof(SkillConfig.skillName));
            if (EditorGUI.EndChangeCheck())
            {
                LocalizeEffects();
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("打开技能编辑器")) SkillEditorWindow.Open(skill);
        }

        private void LocalizeEffects()
        {
            var skill = (SkillConfig)target;
            SerializedProperty effects = serializedObject.FindProperty(nameof(SkillConfig.effects));
            for (int i = 0; i < effects.arraySize; i++)
            {
                SerializedProperty effect = effects.GetArrayElementAtIndex(i);
                SerializedProperty field = effect.managedReferenceValue switch
                {
                    AnimationSkillEffect => effect.FindPropertyRelative(nameof(AnimationSkillEffect.clip)),
                    RootMotionSkillEffect => effect.FindPropertyRelative(nameof(RootMotionSkillEffect.motionData)),
                    _ => null
                };
                if (field != null)
                    field.objectReferenceValue = SkillAssetUtils.LocalizeAsset(field.objectReferenceValue, skill);
            }
        }
    }
}
