using System;
using System.Collections.Generic;
using System.IO;
using GamePlay.BattleSystem;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    internal static class SkillAssetUtils
    {
        private const string ROOT = "Assets/AbilityConfigs";
        private const string DEFAULT_NAME = "新技能";

        public static SkillConfig CreateSkill(uint durationTicks)
        {
            if (!AssetDatabase.IsValidFolder(ROOT))
                AssetDatabase.CreateFolder("Assets", "AbilityConfigs");

            var usedIds = new HashSet<uint>();
            foreach (SkillConfig skill in LoadSkills())
                usedIds.Add(skill.skillId);

            uint id = 1;
            while (usedIds.Contains(id) || AssetDatabase.IsValidFolder(GetExpectedFolder(id, DEFAULT_NAME)))
                id = checked(id + 1);

            string folder = AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder(ROOT, $"{id}_{DEFAULT_NAME}"));
            if (string.IsNullOrEmpty(folder))
            {
                EditorUtility.DisplayDialog("新建技能失败", $"无法在 {ROOT} 下创建技能目录。", "确定");
                return null;
            }

            SkillConfig config = ScriptableObject.CreateInstance<SkillConfig>();
            config.skillId = id;
            config.skillName = DEFAULT_NAME;
            config.durationTicks = durationTicks;
            AssetDatabase.CreateAsset(config, $"{folder}/{DEFAULT_NAME}.asset");
            AssetDatabase.SaveAssetIfDirty(config);
            return config;
        }

        public static string GetFolder(SkillConfig skill)
        {
            string path = AssetDatabase.GetAssetPath(skill);
            return path.Substring(0, path.LastIndexOf('/'));
        }

        public static bool DrawIdentity(SkillConfig skill)
        {
            EditorGUILayout.LabelField("技能 ID", skill.skillId.ToString());
            EditorGUI.BeginChangeCheck();
            string name = EditorGUILayout.DelayedTextField("名称", skill.skillName).Trim();
            if (!EditorGUI.EndChangeCheck() || name == skill.skillName)
                return false;

            string error = RenameSkill(skill, name);
            if (string.IsNullOrEmpty(error)) return true;

            EditorUtility.DisplayDialog("修改技能失败", error, "确定");
            return false;
        }

        public static void DrawLocalAsset(SerializedProperty field, GUIContent label, Type assetType)
        {
            EditorGUI.BeginChangeCheck();
            UnityEngine.Object asset = EditorGUILayout.ObjectField(label, field.objectReferenceValue, assetType, false);
            if (EditorGUI.EndChangeCheck())
                field.objectReferenceValue = LocalizeAsset(asset, (SkillConfig)field.serializedObject.targetObject);
        }

        public static UnityEngine.Object LocalizeAsset(UnityEngine.Object asset, SkillConfig skill)
        {
            if (!asset) return null;
            string folder = GetFolder(skill);
            string path = AssetDatabase.GetAssetPath(asset);
            if (path.StartsWith(folder + "/", StringComparison.Ordinal)) return asset;

            UnityEngine.Object copy = UnityEngine.Object.Instantiate(asset);
            copy.name = asset.name;
            copy.hideFlags = HideFlags.None;
            string extension = asset is AnimationClip ? ".anim" : ".asset";
            string fileName = string.Join("_", asset.name.Split(Path.GetInvalidFileNameChars())).TrimEnd(' ', '.');
            AssetDatabase.CreateAsset(copy, AssetDatabase.GenerateUniqueAssetPath($"{folder}/{fileName}{extension}"));
            return copy;
        }

        private static string GetExpectedFolder(uint id, string name)
        {
            return $"{ROOT}/{id}_{name}";
        }

        private static IEnumerable<SkillConfig> LoadSkills()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:SkillConfig", new[] { ROOT }))
                yield return AssetDatabase.LoadAssetAtPath<SkillConfig>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private static string RenameSkill(SkillConfig skill, string name)
        {
            if (string.IsNullOrEmpty(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.EndsWith("."))
                return "请使用有效的技能名称。";

            string current = GetFolder(skill);
            string destination = GetExpectedFolder(skill.skillId, name);
            string fileName = Path.GetFileName(AssetDatabase.GetAssetPath(skill));
            string newFileName = $"{name}.asset";
            if (!string.Equals(fileName, newFileName, StringComparison.OrdinalIgnoreCase))
            {
                string error = AssetDatabase.ValidateMoveAsset($"{current}/{fileName}", $"{current}/{newFileName}");
                if (!string.IsNullOrEmpty(error)) return error;
            }

            bool moveFolder = !string.Equals(current, destination, StringComparison.Ordinal);
            if (moveFolder)
            {
                if (!string.Equals(Path.GetDirectoryName(current)?.Replace('\\', '/'), ROOT, StringComparison.Ordinal))
                    return $"技能配置必须位于 {ROOT} 下的独立目录。";

                string error = MoveAsset(current, destination);
                if (!string.IsNullOrEmpty(error)) return error;
            }

            if (!string.Equals(fileName, newFileName, StringComparison.Ordinal))
            {
                string error = MoveAsset($"{destination}/{fileName}", $"{destination}/{newFileName}");
                if (!string.IsNullOrEmpty(error))
                {
                    string rollbackError = moveFolder ? MoveAsset(destination, current) : null;
                    return string.IsNullOrEmpty(rollbackError) ? error : $"{error}\n目录恢复失败：{rollbackError}";
                }
            }

            skill.skillName = name;
            EditorUtility.SetDirty(skill);
            AssetDatabase.SaveAssetIfDirty(skill);
            return null;
        }

        private static string MoveAsset(string source, string destination)
        {
            if (!string.Equals(source, destination, StringComparison.OrdinalIgnoreCase))
                return AssetDatabase.MoveAsset(source, destination);

            string name = AssetDatabase.IsValidFolder(source)
                ? Path.GetFileName(destination)
                : Path.GetFileNameWithoutExtension(destination);
            return AssetDatabase.RenameAsset(source, name);
        }
    }
}
