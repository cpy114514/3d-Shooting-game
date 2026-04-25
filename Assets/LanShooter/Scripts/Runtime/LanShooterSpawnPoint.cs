using UnityEngine;

namespace LanShooter
{
    public sealed class LanShooterSpawnPoint : MonoBehaviour
    {
        [SerializeField] private Color gizmoColor = new(0.22f, 0.85f, 0.35f, 1f);
        [SerializeField] private float gizmoRadius = 0.45f;

        public Vector3 Position => transform.position;

        public Quaternion Rotation => transform.rotation;

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, gizmoRadius);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1.2f);
        }
    }
}
