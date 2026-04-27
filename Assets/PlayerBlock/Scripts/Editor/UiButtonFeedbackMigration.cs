#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlayerBlock.Editor
{
    [InitializeOnLoad]
    public static class UiButtonFeedbackMigration
    {
        private const string CompletionKey = "LanShooter.UiButtonFeedbackMigration.V1";

        static UiButtonFeedbackMigration()
        {
            EditorApplication.delayCall += TryRunOnce;
        }

        [MenuItem("Tools/Block Player/Normalize Button Feedback")]
        private static void RunMenuItem()
        {
            RunMigration(force: true);
        }

        private static void TryRunOnce()
        {
            if (EditorPrefs.GetBool(CompletionKey, false))
            {
                return;
            }

            RunMigration(force: false);
        }

        private static void RunMigration(bool force)
        {
            if (!force && EditorPrefs.GetBool(CompletionKey, false))
            {
                return;
            }

            var changed = false;
            var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });

            foreach (var guid in sceneGuids)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(guid);
                if (!scenePath.EndsWith(".unity"))
                {
                    continue;
                }

                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                if (NormalizeScene(scene))
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    changed = true;
                }
            }

            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/PlayerBlock/UI", "Assets/Scenes" });
            foreach (var guid in prefabGuids)
            {
                var prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!prefabPath.EndsWith(".prefab"))
                {
                    continue;
                }

                if (NormalizePrefab(prefabPath))
                {
                    changed = true;
                }
            }

            EditorPrefs.SetBool(CompletionKey, true);
            if (changed)
            {
                AssetDatabase.Refresh();
            }
        }

        private static bool NormalizeScene(Scene scene)
        {
            var modified = false;
            var buttons = Object.FindObjectsByType<PlayerBlock.UiButtonFeedback>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < buttons.Length; i++)
            {
                var feedback = buttons[i];
                if (feedback == null || feedback.gameObject.scene != scene)
                {
                    continue;
                }

                Apply(feedback);
                EditorUtility.SetDirty(feedback);
                modified = true;
            }

            return modified;
        }

        private static bool NormalizePrefab(string prefabPath)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
            {
                return false;
            }

            var modified = false;
            var buttons = root.GetComponentsInChildren<PlayerBlock.UiButtonFeedback>(true);
            for (var i = 0; i < buttons.Length; i++)
            {
                Apply(buttons[i]);
                modified = true;
            }

            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }

            PrefabUtility.UnloadPrefabContents(root);
            return modified;
        }

        private static void Apply(PlayerBlock.UiButtonFeedback feedback)
        {
            var serializedObject = new SerializedObject(feedback);
            SetFloat(serializedObject, "hoverScale", 1.05f);
            SetFloat(serializedObject, "pressedScale", 0.985f);
            SetColor(serializedObject, "hoverTint", new Color(1f, 0.995f, 0.97f, 1f));
            SetColor(serializedObject, "pressedTint", new Color(0.93f, 0.95f, 0.98f, 1f));
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(SerializedObject serializedObject, string propertyName, float value)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        private static void SetColor(SerializedObject serializedObject, string propertyName, Color value)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.colorValue = value;
            }
        }
    }
}
#endif
