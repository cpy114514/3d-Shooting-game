using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PlayerBlock
{
    public sealed class TutorialDirector : MonoBehaviour
    {
        private const string SeenTutorialKey = "LanShooter.Tutorial.Seen";
        private const string DirectorName = "LanShooterTutorialDirector";
        private const string TutorialPanelName = "TutorialPanel";
        private const string TutorialButtonName = "TutorialButton";

        private static readonly TutorialPage[] Pages =
        {
            new TutorialPage("MOVE", "WASD TO MOVE\nSPACE TO JUMP\nSHIFT TO DASH"),
            new TutorialPage("CAMERA", "MOVE THE MOUSE TO LOOK AROUND\nRIGHT CLICK TO AIM\nLEFT CLICK TO CAST A SHADOW BULLET"),
            new TutorialPage("SHADOWS", "SCROLL OR PRESS 1 2 3 4 TO SWITCH LOADOUT\nMELEE COSTS 1 ENERGY\nRANGED AND SHIELD COST 2 ENERGY"),
            new TutorialPage("FIGHT", "SHADOW BULLETS PLACE SHADOWS\nWATCH YOUR HEALTH AND ENERGY\nUSE ESC TO PAUSE"),
            new TutorialPage("CLEAR", "DEFEAT ENEMIES TO UNLOCK THE SEAL\nMOVE TO THE SEAL\nPRESS E TO CLEAR THE STAGE")
        };

        private GameObject _tutorialPanel;
        private Text _titleLabel;
        private Text _bodyLabel;
        private Text _indexLabel;
        private Text _nextLabel;
        private Text _backLabel;
        private Text _closeLabel;
        private Button _nextButton;
        private Button _backButton;
        private Button _closeButton;
        private UiPanelAnimator _panelAnimator;
        private int _pageIndex;
        private bool _openedFromGameplay;
        private bool _markSeenOnClose;
        private bool _initialSceneHandled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<TutorialDirector>() != null)
            {
                return;
            }

            var root = new GameObject(DirectorName);
            DontDestroyOnLoad(root);
            root.AddComponent<TutorialDirector>();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start()
        {
            if (_initialSceneHandled)
            {
                return;
            }

            HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            _initialSceneHandled = true;
            StopAllCoroutines();
            CacheSceneUi(scene);

            if (IsStartScene(scene))
            {
                BindMenuTutorialButton();
                BindPanelButtons();
                HidePanelInstant();
                return;
            }

            if (HasGameplayPlayer())
            {
                BindPanelButtons();
                HidePanelInstant();
                StartCoroutine(ShowTutorialIfFirstTime());
            }
        }

        private IEnumerator ShowTutorialIfFirstTime()
        {
            yield return null;

            if (!HasSeenTutorial())
            {
                OpenTutorial(fromGameplay: true, markSeenOnClose: true);
            }
        }

        private void CacheSceneUi(Scene scene)
        {
            _tutorialPanel = FindSceneObject(scene, TutorialPanelName);
            _panelAnimator = _tutorialPanel != null ? _tutorialPanel.GetComponent<UiPanelAnimator>() : null;

            if (_tutorialPanel != null)
            {
                _titleLabel = FindChildRecursive(_tutorialPanel.transform, "TutorialTitleLabel")?.GetComponent<Text>();
                _bodyLabel = FindChildRecursive(_tutorialPanel.transform, "TutorialBodyLabel")?.GetComponent<Text>();
                _indexLabel = FindChildRecursive(_tutorialPanel.transform, "TutorialIndexLabel")?.GetComponent<Text>();
                _backButton = FindChildRecursive(_tutorialPanel.transform, "TutorialBackButton")?.GetComponent<Button>();
                _nextButton = FindChildRecursive(_tutorialPanel.transform, "TutorialNextButton")?.GetComponent<Button>();
                _closeButton = FindChildRecursive(_tutorialPanel.transform, "TutorialCloseButton")?.GetComponent<Button>();
                _backLabel = FindChildRecursive(_tutorialPanel.transform, "TutorialBackButtonLabel")?.GetComponent<Text>();
                _nextLabel = FindChildRecursive(_tutorialPanel.transform, "TutorialNextButtonLabel")?.GetComponent<Text>();
                _closeLabel = FindChildRecursive(_tutorialPanel.transform, "TutorialCloseButtonLabel")?.GetComponent<Text>();
            }
        }

        private void BindMenuTutorialButton()
        {
            var buttonObject = FindSceneObject(SceneManager.GetActiveScene(), TutorialButtonName);
            if (buttonObject == null)
            {
                return;
            }

            var button = buttonObject.GetComponent<Button>();
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OpenTutorial(fromGameplay: false, markSeenOnClose: false));
        }

        private void BindPanelButtons()
        {
            if (_backButton != null)
            {
                _backButton.onClick.RemoveAllListeners();
                _backButton.onClick.AddListener(GoPrevious);
            }

            if (_nextButton != null)
            {
                _nextButton.onClick.RemoveAllListeners();
                _nextButton.onClick.AddListener(GoNext);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(CloseTutorial);
            }
        }

        private void OpenTutorial(bool fromGameplay, bool markSeenOnClose)
        {
            if (_tutorialPanel == null)
            {
                return;
            }

            _openedFromGameplay = fromGameplay;
            _markSeenOnClose = markSeenOnClose;
            _pageIndex = 0;
            RefreshPage();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SetPanelVisible(true);

            if (_openedFromGameplay)
            {
                Time.timeScale = 0f;
            }
        }

        private void CloseTutorial()
        {
            SetPanelVisible(false);
            if (_markSeenOnClose)
            {
                SetTutorialSeen();
            }

            if (_openedFromGameplay)
            {
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void GoPrevious()
        {
            _pageIndex = Mathf.Max(0, _pageIndex - 1);
            RefreshPage();
        }

        private void GoNext()
        {
            if (_pageIndex >= Pages.Length - 1)
            {
                CloseTutorial();
                return;
            }

            _pageIndex = Mathf.Min(Pages.Length - 1, _pageIndex + 1);
            RefreshPage();
        }

        private void RefreshPage()
        {
            if (_titleLabel == null || _bodyLabel == null || _indexLabel == null)
            {
                return;
            }

            var page = Pages[_pageIndex];
            _titleLabel.text = page.Title;
            _bodyLabel.text = page.Body;
            _indexLabel.text = $"{_pageIndex + 1} / {Pages.Length}";

            if (_backButton != null)
            {
                _backButton.interactable = _pageIndex > 0;
            }

            if (_backLabel != null)
            {
                _backLabel.color = _pageIndex > 0
                    ? new Color(1f, 0.95f, 0.84f, 1f)
                    : new Color(0.56f, 0.56f, 0.6f, 1f);
            }

            if (_nextLabel != null)
            {
                _nextLabel.text = _pageIndex >= Pages.Length - 1
                    ? (_openedFromGameplay ? "START" : "DONE")
                    : "NEXT";
            }

            if (_closeLabel != null)
            {
                _closeLabel.text = _openedFromGameplay ? "SKIP" : "CLOSE";
            }
        }

        private void SetPanelVisible(bool visible)
        {
            if (_tutorialPanel == null)
            {
                return;
            }

            if (_panelAnimator != null)
            {
                if (visible)
                {
                    _panelAnimator.Show();
                }
                else
                {
                    _panelAnimator.Hide();
                }
            }
            else
            {
                _tutorialPanel.SetActive(visible);
            }
        }

        private void HidePanelInstant()
        {
            if (_tutorialPanel == null)
            {
                return;
            }

            if (_panelAnimator != null)
            {
                _panelAnimator.Hide(true);
            }
            else
            {
                _tutorialPanel.SetActive(false);
            }
        }

        private static bool HasSeenTutorial()
        {
            return PlayerPrefs.GetInt(SeenTutorialKey, 0) == 1;
        }

        private static void SetTutorialSeen()
        {
            PlayerPrefs.SetInt(SeenTutorialKey, 1);
            PlayerPrefs.Save();
        }

        private static bool IsStartScene(Scene scene)
        {
            return scene.name == "start";
        }

        private static bool HasGameplayPlayer()
        {
            return FindFirstObjectByType<BlockPlayerController>() != null;
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

        private readonly struct TutorialPage
        {
            public TutorialPage(string title, string body)
            {
                Title = title;
                Body = body;
            }

            public string Title { get; }
            public string Body { get; }
        }
    }
}
