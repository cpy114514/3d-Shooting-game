using UnityEngine;

namespace PlayerBlock
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ShadowMinionShield : MonoBehaviour
    {
        private BoxCollider _shieldCollider;

        private void Awake()
        {
            EnsureCollider();
        }

        private void OnEnable()
        {
            EnsureCollider();
        }

        private void OnValidate()
        {
            EnsureCollider();
        }

        private void EnsureCollider()
        {
            if (_shieldCollider == null)
            {
                _shieldCollider = GetComponent<BoxCollider>();
            }

            if (_shieldCollider == null)
            {
                _shieldCollider = gameObject.AddComponent<BoxCollider>();
            }

            _shieldCollider.isTrigger = false;
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                var bounds = renderer.bounds;
                var scale = transform.lossyScale;
                var localCenter = transform.InverseTransformPoint(bounds.center);
                var localSize = new Vector3(
                    Mathf.Abs(scale.x) > 0.0001f ? bounds.size.x / Mathf.Abs(scale.x) : 1f,
                    Mathf.Abs(scale.y) > 0.0001f ? bounds.size.y / Mathf.Abs(scale.y) : 1f,
                    Mathf.Abs(scale.z) > 0.0001f ? bounds.size.z / Mathf.Abs(scale.z) : 1f);

                localSize.x = Mathf.Max(localSize.x * 1.05f, 1.15f);
                localSize.y = Mathf.Max(localSize.y * 1.05f, 1.65f);
                localSize.z = Mathf.Max(localSize.z * 4.5f, 0.7f);

                _shieldCollider.center = localCenter + new Vector3(0f, 0f, 0.12f);
                _shieldCollider.size = localSize;
                return;
            }

            _shieldCollider.center = new Vector3(0f, 0f, 0.12f);
            _shieldCollider.size = new Vector3(1.25f, 1.85f, 2.2f);
        }
    }
}
