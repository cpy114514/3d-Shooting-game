using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PlayerBlock
{
    public static class UiEffectsUtility
    {
        public static void EnsureSceneButtonEffects()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                EnsureButtonEffects(roots[i].transform);
            }
        }

        public static void EnsureButtonEffects(Transform root)
        {
            if (root == null)
            {
                return;
            }

            var buttons = root.GetComponentsInChildren<Button>(true);
            for (var i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                if (button == null || button.GetComponent<UiButtonFeedback>() != null)
                {
                    continue;
                }

                button.gameObject.AddComponent<UiButtonFeedback>();
            }
        }

        public static UiPanelAnimator EnsureSettingsAnimator(GameObject panel)
        {
            return EnsurePanelAnimator(panel, -36f, 0.94f, 6f, 0.006f, 0.18f, 0.12f);
        }

        public static UiPanelAnimator EnsurePauseAnimator(GameObject panel)
        {
            return EnsurePanelAnimator(panel, -32f, 0.95f, 4f, 0.005f, 0.16f, 0.1f);
        }

        public static UiPanelAnimator EnsureEndAnimator(GameObject panel)
        {
            return EnsurePanelAnimator(panel, -24f, 0.9f, 8f, 0.01f, 0.2f, 0.12f);
        }

        public static UiPanelAnimator EnsureDeathAnimator(GameObject panel)
        {
            return EnsurePanelAnimator(panel, -18f, 0.9f, 7f, 0.009f, 0.18f, 0.1f);
        }

        private static UiPanelAnimator EnsurePanelAnimator(GameObject panel, float hiddenY, float collapsedScale, float idleY, float idleScale, float showTime, float hideTime)
        {
            if (panel == null)
            {
                return null;
            }

            var animator = panel.GetComponent<UiPanelAnimator>();
            if (animator == null)
            {
                animator = panel.AddComponent<UiPanelAnimator>();
            }

            animator.Configure(hiddenY, collapsedScale, idleY, idleScale, showTime, hideTime);
            return animator;
        }
    }
}
