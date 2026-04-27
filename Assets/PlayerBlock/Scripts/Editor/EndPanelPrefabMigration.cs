#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using PlayerBlock;

namespace LanShooter.EditorTools
{
    [InitializeOnLoad]
    public static class EndPanelPrefabMigration
    {
        private const string PrefabPath = "Assets/PlayerBlock/UI/EndPanel.prefab";
        private const string CompletionKey = "LanShooter.EndPanelPrefabMigration.V3";

        static EndPanelPrefabMigration()
        {
            EditorApplication.delayCall += TryRunOnce;
        }

        [MenuItem("Tools/LanShooter/Convert EndPanels To Prefab")]
        private static void ConvertMenuItem()
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

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"EndPanel prefab not found at {PrefabPath}");
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
                var changed = ConvertScene(scene, prefab);
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
                Debug.Log($"Converted EndPanel to prefab in {changedScenes.Count} scene(s): {string.Join(", ", changedScenes)}");
                AssetDatabase.Refresh();
            }
        }

        private static bool ConvertScene(Scene scene, GameObject prefab)
        {
            var changed = false;
            var roots = scene.GetRootGameObjects();

            foreach (var root in roots)
            {
                var panels = root.GetComponentsInChildren<Transform>(true);
                for (var i = 0; i < panels.Length; i++)
                {
                    var panelTransform = panels[i];
                    if (panelTransform.name != "EndPanel")
                    {
                        continue;
                    }

                    var panelObject = panelTransform.gameObject;
                    if (IsAlreadyPrefabInstance(panelObject, prefab))
                    {
                        continue;
                    }

                    var parent = panelTransform.parent;
                    var siblingIndex = panelTransform.GetSiblingIndex();

                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                    if (instance == null)
                    {
                        Debug.LogError($"Failed to instantiate EndPanel prefab in scene {scene.path}");
                        continue;
                    }

                    instance.name = "EndPanel";

                    var instanceTransform = instance.transform;
                    if (parent != null)
                    {
                        instanceTransform.SetParent(parent, false);
                    }
                    instanceTransform.SetSiblingIndex(siblingIndex);

                    Object.DestroyImmediate(panelObject);
                    ClearCombatHudBindings(scene);
                    changed = true;
                }
            }

            return changed;
        }

        private static bool IsAlreadyPrefabInstance(GameObject panelObject, GameObject prefab)
        {
            var source = PrefabUtility.GetCorrespondingObjectFromSource(panelObject);
            return source != null && source == prefab;
        }

        private static void ClearCombatHudBindings(Scene scene)
        {
            var huds = Resources.FindObjectsOfTypeAll<CombatHud>();
            foreach (var hud in huds)
            {
                if (hud == null || hud.gameObject.scene != scene)
                {
                    continue;
                }

                var serializedObject = new SerializedObject(hud);
                SetObjectReference(serializedObject, "endPanel", null);
                SetObjectReference(serializedObject, "endPanelLabel", null);
                SetObjectReference(serializedObject, "endPanelConfirmButton", null);
                SetObjectReference(serializedObject, "endPanelConfirmLabel", null);
                SetObjectReference(serializedObject, "endPanelMainMenuButton", null);
                SetObjectReference(serializedObject, "endPanelMainMenuLabel", null);
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(hud);
            }
        }

        private static void SetObjectReference(SerializedObject serializedObject, string propertyName, Object value)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }
    }
}
#endif
