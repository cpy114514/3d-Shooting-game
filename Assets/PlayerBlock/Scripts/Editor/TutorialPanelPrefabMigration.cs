#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlayerBlock.Editor
{
    [InitializeOnLoad]
    public static class TutorialPanelPrefabMigration
    {
        private const string CompletionKey = "LanShooter.TutorialPanelPrefabMigration.V1";
        private const string TutorialPanelPrefabPath = "Assets/PlayerBlock/UI/TutorialPanel.prefab";

        static TutorialPanelPrefabMigration()
        {
            EditorApplication.delayCall += TryRunOnce;
        }

        [MenuItem("Tools/Block Player/Convert Tutorial Panels To Prefab")]
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

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TutorialPanelPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"TutorialPanel prefab not found at {TutorialPanelPrefabPath}.");
                return;
            }

            var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
            var changedScenes = new List<string>();

            foreach (var guid in sceneGuids)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(guid);
                if (!scenePath.EndsWith(".unity"))
                {
                    continue;
                }

                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                var changed = ReplaceTutorialPanel(scene, prefab);
                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    changedScenes.Add(scenePath);
                }
            }

            EditorPrefs.SetBool(CompletionKey, true);
            if (changedScenes.Count > 0)
            {
                Debug.Log($"Converted tutorial panels to prefab in {changedScenes.Count} scene(s): {string.Join(", ", changedScenes)}");
                AssetDatabase.Refresh();
            }
        }

        private static bool ReplaceTutorialPanel(Scene scene, GameObject prefab)
        {
            var changed = false;
            var roots = scene.GetRootGameObjects();

            foreach (var root in roots)
            {
                if (root == null)
                {
                    continue;
                }

                var panel = FindChildRecursive(root.transform, "TutorialPanel");
                if (panel == null)
                {
                    continue;
                }

                var assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(panel.gameObject);
                if (assetPath == TutorialPanelPrefabPath)
                {
                    continue;
                }

                var parent = panel.parent;
                var siblingIndex = panel.GetSiblingIndex();
                var localPosition = panel.localPosition;
                var localRotation = panel.localRotation;
                var localScale = panel.localScale;
                var activeSelf = panel.gameObject.activeSelf;

                Object.DestroyImmediate(panel.gameObject);

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.name = "TutorialPanel";
                instance.transform.SetParent(parent, false);
                instance.transform.SetSiblingIndex(siblingIndex);
                instance.transform.localPosition = localPosition;
                instance.transform.localRotation = localRotation;
                instance.transform.localScale = localScale;
                instance.SetActive(activeSelf);

                changed = true;
                break;
            }

            return changed;
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == name)
                {
                    return child;
                }

                var nested = FindChildRecursive(child, name);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }
    }
}
#endif
