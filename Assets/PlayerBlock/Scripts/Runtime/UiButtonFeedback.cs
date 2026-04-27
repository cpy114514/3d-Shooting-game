using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PlayerBlock
{
    [DisallowMultipleComponent]
    public sealed class UiButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private float hoverScale = 1.025f;
        [SerializeField] private float pressedScale = 0.985f;
        [SerializeField] private float transitionSpeed = 18f;
        [SerializeField] private Color hoverTint = new Color(1f, 0.985f, 0.94f, 1f);
        [SerializeField] private Color pressedTint = new Color(0.93f, 0.95f, 0.98f, 1f);

        private RectTransform _rectTransform;
        private Graphic _graphic;
        private Button _button;
        private Vector3 _baseScale;
        private Color _baseColor;
        private Vector3 _targetScale;
        private Color _targetColor;
        private Coroutine _pulseRoutine;
        private bool _hovered;
        private bool _pressed;

        private void Awake()
        {
            _rectTransform = transform as RectTransform;
            _graphic = GetComponent<Graphic>();
            _button = GetComponent<Button>();
            _baseScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
            _targetScale = _baseScale;

            if (_graphic != null)
            {
                _baseColor = _graphic.color;
                _targetColor = _baseColor;
            }
        }

        private void OnEnable()
        {
            ApplyImmediate(false);
        }

        private void Update()
        {
            if (_button != null && !_button.interactable)
            {
                _targetScale = _baseScale;
                _targetColor = _baseColor * 0.85f;
            }

            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.unscaledDeltaTime * transitionSpeed);
            if (_graphic != null)
            {
                _graphic.color = Color.Lerp(_graphic.color, _targetColor, Time.unscaledDeltaTime * transitionSpeed);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            RefreshTargets();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            _pressed = false;
            RefreshTargets();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressed = true;
            RefreshTargets();
            PlayPulse();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _pressed = false;
            RefreshTargets();
        }

        public void OnSelect(BaseEventData eventData)
        {
            _hovered = true;
            RefreshTargets();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _hovered = false;
            _pressed = false;
            RefreshTargets();
        }

        private void RefreshTargets()
        {
            if (_button != null && !_button.interactable)
            {
                _targetScale = _baseScale;
                _targetColor = _baseColor * 0.85f;
                return;
            }

            if (_pressed)
            {
                _targetScale = _baseScale * pressedScale;
                _targetColor = pressedTint;
                return;
            }

            if (_hovered)
            {
                _targetScale = _baseScale * hoverScale;
                _targetColor = hoverTint;
                return;
            }

            _targetScale = _baseScale;
            _targetColor = _baseColor;
        }

        private void ApplyImmediate(bool preservePressed)
        {
            _hovered = false;
            _pressed = preservePressed && _pressed;
            _targetScale = _baseScale;
            transform.localScale = _baseScale;
            if (_graphic != null)
            {
                _targetColor = _baseColor;
                _graphic.color = _baseColor;
            }
        }

        private void PlayPulse()
        {
            if (_pulseRoutine != null)
            {
                StopCoroutine(_pulseRoutine);
            }

            _pulseRoutine = StartCoroutine(PulseRoutine());
        }

        private IEnumerator PulseRoutine()
        {
            var pulseDuration = 0.12f;
            var pulseScale = _baseScale * 1.03f;

            for (var elapsed = 0f; elapsed < pulseDuration; elapsed += Time.unscaledDeltaTime)
            {
                var t = Mathf.Clamp01(elapsed / pulseDuration);
                transform.localScale = Vector3.LerpUnclamped(_baseScale, pulseScale, t);
                yield return null;
            }

            RefreshTargets();
            _pulseRoutine = null;
        }
    }
}
