using System.Collections;
using UnityEngine;

namespace PlayerBlock
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class UiPanelAnimator : MonoBehaviour
    {
        [SerializeField] private float showDuration = 0.18f;
        [SerializeField] private float hideDuration = 0.12f;
        [SerializeField] private float hiddenScale = 0.92f;
        [SerializeField] private float hiddenYOffset = -28f;
        [SerializeField] private float showOvershootScale = 1.02f;
        [SerializeField] private float idleBobAmplitude = 5f;
        [SerializeField] private float idleScaleAmplitude = 0.008f;
        [SerializeField] private float idleFrequency = 2.2f;

        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private Vector2 _shownAnchoredPosition;
        private Vector3 _shownScale;
        private Coroutine _animationRoutine;
        private bool _initialized;
        private bool _visible;
        private float _shownTime;

        public bool IsVisible => _visible;

        public void Configure(float hiddenY, float collapsedScale, float idleY, float idleScale, float showTime, float hideTime)
        {
            hiddenYOffset = hiddenY;
            hiddenScale = collapsedScale;
            idleBobAmplitude = idleY;
            idleScaleAmplitude = idleScale;
            showDuration = showTime;
            hideDuration = hideTime;
        }

        public void Show(bool instant = false)
        {
            EnsureInitialized();

            if (_animationRoutine != null)
            {
                StopCoroutine(_animationRoutine);
                _animationRoutine = null;
            }

            gameObject.SetActive(true);
            _visible = true;
            _shownTime = Time.unscaledTime;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;

            if (instant)
            {
                ApplyShownState(1f);
                return;
            }

            _animationRoutine = StartCoroutine(Animate(true));
        }

        public void Hide(bool instant = false)
        {
            EnsureInitialized();

            if (!gameObject.activeSelf && !_visible)
            {
                return;
            }

            if (_animationRoutine != null)
            {
                StopCoroutine(_animationRoutine);
                _animationRoutine = null;
            }

            _visible = false;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            if (instant)
            {
                ApplyHiddenState();
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            _animationRoutine = StartCoroutine(Animate(false));
        }

        private void Awake()
        {
            EnsureInitialized();
            if (gameObject.activeSelf)
            {
                _visible = true;
                ApplyShownState(1f);
            }
        }

        private void LateUpdate()
        {
            if (!_initialized || !_visible || _animationRoutine != null)
            {
                return;
            }

            var bobPhase = (Time.unscaledTime - _shownTime) * idleFrequency;
            var bobOffset = Mathf.Sin(bobPhase) * idleBobAmplitude;
            var scaleOffset = 1f + Mathf.Sin(bobPhase * 0.9f + 0.3f) * idleScaleAmplitude;

            _rectTransform.anchoredPosition = _shownAnchoredPosition + new Vector2(0f, bobOffset);
            _rectTransform.localScale = _shownScale * scaleOffset;
        }

        private IEnumerator Animate(bool showing)
        {
            var duration = Mathf.Max(0.01f, showing ? showDuration : hideDuration);
            var startAlpha = _canvasGroup.alpha;
            var startPosition = _rectTransform.anchoredPosition;
            var startScale = _rectTransform.localScale;

            var targetAlpha = showing ? 1f : 0f;
            var targetPosition = showing ? _shownAnchoredPosition : _shownAnchoredPosition + new Vector2(0f, hiddenYOffset);
            var endScale = showing ? _shownScale : _shownScale * hiddenScale;

            for (var elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = EaseOutCubic(t);
                if (showing)
                {
                    var overshoot = Mathf.LerpUnclamped(hiddenScale, showOvershootScale, eased);
                    var settle = Mathf.Lerp(overshoot, 1f, Mathf.SmoothStep(0f, 1f, t));
                    _rectTransform.localScale = _shownScale * settle;
                }
                else
                {
                    _rectTransform.localScale = Vector3.LerpUnclamped(startScale, endScale, eased);
                }

                _canvasGroup.alpha = Mathf.LerpUnclamped(startAlpha, targetAlpha, eased);
                _rectTransform.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, eased);
                yield return null;
            }

            if (showing)
            {
                ApplyShownState(1f);
            }
            else
            {
                ApplyHiddenState();
                gameObject.SetActive(false);
            }

            _animationRoutine = null;
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            _shownAnchoredPosition = _rectTransform.anchoredPosition;
            _shownScale = _rectTransform.localScale == Vector3.zero ? Vector3.one : _rectTransform.localScale;
            _initialized = true;
        }

        private void ApplyShownState(float alpha)
        {
            _canvasGroup.alpha = alpha;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            _rectTransform.anchoredPosition = _shownAnchoredPosition;
            _rectTransform.localScale = _shownScale;
        }

        private void ApplyHiddenState()
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            _rectTransform.anchoredPosition = _shownAnchoredPosition + new Vector2(0f, hiddenYOffset);
            _rectTransform.localScale = _shownScale * hiddenScale;
        }

        private static float EaseOutCubic(float value)
        {
            var inverse = 1f - value;
            return 1f - inverse * inverse * inverse;
        }
    }
}
