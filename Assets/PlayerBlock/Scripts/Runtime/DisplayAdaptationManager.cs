using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace PlayerBlock
{
    public sealed class DisplayAdaptationManager : MonoBehaviour
    {
        private static DisplayAdaptationManager _instance;

        private Vector2Int _lastScreenSize;
        private FullScreenMode _lastFullScreenMode;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            EnsureInstance();
        }

        public static void Refresh()
        {
            EnsureInstance();
            if (_instance != null)
            {
                _instance.ApplyNow();
            }
        }

        private static void EnsureInstance()
        {
            if (_instance != null)
            {
                return;
            }

            _instance = FindFirstObjectByType<DisplayAdaptationManager>();
            if (_instance != null)
            {
                _instance.CaptureState();
                return;
            }

            var gameObject = new GameObject("DisplayAdaptationManager");
            _instance = gameObject.AddComponent<DisplayAdaptationManager>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplyNow();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                _instance = null;
            }
        }

        private void Update()
        {
            if (_lastScreenSize.x != Screen.width || _lastScreenSize.y != Screen.height || _lastFullScreenMode != Screen.fullScreenMode)
            {
                ApplyNow();
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyNow();
        }

        private void ApplyNow()
        {
            ApplyCanvasMatching();
            CaptureState();
        }

        private void CaptureState()
        {
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            _lastFullScreenMode = Screen.fullScreenMode;
        }

        private static void ApplyCanvasMatching()
        {
            var canvases = Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (canvases == null || canvases.Length == 0)
            {
                return;
            }

            var width = Mathf.Max(1, Screen.width);
            var height = Mathf.Max(1, Screen.height);
            var aspect = width / (float)height;
            var matchHeight = 1f - Mathf.Clamp01(Mathf.InverseLerp(1.45f, 2.35f, aspect));

            for (var i = 0; i < canvases.Length; i++)
            {
                var scaler = canvases[i];
                if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
                {
                    continue;
                }

                if (scaler.referenceResolution.x <= 0f || scaler.referenceResolution.y <= 0f)
                {
                    scaler.referenceResolution = new Vector2(1920f, 1080f);
                }

                scaler.matchWidthOrHeight = matchHeight;
            }
        }
    }
}
