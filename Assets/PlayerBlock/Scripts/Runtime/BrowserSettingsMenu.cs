using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PlayerBlock
{
    public sealed class BrowserSettingsMenu : MonoBehaviour
    {
        private GameObject _settingsPanel;
        private Transform _settingsRoot;
        private UiPanelAnimator _settingsAnimator;
        private bool _syncingControls;

        public event Action Closed;

        public bool IsOpen => _settingsPanel != null && _settingsPanel.activeSelf;

        private void Awake()
        {
            BrowserGameSettings.Changed += SyncUi;
            CacheSceneReferences();
            UiEffectsUtility.EnsureSceneButtonEffects();
            BindButtons();
            SyncUi();
            if (_settingsPanel != null)
            {
                if (_settingsAnimator != null)
                {
                    _settingsAnimator.Hide(true);
                }
                else
                {
                    _settingsPanel.SetActive(false);
                }
            }
        }

        private void OnDestroy()
        {
            BrowserGameSettings.Changed -= SyncUi;
        }

        public void OpenPanel()
        {
            CacheSceneReferences();
            UiEffectsUtility.EnsureSceneButtonEffects();
            BindButtons();
            SyncUi();
            if (_settingsPanel != null)
            {
                if (_settingsAnimator != null)
                {
                    _settingsAnimator.Show();
                }
                else
                {
                    _settingsPanel.SetActive(true);
                }
            }
        }

        public void ClosePanel()
        {
            if (_settingsPanel != null)
            {
                if (_settingsAnimator != null)
                {
                    _settingsAnimator.Hide();
                }
                else
                {
                    _settingsPanel.SetActive(false);
                }
            }

            Closed?.Invoke();
        }

        private void CacheSceneReferences()
        {
            _settingsPanel = FindLocalObject("SettingsPanel");
            if (_settingsPanel == null)
            {
                _settingsPanel = FindSceneObject("SettingsPanel");
            }

            _settingsRoot = _settingsPanel != null ? _settingsPanel.transform : null;
            _settingsAnimator = UiEffectsUtility.EnsureSettingsAnimator(_settingsPanel);
            if (_settingsRoot != null)
            {
                UiEffectsUtility.EnsureButtonEffects(_settingsRoot);
            }
        }

        private void BindButtons()
        {
            BindDropdown("QualityDropdown", (value) => BrowserGameSettings.SetGraphicsQuality((BrowserGameSettings.GraphicsQualityLevel)value));
            BindSlider("MouseSensitivitySlider", BrowserGameSettings.SetMouseSensitivity);
            BindSlider("MasterVolumeSlider", BrowserGameSettings.SetMasterVolume);
            BindSlider("MusicVolumeSlider", BrowserGameSettings.SetMusicVolume);
            BindSlider("SfxVolumeSlider", BrowserGameSettings.SetSfxVolume);
            BindSlider("CameraDistanceSlider", BrowserGameSettings.SetCameraDistance);

            if (FindSettingsObject("QualityDropdown") == null)
            {
                Bind("QualityButton", () => BrowserGameSettings.CycleGraphicsQuality(1));
            }

            Bind("ShadowsButton", BrowserGameSettings.ToggleShadows);
            Bind("ShowFpsButton", BrowserGameSettings.ToggleShowFps);
            if (FindSettingsObject("MouseSensitivitySlider") == null)
            {
                Bind("MouseSensitivityButton", () => BrowserGameSettings.SetMouseSensitivity(BrowserGameSettings.MouseSensitivity + 0.01f));
            }

            Bind("InvertYAxisButton", BrowserGameSettings.ToggleInvertYAxis);
            if (FindSettingsObject("MasterVolumeSlider") == null)
            {
                Bind("MasterVolumeButton", () => BrowserGameSettings.SetMasterVolume(BrowserGameSettings.MasterVolume + 0.05f));
            }

            if (FindSettingsObject("MusicVolumeSlider") == null)
            {
                Bind("MusicVolumeButton", () => BrowserGameSettings.SetMusicVolume(BrowserGameSettings.MusicVolume + 0.05f));
            }

            if (FindSettingsObject("SfxVolumeSlider") == null)
            {
                Bind("SfxVolumeButton", () => BrowserGameSettings.SetSfxVolume(BrowserGameSettings.SfxVolume + 0.05f));
            }

            if (FindSettingsObject("CameraDistanceSlider") == null)
            {
                Bind("CameraDistanceButton", () => BrowserGameSettings.SetCameraDistance(BrowserGameSettings.CameraDistance + 0.05f));
            }

            Bind("ScreenShakeButton", BrowserGameSettings.ToggleScreenShake);
            Bind("ResetButton", BrowserGameSettings.ResetDefaults);
            Bind("BackButton", ClosePanel);
        }

        private void BindDropdown(string objectName, UnityEngine.Events.UnityAction<int> action)
        {
            var dropdownObject = FindSettingsObject(objectName);
            var dropdown = dropdownObject != null ? dropdownObject.GetComponent<Dropdown>() : null;
            if (dropdown == null || action == null)
            {
                return;
            }

            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.onValueChanged.AddListener(value =>
            {
                if (_syncingControls)
                {
                    return;
                }

                action.Invoke(value);
            });
        }

        private void BindSlider(string objectName, UnityEngine.Events.UnityAction<float> action)
        {
            var sliderObject = FindSettingsObject(objectName);
            var slider = sliderObject != null ? sliderObject.GetComponent<Slider>() : null;
            if (slider == null || action == null)
            {
                return;
            }

            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(value =>
            {
                if (_syncingControls)
                {
                    return;
                }

                action.Invoke(value);
            });
        }

        private void Bind(string buttonObjectName, UnityEngine.Events.UnityAction action)
        {
            var buttonObject = FindSettingsObject(buttonObjectName);
            if (buttonObject == null || action == null)
            {
                return;
            }

            var button = buttonObject.GetComponent<Button>();
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private void SyncUi()
        {
            _syncingControls = true;
            SetDropdown("QualityDropdown", (int)BrowserGameSettings.GraphicsQuality);
            SetSlider("MouseSensitivitySlider", BrowserGameSettings.MouseSensitivity);
            SetSlider("MasterVolumeSlider", BrowserGameSettings.MasterVolume);
            SetSlider("MusicVolumeSlider", BrowserGameSettings.MusicVolume);
            SetSlider("SfxVolumeSlider", BrowserGameSettings.SfxVolume);
            SetSlider("CameraDistanceSlider", BrowserGameSettings.CameraDistance);
            _syncingControls = false;

            SetText("QualityButtonLabel", FindSettingsObject("QualityDropdown") != null
                ? "QUALITY"
                : "QUALITY  " + BrowserGameSettings.GraphicsQuality.ToString().ToUpperInvariant());
            SetText("ShadowsButtonLabel", "SHADOWS  " + (BrowserGameSettings.ShadowsEnabled ? "ON" : "OFF"));
            SetText("ShowFpsButtonLabel", "SHOW FPS  " + (BrowserGameSettings.ShowFps ? "ON" : "OFF"));
            SetText("MouseSensitivityButtonLabel", FindSettingsObject("MouseSensitivitySlider") != null
                ? "MOUSE SENSITIVITY"
                : "MOUSE SENSITIVITY  " + BrowserGameSettings.MouseSensitivity.ToString("0.00"));
            SetText("MouseSensitivityValueLabel", BrowserGameSettings.MouseSensitivity.ToString("0.00"));
            SetText("InvertYAxisButtonLabel", "INVERT Y AXIS  " + (BrowserGameSettings.InvertYAxis ? "ON" : "OFF"));
            SetText("MasterVolumeButtonLabel", FindSettingsObject("MasterVolumeSlider") != null
                ? "MASTER VOLUME"
                : "MASTER VOLUME  " + Mathf.RoundToInt(BrowserGameSettings.MasterVolume * 100f) + "%");
            SetText("MasterVolumeValueLabel", Mathf.RoundToInt(BrowserGameSettings.MasterVolume * 100f) + "%");
            SetText("MusicVolumeButtonLabel", FindSettingsObject("MusicVolumeSlider") != null
                ? "MUSIC VOLUME"
                : "MUSIC VOLUME  " + Mathf.RoundToInt(BrowserGameSettings.MusicVolume * 100f) + "%");
            SetText("MusicVolumeValueLabel", Mathf.RoundToInt(BrowserGameSettings.MusicVolume * 100f) + "%");
            SetText("SfxVolumeButtonLabel", FindSettingsObject("SfxVolumeSlider") != null
                ? "SFX VOLUME"
                : "SFX VOLUME  " + Mathf.RoundToInt(BrowserGameSettings.SfxVolume * 100f) + "%");
            SetText("SfxVolumeValueLabel", Mathf.RoundToInt(BrowserGameSettings.SfxVolume * 100f) + "%");
            SetText("CameraDistanceButtonLabel", FindSettingsObject("CameraDistanceSlider") != null
                ? "CAMERA DISTANCE"
                : "CAMERA DISTANCE  " + BrowserGameSettings.CameraDistance.ToString("0.00") + "x");
            SetText("CameraDistanceValueLabel", BrowserGameSettings.CameraDistance.ToString("0.00") + "x");
            SetText("ScreenShakeButtonLabel", "SCREEN SHAKE  " + (BrowserGameSettings.ScreenShakeEnabled ? "ON" : "OFF"));
        }

        private void SetDropdown(string objectName, int value)
        {
            var dropdownObject = FindSettingsObject(objectName);
            var dropdown = dropdownObject != null ? dropdownObject.GetComponent<Dropdown>() : null;
            if (dropdown == null)
            {
                return;
            }

            if (dropdown.options == null || dropdown.options.Count == 0)
            {
                return;
            }

            dropdown.SetValueWithoutNotify(Mathf.Clamp(value, 0, dropdown.options.Count - 1));
            dropdown.RefreshShownValue();
        }

        private void SetSlider(string objectName, float value)
        {
            var sliderObject = FindSettingsObject(objectName);
            var slider = sliderObject != null ? sliderObject.GetComponent<Slider>() : null;
            if (slider == null)
            {
                return;
            }

            slider.SetValueWithoutNotify(Mathf.Clamp(value, slider.minValue, slider.maxValue));
        }

        private void SetText(string objectName, string value)
        {
            var textObject = FindSettingsObject(objectName);
            if (textObject == null)
            {
                return;
            }

            var tmp = textObject.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.text = value;
                return;
            }

            var text = textObject.GetComponent<Text>();
            if (text != null)
            {
                text.text = value;
            }
        }

        private GameObject FindSettingsObject(string name)
        {
            if (_settingsRoot != null)
            {
                if (_settingsRoot.name == name)
                {
                    return _settingsRoot.gameObject;
                }

                var nested = FindChildRecursive(_settingsRoot, name);
                if (nested != null)
                {
                    return nested.gameObject;
                }
            }

            return FindSceneObject(name);
        }

        private GameObject FindLocalObject(string name)
        {
            if (transform.name == name)
            {
                return gameObject;
            }

            var nested = FindChildRecursive(transform, name);
            return nested != null ? nested.gameObject : null;
        }

        private static GameObject FindSceneObject(string name)
        {
            var scene = SceneManager.GetActiveScene();
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
