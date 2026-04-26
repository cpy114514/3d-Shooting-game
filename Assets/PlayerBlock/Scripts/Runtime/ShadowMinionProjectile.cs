using UnityEngine;

namespace PlayerBlock
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public sealed class ShadowMinionProjectile : MonoBehaviour
    {
        [SerializeField] private float damage = 6f;
        [SerializeField] private float lifeTime = 3f;

        private Rigidbody _rigidbody;
        private GameObject _owner;
        private float _age;
        private bool _hasImpacted;

        public void Launch(Vector3 velocity, float projectileDamage, GameObject owner)
        {
            damage = projectileDamage;
            _owner = owner;
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.useGravity = false;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _rigidbody.interpolation = RigidbodyInterpolation.None;
            _rigidbody.linearVelocity = velocity;
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
            if (_hasImpacted || collision.gameObject == _owner)
            {
                return;
            }

            var contact = collision.GetContact(0);
            ResolveHit(collision.collider, contact.point, contact.normal);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasImpacted || other.gameObject == _owner)
            {
                return;
            }

            var impactPoint = other.ClosestPoint(transform.position);
            var impactNormal = _rigidbody != null && _rigidbody.linearVelocity.sqrMagnitude > 0.001f
                ? -_rigidbody.linearVelocity.normalized
                : -transform.forward;
            ResolveHit(other, impactPoint, impactNormal);
        }

        private void ResolveHit(Collider other, Vector3 impactPoint, Vector3 impactNormal)
        {
            if (_hasImpacted || other == null)
            {
                return;
            }

            var shadow = other.GetComponentInParent<ShadowCloneTarget>();
            if (shadow != null && shadow.IsAlive)
            {
                if (other.GetComponentInParent<ShadowMinionShield>() != null)
                {
                    _hasImpacted = true;
                    CombatVfxUtility.SpawnImpactBurst(impactPoint, impactNormal, new Color(0.08f, 0.05f, 0.12f, 1f), 0.2f, 5);
                    Destroy(gameObject);
                    return;
                }

                _hasImpacted = true;
                if (shadow.IsShield
                    && !shadow.IsShieldBroken
                    && shadow.TryBlockIncomingAttack(transform.position - impactNormal * 0.3f, impactPoint))
                {
                    Destroy(gameObject);
                    return;
                }

                shadow.TakeDamage(damage);
                CombatVfxUtility.SpawnImpactBurst(impactPoint, impactNormal, new Color(0.08f, 0.05f, 0.1f, 1f), 0.2f, 5);
                Destroy(gameObject);
                return;
            }

            var player = other.GetComponentInParent<BlockPlayerController>();
            if (player != null && player.Health > 0f)
            {
                if (other.GetComponentInParent<ShadowMinionShield>() != null)
                {
                    _hasImpacted = true;
                    CombatVfxUtility.SpawnImpactBurst(impactPoint, impactNormal, new Color(0.08f, 0.05f, 0.12f, 1f), 0.2f, 5);
                    Destroy(gameObject);
                    return;
                }

                _hasImpacted = true;
                player.TakeDamage(damage);
                CombatVfxUtility.SpawnImpactBurst(impactPoint, impactNormal, new Color(0.08f, 0.05f, 0.1f, 1f), 0.2f, 5);
                Destroy(gameObject);
                return;
            }

            var boss = other.GetComponentInParent<GiantBossController>();
            if (boss != null)
            {
                return;
            }

            _hasImpacted = true;
            CombatVfxUtility.SpawnDustBurst(impactPoint, impactNormal, 0.14f, 4);
            Destroy(gameObject);
        }
    }
}
