using UnityEngine;

namespace PlayerBlock
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public sealed class ShadowBoltProjectile : MonoBehaviour
    {
        [SerializeField] private float damage = 1f;
        [SerializeField] private float lifeTime = 2.2f;

        private Rigidbody _rigidbody;
        private float _age;
        private bool _hasImpacted;

        public void Launch(Vector3 velocity, float boltDamage)
        {
            damage = boltDamage;
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.useGravity = false;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _rigidbody.interpolation = RigidbodyInterpolation.None;
            _rigidbody.linearVelocity = velocity;
            CombatVfxUtility.ConfigureTrail(gameObject, 0.14f, Mathf.Max(0.04f, transform.localScale.x * 0.32f));
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_age >= lifeTime)
            {
                Destroy(gameObject);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_hasImpacted)
            {
                return;
            }

            var contact = collision.GetContact(0);
            ResolveHit(collision.collider, contact.point, contact.normal);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasImpacted)
            {
                return;
            }

            var impactNormal = _rigidbody != null && _rigidbody.linearVelocity.sqrMagnitude > 0.001f
                ? -_rigidbody.linearVelocity.normalized
                : -transform.forward;
            ResolveHit(other, other.ClosestPoint(transform.position), impactNormal);
        }

        private void ResolveHit(Collider other, Vector3 impactPoint, Vector3 impactNormal)
        {
            if (_hasImpacted || other == null)
            {
                return;
            }

            var shield = other.GetComponentInParent<ShadowMinionShield>();
            if (shield != null)
            {
                _hasImpacted = true;
                CombatVfxUtility.SpawnImpactBurst(impactPoint, impactNormal, new Color(0.08f, 0.05f, 0.12f, 1f), 0.18f, 5);
                Destroy(gameObject);
                return;
            }

            var boss = other.GetComponentInParent<GiantBossController>();
            if (boss != null)
            {
                _hasImpacted = true;
                boss.TakeDamage(damage);
                CombatVfxUtility.SpawnImpactBurst(impactPoint, impactNormal, new Color(0.1f, 0.06f, 0.14f, 1f), 0.18f, 5);
                Destroy(gameObject);
                return;
            }

            var minion = other.GetComponentInParent<ShadowMinionController>();
            if (minion != null && minion.IsAlive)
            {
                _hasImpacted = true;
                minion.TakeDamage(damage);
                CombatVfxUtility.SpawnImpactBurst(impactPoint, impactNormal, new Color(0.08f, 0.05f, 0.12f, 1f), 0.18f, 5);
                Destroy(gameObject);
                return;
            }

            _hasImpacted = true;
            CombatVfxUtility.SpawnDustBurst(impactPoint, impactNormal, 0.14f, 4);
            Destroy(gameObject);
        }
    }
}
