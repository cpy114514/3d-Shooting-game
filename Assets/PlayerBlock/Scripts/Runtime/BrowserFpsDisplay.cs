using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PlayerBlock
{
    public sealed class BrowserFpsDisplay : MonoBehaviour
    {
        private TMP_Text _tmpLabel;
        private Text _uiTextLabel;
        private float _smoothedDeltaTime = 1f / 60f;

        private void Awake()
        {
            BrowserGameSettings.Changed += RefreshVisibility;
            CacheLabel();
            RefreshVisibility();
        }

        private void OnEnable()
        {
            CacheLabel();
            RefreshVisibility();
        }

        private void OnDestroy()
        {
            BrowserGameSettings.Changed -= RefreshVisibility;
        }

        private void Update()
        {
            if (!BrowserGameSettings.ShowFps)
            {
                return;
            }

            if (_tmpLabel == null && _uiTextLabel == null)
            {
                CacheLabel();
            }

            _smoothedDeltaTime = Mathf.Lerp(_smoothedDeltaTime, Time.unscaledDeltaTime, 0.12f);
            var fps = _smoothedDeltaTime > 0.0001f ? Mathf.RoundToInt(1f / _smoothedDeltaTime) : 0;
            var text = "FPS " + fps;

            if (_tmpLabel != null)
            {
                _tmpLabel.text = text;
            }

            if (_uiTextLabel != null)
            {
                _uiTextLabel.text = text;
            }
        }

        private void RefreshVisibility()
        {
            CacheLabel();

            if (_tmpLabel != null)
            {
                _tmpLabel.gameObject.SetActive(BrowserGameSettings.ShowFps);
            }

            if (_uiTextLabel != null)
            {
                _uiTextLabel.gameObject.SetActive(BrowserGameSettings.ShowFps);
            }
        }

        private void CacheLabel()
        {
            if (_tmpLabel != null || _uiTextLabel != null)
            {
                return;
            }

            var labelObject = FindSceneObject("FpsLabel");
            if (labelObject == null)
            {
                return;
            }

            _tmpLabel = labelObject.GetComponent<TMP_Text>();
            _uiTextLabel = labelObject.GetComponent<Text>();
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
