using UnityEngine;

namespace LanShooter
{
    public sealed class LanShooterEnemySpawnPoint : MonoBehaviour
    {
        [SerializeField] private Color gizmoColor = new(0.92f, 0.28f, 0.18f, 1f);
        [SerializeField] private float gizmoRadius = 0.55f;

        public Vector3 Position => transform.position;

        public Quaternion Rotation => transform.rotation;

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, gizmoRadius);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1.35f);
        }
    }
}
