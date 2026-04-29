using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PlayerBlock
{
    [DisallowMultipleComponent]
    public sealed class LevelSelectMenu : MonoBehaviour
    {
        [Serializable]
        public sealed class LevelEntry
        {
            public string DisplayName = "LEVEL";
            public string SceneName = "Maingame1";
            public bool IsEndlessMode;
            public Button Button;
            public Text Label;
        }

        [Header("Scene UI")]
        [SerializeField] private GameObject levelSelectPanel;
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button unlockAllButton;
        [SerializeField] private Button backButton;
        [SerializeField] private LevelEntry[] levels =
        {
            new LevelEntry { DisplayName = "LEVEL 1", SceneName = "Maingame1" },
            new LevelEntry { DisplayName = "LEVEL 2", SceneName = "Maingame2" },
            new LevelEntry { DisplayName = "LEVEL 3", SceneName = "Maingame3" },
            new LevelEntry { DisplayName = "LEVEL 4", SceneName = "Maingame4" },
            new LevelEntry { DisplayName = "BOSS", SceneName = "Boss" },
            new LevelEntry { DisplayName = "ENDLESS", SceneName = EndlessModeDirector.SceneName, IsEndlessMode = true }
        };

        private UiPanelAnimator _panelAnimator;

        private void Awake()
        {
            AutoBindMissingReferences();
            BindButtons();
            RefreshLevelButtons();
            SetPanelVisible(false, instant: true);
        }

        public void OpenPanel()
        {
            RefreshLevelButtons();
            SetPanelVisible(true, instant: false);
        }

        public void ClosePanel()
        {
            SetPanelVisible(false, instant: false);
        }

        public void UnlockAllLevels()
        {
            LevelProgress.UnlockAll(levels != null ? levels.Length : 1);
            RefreshLevelButtons();
        }

        private void LoadLevel(int index)
        {
            if (levels == null || index < 0 || index >= levels.Length)
            {
                return;
            }

            var levelNumber = index + 1;
            if (!levels[index].IsEndlessMode && !LevelProgress.IsUnlocked(levelNumber))
            {
                RefreshLevelButtons();
                return;
            }

            var sceneName = levels[index].SceneName;
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning($"Level {levelNumber} has no scene name.");
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        private void BindButtons()
        {
            if (startGameButton != null)
            {
                var directLoader = startGameButton.GetComponent<LoadSceneButton>();
                if (directLoader != null)
                {
                    directLoader.enabled = false;
                }

                startGameButton.onClick.RemoveAllListeners();
                startGameButton.onClick.AddListener(OpenPanel);
                UiEffectsUtility.EnsureButtonEffects(startGameButton.transform);
            }

            if (unlockAllButton != null)
            {
                unlockAllButton.onClick.RemoveAllListeners();
                unlockAllButton.onClick.AddListener(UnlockAllLevels);
                UiEffectsUtility.EnsureButtonEffects(unlockAllButton.transform);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(ClosePanel);
                UiEffectsUtility.EnsureButtonEffects(backButton.transform);
            }

            if (levels == null)
            {
                return;
            }

            for (var i = 0; i < levels.Length; i++)
            {
                var index = i;
                var entry = levels[i];
                if (entry?.Button == null)
                {
                    continue;
                }

                entry.Button.onClick.RemoveAllListeners();
                entry.Button.onClick.AddListener(() => LoadLevel(index));
                UiEffectsUtility.EnsureButtonEffects(entry.Button.transform);
            }
        }

        private void RefreshLevelButtons()
        {
            if (levels == null)
            {
                return;
            }

            for (var i = 0; i < levels.Length; i++)
            {
                var entry = levels[i];
                if (entry == null)
                {
                    continue;
                }

                var unlocked = entry.IsEndlessMode || LevelProgress.IsUnlocked(i + 1);
                if (entry.Button != null)
                {
                    entry.Button.interactable = unlocked;
                }

                if (entry.Label != null)
                {
                    entry.Label.text = unlocked ? entry.DisplayName : $"{entry.DisplayName}  LOCKED";
                    entry.Label.color = unlocked
                        ? new Color(1f, 0.95f, 0.84f, 1f)
                        : new Color(0.5f, 0.52f, 0.56f, 1f);
                }
            }
        }

        private void SetPanelVisible(bool visible, bool instant)
        {
            if (levelSelectPanel == null)
            {
                return;
            }

            if (_panelAnimator == null)
            {
                _panelAnimator = levelSelectPanel.GetComponent<UiPanelAnimator>();
            }

            if (_panelAnimator != null)
            {
                if (visible)
                {
                    _panelAnimator.Show(instant);
                }
                else
                {
                    _panelAnimator.Hide(instant);
                }

                return;
            }

            levelSelectPanel.SetActive(visible);
        }

        private void AutoBindMissingReferences()
        {
            if (levelSelectPanel == null)
            {
                levelSelectPanel = FindChild("LevelSelectPanel")?.gameObject;
            }

            if (startGameButton == null)
            {
                startGameButton = FindChild("Button")?.GetComponent<Button>();
            }

            if (unlockAllButton == null)
            {
                unlockAllButton = FindChild("UnlockAllButton")?.GetComponent<Button>();
            }

            if (backButton == null)
            {
                backButton = FindChild("LevelSelectBackButton")?.GetComponent<Button>();
            }

            if (levels == null || levels.Length == 0)
            {
                return;
            }

            for (var i = 0; i < levels.Length; i++)
            {
                var entry = levels[i];
                if (entry == null)
                {
                    continue;
                }

                var levelButton = FindChild($"LevelButton{i + 1}");
                if (entry.Button == null)
                {
                    entry.Button = levelButton != null ? levelButton.GetComponent<Button>() : null;
                }

                if (entry.Label == null)
                {
                    entry.Label = FindChild($"LevelButton{i + 1}Label")?.GetComponent<Text>();
                }
            }
        }

        private Transform FindChild(string childName)
        {
            return FindChildRecursive(transform, childName);
        }

        private static Transform FindChildRecursive(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == childName)
                {
                    return child;
                }

                var nested = FindChildRecursive(child, childName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }
    }
}
