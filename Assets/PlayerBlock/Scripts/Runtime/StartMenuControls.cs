using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PlayerBlock
{
    [DisallowMultipleComponent]
    public sealed class StartMenuControls : MonoBehaviour
    {
        [SerializeField] private Button exitButton;

        private void Awake()
        {
            BindExitButton();
        }

        private void Update()
        {
            if (FullscreenTogglePressed())
            {
                ToggleFullscreen();
            }
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void BindExitButton()
        {
            if (exitButton == null)
            {
                exitButton = FindSceneObject("ExitButton")?.GetComponent<Button>();
            }

            if (exitButton == null)
            {
                return;
            }

            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(QuitGame);
            UiEffectsUtility.EnsureButtonEffects(exitButton.transform);
        }

        private static bool FullscreenTogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.f11Key.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.F11);
#endif
        }

        private static void ToggleFullscreen()
        {
            var nextMode = Screen.fullScreenMode == FullScreenMode.Windowed
                ? FullScreenMode.FullScreenWindow
                : FullScreenMode.Windowed;

            Screen.fullScreenMode = nextMode;
            Screen.fullScreen = nextMode != FullScreenMode.Windowed;
        }

        private static GameObject FindSceneObject(string name)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
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
