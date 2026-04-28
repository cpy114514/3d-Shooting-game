#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerBlock.Editor
{
    [InitializeOnLoad]
    public static class SettingsControlsPrefabSetup
    {
        private const string CompletionKey = "PlayerBlock.SettingsControlsPrefabSetup.V1";
        private const string SettingsPanelPrefabPath = "Assets/PlayerBlock/UI/SettingsPanel.prefab";

        static SettingsControlsPrefabSetup()
        {
            EditorApplication.delayCall += TryRunOnce;
        }

        [MenuItem("Tools/Block Player/Add Settings Sliders And Dropdowns")]
        private static void RunMenuItem()
        {
            Run(force: true);
        }

        private static void TryRunOnce()
        {
            if (EditorPrefs.GetBool(CompletionKey, false))
            {
                return;
            }

            Run(force: false);
        }

        private static void Run(bool force)
        {
            if (!force && EditorPrefs.GetBool(CompletionKey, false))
            {
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SettingsPanelPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"SettingsPanel prefab not found at {SettingsPanelPrefabPath}.");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(SettingsPanelPrefabPath);
            try
            {
                var changed = false;
                changed |= EnsureQualityDropdown(root.transform);
                changed |= EnsureSliderRow(root.transform, "MouseSensitivityButton", "MouseSensitivitySlider", "MouseSensitivityValueLabel", 0.03f, 0.3f, 0.14f);
                changed |= EnsureSliderRow(root.transform, "MasterVolumeButton", "MasterVolumeSlider", "MasterVolumeValueLabel", 0f, 1f, 1f);
                changed |= EnsureSliderRow(root.transform, "MusicVolumeButton", "MusicVolumeSlider", "MusicVolumeValueLabel", 0f, 1f, 0.85f);
                changed |= EnsureSliderRow(root.transform, "SfxVolumeButton", "SfxVolumeSlider", "SfxVolumeValueLabel", 0f, 1f, 1f);
                changed |= EnsureSliderRow(root.transform, "CameraDistanceButton", "CameraDistanceSlider", "CameraDistanceValueLabel", 0.75f, 1.5f, 1f);

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, SettingsPanelPrefabPath);
                    AssetDatabase.Refresh();
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            EditorPrefs.SetBool(CompletionKey, true);
        }

        private static bool EnsureQualityDropdown(Transform root)
        {
            var row = FindChild(root, "QualityButton");
            if (row == null || FindChild(row, "QualityDropdown") != null)
            {
                return false;
            }

            DisableRowButtonHitTarget(row);
            AdjustRowLabel(row, "QualityButtonLabel");
            var dropdownObject = CreateUiObject("QualityDropdown", row, typeof(Image), typeof(Dropdown));
            var rect = dropdownObject.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(170f, 0f), new Vector2(270f, 56f));

            var image = dropdownObject.GetComponent<Image>();
            image.color = new Color(0.08f, 0.09f, 0.12f, 1f);
            image.raycastTarget = true;

            var label = CreateText(dropdownObject.transform, "Label", "High", 28, TextAnchor.MiddleLeft);
            Stretch(label.rectTransform, new Vector2(18f, 0f), new Vector2(-54f, 0f));
            label.color = new Color(1f, 0.95f, 0.84f, 1f);

            var arrow = CreateText(dropdownObject.transform, "Arrow", "v", 30, TextAnchor.MiddleCenter);
            SetRect(arrow.rectTransform, new Vector2(112f, 0f), new Vector2(42f, 52f));
            arrow.color = new Color(0.72f, 0.79f, 0.9f, 1f);

            var template = CreateDropdownTemplate(dropdownObject.transform);
            var dropdown = dropdownObject.GetComponent<Dropdown>();
            dropdown.targetGraphic = image;
            dropdown.captionText = label;
            dropdown.itemText = FindChild(template, "Item Label")?.GetComponent<Text>();
            dropdown.template = template.GetComponent<RectTransform>();
            dropdown.options = new List<Dropdown.OptionData>
            {
                new Dropdown.OptionData("Low"),
                new Dropdown.OptionData("Medium"),
                new Dropdown.OptionData("High")
            };
            dropdown.value = 2;
            dropdown.RefreshShownValue();
            return true;
        }

        private static bool EnsureSliderRow(Transform root, string rowName, string sliderName, string valueLabelName, float min, float max, float value)
        {
            var row = FindChild(root, rowName);
            if (row == null || FindChild(row, sliderName) != null)
            {
                return false;
            }

            DisableRowButtonHitTarget(row);
            AdjustRowLabel(row, rowName + "Label");
            var sliderObject = CreateUiObject(sliderName, row, typeof(Slider));
            SetRect(sliderObject.GetComponent<RectTransform>(), new Vector2(170f, -10f), new Vector2(300f, 36f));

            var background = CreateUiObject("Background", sliderObject.transform, typeof(Image));
            Stretch(background.GetComponent<RectTransform>(), new Vector2(0f, 12f), new Vector2(0f, -12f));
            background.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.12f, 1f);

            var fillArea = CreateUiObject("Fill Area", sliderObject.transform);
            Stretch(fillArea.GetComponent<RectTransform>(), new Vector2(10f, 12f), new Vector2(-10f, -12f));

            var fill = CreateUiObject("Fill", fillArea.transform, typeof(Image));
            Stretch(fill.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            fill.GetComponent<Image>().color = new Color(0.42f, 0.72f, 1f, 1f);

            var handleArea = CreateUiObject("Handle Slide Area", sliderObject.transform);
            Stretch(handleArea.GetComponent<RectTransform>(), new Vector2(10f, 0f), new Vector2(-10f, 0f));

            var handle = CreateUiObject("Handle", handleArea.transform, typeof(Image));
            SetRect(handle.GetComponent<RectTransform>(), Vector2.zero, new Vector2(26f, 34f));
            handle.GetComponent<Image>().color = new Color(1f, 0.95f, 0.84f, 1f);

            var slider = sliderObject.GetComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.wholeNumbers = false;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handle.GetComponent<RectTransform>();

            var valueLabel = CreateText(row, valueLabelName, FormatValue(sliderName, value), 28, TextAnchor.MiddleRight);
            SetRect(valueLabel.rectTransform, new Vector2(360f, -10f), new Vector2(130f, 44f));
            valueLabel.color = new Color(0.72f, 0.79f, 0.9f, 1f);
            return true;
        }

        private static RectTransform CreateDropdownTemplate(Transform parent)
        {
            var template = CreateUiObject("Template", parent, typeof(Image), typeof(ScrollRect), typeof(CanvasGroup));
            var templateRect = template.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.anchoredPosition = new Vector2(0f, -4f);
            templateRect.sizeDelta = new Vector2(0f, 170f);
            template.SetActive(false);
            template.GetComponent<Image>().color = new Color(0.045f, 0.05f, 0.065f, 0.98f);

            var viewport = CreateUiObject("Viewport", template.transform, typeof(Image), typeof(Mask));
            Stretch(viewport.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            viewport.GetComponent<Image>().color = Color.white;
            viewport.GetComponent<Image>().raycastTarget = true;
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = CreateUiObject("Content", viewport.transform, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 168f);
            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var item = CreateUiObject("Item", content.transform, typeof(Toggle));
            var itemRect = item.GetComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(0f, 54f);
            var itemToggle = item.GetComponent<Toggle>();

            var background = CreateUiObject("Item Background", item.transform, typeof(Image));
            Stretch(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            background.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.12f, 1f);

            var checkmark = CreateText(item.transform, "Item Checkmark", ">", 28, TextAnchor.MiddleCenter);
            SetRect(checkmark.rectTransform, new Vector2(-110f, 0f), new Vector2(36f, 44f));
            checkmark.color = new Color(0.42f, 0.72f, 1f, 1f);

            var label = CreateText(item.transform, "Item Label", "Option", 26, TextAnchor.MiddleLeft);
            Stretch(label.rectTransform, new Vector2(44f, 0f), new Vector2(-14f, 0f));
            label.color = new Color(1f, 0.95f, 0.84f, 1f);

            itemToggle.targetGraphic = background.GetComponent<Image>();
            itemToggle.graphic = checkmark;

            var scroll = template.GetComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.horizontal = false;
            scroll.vertical = true;
            return templateRect;
        }

        private static void DisableRowButtonHitTarget(Transform row)
        {
            var button = row.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = false;
            }

            var image = row.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = false;
            }
        }

        private static void AdjustRowLabel(Transform row, string labelName)
        {
            var label = FindChild(row, labelName)?.GetComponent<Text>();
            if (label == null)
            {
                return;
            }

            label.alignment = TextAnchor.MiddleLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.fontSize = Mathf.Min(label.fontSize, 34);
            var rect = label.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(24f, 0f);
            rect.sizeDelta = new Vector2(300f, 0f);
        }

        private static GameObject CreateUiObject(string name, Transform parent, params System.Type[] components)
        {
            var allComponents = new List<System.Type> { typeof(RectTransform), typeof(CanvasRenderer) };
            allComponents.AddRange(components);
            var gameObject = new GameObject(name, allComponents.ToArray());
            gameObject.layer = 5;
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static Text CreateText(Transform parent, string name, string value, int fontSize, TextAnchor alignment)
        {
            var textObject = CreateUiObject(name, parent, typeof(Text));
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

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static string FormatValue(string sliderName, float value)
        {
            if (sliderName.Contains("Volume"))
            {
                return Mathf.RoundToInt(value * 100f) + "%";
            }

            return sliderName.Contains("CameraDistance") ? value.ToString("0.00") + "x" : value.ToString("0.00");
        }
    }
}
#endif
