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
            Bind("QualityButton", () => BrowserGameSettings.CycleGraphicsQuality(1));
            Bind("ShadowsButton", BrowserGameSettings.ToggleShadows);
            Bind("ShowFpsButton", BrowserGameSettings.ToggleShowFps);
            Bind("MouseSensitivityButton", () => BrowserGameSettings.SetMouseSensitivity(BrowserGameSettings.MouseSensitivity + 0.01f));
            Bind("InvertYAxisButton", BrowserGameSettings.ToggleInvertYAxis);
            Bind("MasterVolumeButton", () => BrowserGameSettings.SetMasterVolume(BrowserGameSettings.MasterVolume + 0.05f));
            Bind("MusicVolumeButton", () => BrowserGameSettings.SetMusicVolume(BrowserGameSettings.MusicVolume + 0.05f));
            Bind("SfxVolumeButton", () => BrowserGameSettings.SetSfxVolume(BrowserGameSettings.SfxVolume + 0.05f));
            Bind("CameraDistanceButton", () => BrowserGameSettings.SetCameraDistance(BrowserGameSettings.CameraDistance + 0.05f));
            Bind("ScreenShakeButton", BrowserGameSettings.ToggleScreenShake);
            Bind("ResetButton", BrowserGameSettings.ResetDefaults);
            Bind("BackButton", ClosePanel);
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
            SetText("QualityButtonLabel", "QUALITY  " + BrowserGameSettings.GraphicsQuality.ToString().ToUpperInvariant());
            SetText("ShadowsButtonLabel", "SHADOWS  " + (BrowserGameSettings.ShadowsEnabled ? "ON" : "OFF"));
            SetText("ShowFpsButtonLabel", "SHOW FPS  " + (BrowserGameSettings.ShowFps ? "ON" : "OFF"));
            SetText("MouseSensitivityButtonLabel", "MOUSE SENSITIVITY  " + BrowserGameSettings.MouseSensitivity.ToString("0.00"));
            SetText("InvertYAxisButtonLabel", "INVERT Y AXIS  " + (BrowserGameSettings.InvertYAxis ? "ON" : "OFF"));
            SetText("MasterVolumeButtonLabel", "MASTER VOLUME  " + Mathf.RoundToInt(BrowserGameSettings.MasterVolume * 100f) + "%");
            SetText("MusicVolumeButtonLabel", "MUSIC VOLUME  " + Mathf.RoundToInt(BrowserGameSettings.MusicVolume * 100f) + "%");
            SetText("SfxVolumeButtonLabel", "SFX VOLUME  " + Mathf.RoundToInt(BrowserGameSettings.SfxVolume * 100f) + "%");
            SetText("CameraDistanceButtonLabel", "CAMERA DISTANCE  " + BrowserGameSettings.CameraDistance.ToString("0.00") + "x");
            SetText("ScreenShakeButtonLabel", "SCREEN SHAKE  " + (BrowserGameSettings.ScreenShakeEnabled ? "ON" : "OFF"));
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
