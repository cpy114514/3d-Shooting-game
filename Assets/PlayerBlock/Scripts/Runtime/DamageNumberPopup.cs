using UnityEngine;

namespace PlayerBlock
{
    [RequireComponent(typeof(TextMesh))]
    public sealed class DamageNumberPopup : MonoBehaviour
    {
        [SerializeField] private float lifeTime = 0.9f;
        [SerializeField] private float riseSpeed = 1.2f;
        [SerializeField] private float driftSpeed = 0.35f;
        [SerializeField] private float scaleGrowth = 0.18f;

        private TextMesh _textMesh;
        private float _age;
        private Color _baseColor;
        private Vector3 _drift;
        private Vector3 _baseScale;

        public void Initialize(Color color, float amount)
        {
            _textMesh = GetComponent<TextMesh>();
            _baseColor = color;
            _baseScale = transform.localScale;
            _drift = new Vector3(Random.Range(-0.2f, 0.2f), 0f, Random.Range(-0.12f, 0.12f));

            if (_textMesh != null)
            {
                _textMesh.text = Mathf.CeilToInt(Mathf.Max(0f, amount)).ToString();
                _textMesh.color = color;
            }
        }

        private void Awake()
        {
            _textMesh = GetComponent<TextMesh>();
            _baseScale = transform.localScale;
        }

        private void Start()
        {
            if (_textMesh != null && _textMesh.font == null)
            {
                _textMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
        }

        private void Update()
        {
            _age += Time.deltaTime;
            var t = lifeTime <= 0f ? 1f : Mathf.Clamp01(_age / lifeTime);

            transform.position += (Vector3.up * riseSpeed + _drift * driftSpeed) * Time.deltaTime;
            transform.localScale = _baseScale * (1f + t * scaleGrowth);

            var cameraToFace = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (cameraToFace != null)
            {
                transform.rotation = Quaternion.LookRotation(cameraToFace.transform.position - transform.position, cameraToFace.transform.up);
            }

            if (_textMesh != null)
            {
                var color = _baseColor;
                color.a = Mathf.Lerp(1f, 0f, t);
                _textMesh.color = color;
            }

            if (_age >= lifeTime)
            {
                Destroy(gameObject);
            }
        }
    }
}
