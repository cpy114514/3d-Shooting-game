using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PlayerBlock
{
    public sealed class BrowserPauseMenu : MonoBehaviour
    {
        private GameObject _pausePanel;
        private UiPanelAnimator _pauseAnimator;
        private BrowserSettingsMenu _settingsMenu;

        public static bool IsPaused { get; private set; }

        private void Awake()
        {
            _pausePanel = FindChildRecursive(transform, "PausePanel")?.gameObject;
            _pauseAnimator = UiEffectsUtility.EnsurePauseAnimator(_pausePanel);
            _settingsMenu = GetComponent<BrowserSettingsMenu>();
            if (_settingsMenu != null)
            {
                _settingsMenu.Closed += HandleSettingsClosed;
            }

            UiEffectsUtility.EnsureSceneButtonEffects();
            if (_pausePanel != null)
            {
                UiEffectsUtility.EnsureButtonEffects(_pausePanel.transform);
            }
            SetPausePanelVisible(false);
            BindButtons();
        }

        private void OnDestroy()
        {
            if (_settingsMenu != null)
            {
                _settingsMenu.Closed -= HandleSettingsClosed;
            }
        }

        private void Update()
        {
            if (!EscapePressed())
            {
                return;
            }

            if (_settingsMenu != null && _settingsMenu.IsOpen)
            {
                _settingsMenu.ClosePanel();
                return;
            }

            if (IsPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

        private void BindButtons()
        {
            Bind("ResumeButton", ResumeGame);
            Bind("PauseSettingsButton", OpenSettingsFromPause);
            Bind("PauseMainMenuButton", ReturnToMainMenuFromPause);
        }

        private static void Bind(string buttonObjectName, UnityEngine.Events.UnityAction action)
        {
            var buttonObject = FindSceneObject(buttonObjectName);
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

        private void PauseGame()
        {
            IsPaused = true;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SetPausePanelVisible(true);
        }

        private void ResumeGame()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            SetPausePanelVisible(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OpenSettingsFromPause()
        {
            SetPausePanelVisible(false);
            if (_settingsMenu != null)
            {
                _settingsMenu.OpenPanel();
            }
        }

        private void ReturnToMainMenuFromPause()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SetPausePanelVisible(false);

            var hud = CombatHud.Instance;
            if (hud != null)
            {
                hud.ReturnToMainMenu();
            }
        }

        private void HandleSettingsClosed()
        {
            if (IsPaused)
            {
                SetPausePanelVisible(true);
            }
        }

        private void SetPausePanelVisible(bool visible)
        {
            if (_pausePanel != null)
            {
                if (_pauseAnimator != null)
                {
                    if (visible)
                    {
                        _pauseAnimator.Show();
                    }
                    else
                    {
                        _pauseAnimator.Hide();
                    }
                }
                else
                {
                    _pausePanel.SetActive(visible);
                }
            }
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

        private static GameObject FindSceneObject(string name)
        {
            var sceneRoots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            for (var i = 0; i < sceneRoots.Length; i++)
            {
                var root = sceneRoots[i];
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

        private static bool EscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }
    }
}
