#if UNITY_EDITOR
using System.Collections.Generic;
using PlayerBlock;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PlayerBlock.Editor
{
    [InitializeOnLoad]
    public static class TutorialSceneSetup
    {
        private const string CompletionKey = "LanShooter.TutorialSceneSetup.V1";

        static TutorialSceneSetup()
        {
            EditorApplication.delayCall += TryRunOnce;
        }

        [MenuItem("Tools/Block Player/Add Tutorial UI To Scenes")]
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
                var changed = EnsureTutorialUi(scene);
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
                Debug.Log($"Added tutorial UI to {changedScenes.Count} scene(s): {string.Join(", ", changedScenes)}");
                AssetDatabase.Refresh();
            }
        }

        private static bool EnsureTutorialUi(Scene scene)
        {
            var changed = false;
            var isStartScene = scene.name == "start";
            var needsGameplayPanel = scene.name != "start";

            if (isStartScene)
            {
                changed |= EnsureTutorialButton(scene);
            }

            if (needsGameplayPanel || isStartScene)
            {
                changed |= EnsureTutorialPanel(scene, isStartScene);
            }

            return changed;
        }

        private static bool EnsureTutorialButton(Scene scene)
        {
            if (FindSceneObject(scene, "TutorialButton") != null)
            {
                return false;
            }

            var parent = FindSceneObject(scene, "Panel")?.transform;
            if (parent == null)
            {
                parent = FindFirstCanvas(scene)?.transform;
            }

            if (parent == null)
            {
                return false;
            }

            var buttonObject = new GameObject("TutorialButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(UiButtonFeedback));
            Undo.RegisterCreatedObjectUndo(buttonObject, "Create Tutorial Button");
            buttonObject.layer = 5;
            buttonObject.transform.SetParent(parent, false);

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-150f, -430f);
            rect.sizeDelta = new Vector2(500f, 180f);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.18f, 0.18f, 0.2f, 0.9f);
            image.raycastTarget = true;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            var label = CreateText(buttonObject.transform, "TutorialButtonLabel", "TUTORIAL", 76, TextAnchor.MiddleCenter);
            StretchText(label.rectTransform);
            label.color = new Color(1f, 0.95f, 0.84f, 1f);

            EditorUtility.SetDirty(buttonObject);
            return true;
        }

        private static bool EnsureTutorialPanel(Scene scene, bool gameplayStyle)
        {
            if (FindSceneObject(scene, "TutorialPanel") != null)
            {
                return false;
            }

            var parent = gameplayStyle
                ? FindSceneObject(scene, "PlayerBlockCombatHud")?.transform
                : FindFirstCanvas(scene)?.transform;

            if (parent == null)
            {
                return false;
            }

            var panelObject = new GameObject("TutorialPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(UiPanelAnimator));
            Undo.RegisterCreatedObjectUndo(panelObject, "Create Tutorial Panel");
            panelObject.layer = 5;
            panelObject.transform.SetParent(parent, false);

            var rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = gameplayStyle ? new Vector2(1180f, 700f) : new Vector2(1280f, 760f);

            var image = panelObject.GetComponent<Image>();
            image.color = new Color(0.04f, 0.05f, 0.07f, 0.95f);
            image.raycastTarget = true;

            var title = CreateText(panelObject.transform, "TutorialTitleLabel", "TUTORIAL", 86, TextAnchor.MiddleCenter);
            SetStretchText(title.rectTransform, new Vector2(80f, 64f), new Vector2(-80f, -560f));
            title.color = new Color(1f, 0.93f, 0.78f, 1f);

            var body = CreateText(panelObject.transform, "TutorialBodyLabel", string.Empty, 54, TextAnchor.UpperCenter);
            SetStretchText(body.rectTransform, new Vector2(110f, 180f), new Vector2(-110f, -220f));
            body.color = new Color(0.9f, 0.94f, 1f, 1f);

            var index = CreateText(panelObject.transform, "TutorialIndexLabel", string.Empty, 36, TextAnchor.MiddleCenter);
            SetRect(index.rectTransform, new Vector2(0f, -210f), new Vector2(320f, 46f));
            index.color = new Color(0.72f, 0.79f, 0.9f, 1f);

            CreateTutorialButton(panelObject.transform, "TutorialBackButton", new Vector2(-240f, -292f), "BACK");
            CreateTutorialButton(panelObject.transform, "TutorialNextButton", new Vector2(240f, -292f), "NEXT");
            CreateTutorialButton(panelObject.transform, "TutorialCloseButton", new Vector2(0f, -380f), gameplayStyle ? "SKIP" : "CLOSE");

            var animator = panelObject.GetComponent<UiPanelAnimator>();
            animator.Configure(-36f, 0.94f, 6f, 0.006f, 0.18f, 0.12f);
            animator.Hide(true);

            EditorUtility.SetDirty(panelObject);
            return true;
        }

        private static Button CreateTutorialButton(Transform parent, string name, Vector2 anchoredPosition, string text)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(UiButtonFeedback));
            buttonObject.layer = 5;
            buttonObject.transform.SetParent(parent, false);

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(300f, 92f);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.16f, 0.18f, 0.22f, 0.98f);
            image.raycastTarget = true;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            var label = CreateText(buttonObject.transform, name + "Label", text, 38, TextAnchor.MiddleCenter);
            StretchText(label.rectTransform);
            label.color = new Color(1f, 0.95f, 0.84f, 1f);

            return button;
        }

        private static Text CreateText(Transform parent, string name, string value, int fontSize, TextAnchor alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.layer = 5;
            textObject.transform.SetParent(parent, false);

            var label = textObject.GetComponent<Text>();
            label.text = value;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        private static void StretchText(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetStretchText(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
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
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root == null)
                {
                    continue;
                }

                if (root.name == name)
                {
                    return root;
                }

                var found = FindChildRecursive(root.transform, name);
                if (found != null)
                {
                    return found.gameObject;
                }
            }

            return null;
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
