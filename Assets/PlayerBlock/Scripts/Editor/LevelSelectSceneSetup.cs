#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PlayerBlock.Editor
{
    [InitializeOnLoad]
    public static class LevelSelectSceneSetup
    {
        private const string CompletionKey = "PlayerBlock.LevelSelectSceneSetup.V2";
        private const string StartScenePath = "Assets/Scenes/start.unity";

        private static readonly (string DisplayName, string SceneName)[] LevelData =
        {
            ("LEVEL 1", "Maingame1"),
            ("LEVEL 2", "Maingame2"),
            ("LEVEL 3", "Maingame3"),
            ("LEVEL 4", "Maingame4"),
            ("BOSS", "Boss"),
            ("ENDLESS", EndlessModeDirector.SceneName)
        };

        static LevelSelectSceneSetup()
        {
            EditorApplication.delayCall += TryRunOnce;
        }

        [MenuItem("Tools/Block Player/Add Level Select UI")]
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

            var scene = EditorSceneManager.OpenScene(StartScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogWarning($"Could not open start scene at {StartScenePath}.");
                return;
            }

            if (EnsureLevelSelect(scene))
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.Refresh();
            }

            EditorPrefs.SetBool(CompletionKey, true);
        }

        private static bool EnsureLevelSelect(Scene scene)
        {
            var canvas = FindFirstCanvas(scene);
            if (canvas == null)
            {
                return false;
            }

            var changed = false;
            var startButton = FindSceneObject(scene, "Button")?.GetComponent<Button>();
            var panel = FindSceneObject(scene, "LevelSelectPanel");
            if (panel == null)
            {
                panel = CreateLevelSelectPanel(canvas.transform);
                changed = true;
            }
            else if (EnsureLevelButtons(panel.transform))
            {
                changed = true;
            }

            var menu = canvas.GetComponent<LevelSelectMenu>();
            if (menu == null)
            {
                menu = canvas.gameObject.AddComponent<LevelSelectMenu>();
                changed = true;
            }

            WireMenu(menu, panel, startButton);
            EditorUtility.SetDirty(menu);
            EditorUtility.SetDirty(panel);
            return changed;
        }

        private static GameObject CreateLevelSelectPanel(Transform parent)
        {
            var panel = new GameObject("LevelSelectPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(UiPanelAnimator));
            panel.layer = 5;
            panel.transform.SetParent(parent, false);

            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(1320f, 820f);

            var image = panel.GetComponent<Image>();
            image.color = new Color(0.035f, 0.04f, 0.055f, 0.96f);
            image.raycastTarget = true;

            var animator = panel.GetComponent<UiPanelAnimator>();
            animator.Configure(-36f, 0.94f, 5f, 0.004f, 0.18f, 0.12f);

            var title = CreateText(panel.transform, "LevelSelectTitleLabel", "SELECT LEVEL", 88, TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, new Vector2(0f, 300f), new Vector2(900f, 110f));
            title.color = new Color(1f, 0.92f, 0.74f, 1f);

            for (var i = 0; i < LevelData.Length; i++)
            {
                var row = i / 2;
                var column = i % 2;
                var x = column == 0 ? -330f : 330f;
                var y = 150f - row * 122f;

                CreateButton(panel.transform, $"LevelButton{i + 1}", $"{LevelData[i].DisplayName}", new Vector2(x, y), new Vector2(520f, 104f), 46);
            }

            CreateButton(panel.transform, "UnlockAllButton", "UNLOCK ALL", new Vector2(-260f, -318f), new Vector2(420f, 92f), 38);
            CreateButton(panel.transform, "LevelSelectBackButton", "BACK", new Vector2(260f, -318f), new Vector2(420f, 92f), 38);
            return panel;
        }

        private static bool EnsureLevelButtons(Transform panel)
        {
            var changed = false;
            for (var i = 0; i < LevelData.Length; i++)
            {
                if (FindChild(panel, $"LevelButton{i + 1}") != null)
                {
                    continue;
                }

                var row = i / 2;
                var column = i % 2;
                var x = column == 0 ? -330f : 330f;
                var y = 150f - row * 122f;
                CreateButton(panel, $"LevelButton{i + 1}", LevelData[i].DisplayName, new Vector2(x, y), new Vector2(520f, 104f), 46);
                changed = true;
            }

            return changed;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size, int fontSize)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(UiButtonFeedback));
            buttonObject.layer = 5;
            buttonObject.transform.SetParent(parent, false);

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.14f, 0.16f, 0.2f, 0.98f);
            image.raycastTarget = true;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            var text = CreateText(buttonObject.transform, $"{name}Label", label, fontSize, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            text.color = new Color(1f, 0.95f, 0.84f, 1f);
            return button;
        }

        private static Text CreateText(Transform parent, string name, string value, int fontSize, TextAnchor alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.layer = 5;
            textObject.transform.SetParent(parent, false);

            var text = textObject.GetComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static void WireMenu(LevelSelectMenu menu, GameObject panel, Button startButton)
        {
            var serialized = new SerializedObject(menu);
            serialized.FindProperty("levelSelectPanel").objectReferenceValue = panel;
            serialized.FindProperty("startGameButton").objectReferenceValue = startButton;
            serialized.FindProperty("unlockAllButton").objectReferenceValue = FindChild(panel.transform, "UnlockAllButton")?.GetComponent<Button>();
            serialized.FindProperty("backButton").objectReferenceValue = FindChild(panel.transform, "LevelSelectBackButton")?.GetComponent<Button>();

            var levels = serialized.FindProperty("levels");
            levels.arraySize = LevelData.Length;
            for (var i = 0; i < LevelData.Length; i++)
            {
                var entry = levels.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("DisplayName").stringValue = LevelData[i].DisplayName;
                entry.FindPropertyRelative("SceneName").stringValue = LevelData[i].SceneName;
                entry.FindPropertyRelative("IsEndlessMode").boolValue = LevelData[i].DisplayName == "ENDLESS";
                entry.FindPropertyRelative("Button").objectReferenceValue = FindChild(panel.transform, $"LevelButton{i + 1}")?.GetComponent<Button>();
                entry.FindPropertyRelative("Label").objectReferenceValue = FindChild(panel.transform, $"LevelButton{i + 1}Label")?.GetComponent<Text>();
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Canvas FindFirstCanvas(Scene scene)
        {
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null && canvases[i].gameObject.scene == scene)
                {
                    return canvases[i];
                }
            }

            return null;
        }

        private static GameObject FindSceneObject(Scene scene, string name)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var found = roots[i].name == name ? roots[i].transform : FindChild(roots[i].transform, name);
                if (found != null)
                {
                    return found.gameObject;
                }
            }

            return null;
        }

        private static Transform FindChild(Transform parent, string name)
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

                var nested = FindChild(child, name);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
#endif
