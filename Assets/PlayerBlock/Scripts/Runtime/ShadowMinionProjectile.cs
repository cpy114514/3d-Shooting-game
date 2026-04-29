using System.Collections.Generic;
using UnityEngine;

namespace PlayerBlock
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public sealed class ShadowMinionProjectile : MonoBehaviour
    {
        [SerializeField] private float damage = 6f;
        [SerializeField] private float lifeTime = 3f;

        private static readonly Stack<ShadowMinionProjectile> Pool = new Stack<ShadowMinionProjectile>(24);
        private Rigidbody _rigidbody;
        private SphereCollider _sphereCollider;
        private GameObject _owner;
        private Collider _ownerCollider;
        private float _age;
        private bool _hasImpacted;
        private bool _isPooledRelease;

        public static ShadowMinionProjectile Spawn(
            Vector3 position,
            Vector3 velocity,
            float projectileDamage,
            GameObject owner,
            float scale,
            float trailTime,
            float trailWidth,
            float colliderRadius)
        {
            var projectile = Acquire();
            projectile.transform.SetParent(null, true);
            projectile.transform.SetPositionAndRotation(position, Quaternion.identity);
            projectile.transform.localScale = Vector3.one * scale;
            projectile.gameObject.SetActive(true);
            projectile.Launch(velocity, projectileDamage, owner, trailTime, trailWidth, colliderRadius);
            return projectile;
        }

        public void Launch(Vector3 velocity, float projectileDamage, GameObject owner, float trailTime, float trailWidth, float colliderRadius)
        {
            _isPooledRelease = false;
            damage = projectileDamage;
            _owner = owner;
            _ownerCollider = owner != null ? owner.GetComponent<Collider>() : null;
            _rigidbody = GetComponent<Rigidbody>();
            _sphereCollider = GetComponent<SphereCollider>();
            _age = 0f;
            _hasImpacted = false;
            _rigidbody.useGravity = false;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _rigidbody.interpolation = RigidbodyInterpolation.None;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.linearVelocity = velocity;
            if (_sphereCollider != null)
            {
                _sphereCollider.radius = colliderRadius;
            }

            CombatVfxUtility.ConfigureTrail(gameObject, trailTime, Mathf.Max(0.04f, transform.localScale.x * trailWidth));
            if (_sphereCollider != null && _ownerCollider != null)
            {
                Physics.IgnoreCollision(_sphereCollider, _ownerCollider, true);
            }
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _sphereCollider = GetComponent<SphereCollider>();
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_age >= lifeTime)
            {
                ReleaseToPool();
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
                    GameAudioManager.PlayShieldBlock();
                    ReleaseToPool();
                    return;
                }

                _hasImpacted = true;
                if (shadow.IsShield
                    && !shadow.IsShieldBroken
                    && shadow.TryBlockIncomingAttack(transform.position - impactNormal * 0.3f, impactPoint))
                {
                    GameAudioManager.PlayShieldBlock();
                    ReleaseToPool();
                    return;
                }

                shadow.TakeDamage(damage);
                CombatVfxUtility.SpawnImpactBurst(impactPoint, impactNormal, new Color(0.08f, 0.05f, 0.1f, 1f), 0.2f, 5);
                ReleaseToPool();
                return;
            }

            var player = other.GetComponentInParent<BlockPlayerController>();
            if (player != null && player.Health > 0f)
            {
                if (other.GetComponentInParent<ShadowMinionShield>() != null)
                {
                    _hasImpacted = true;
                    CombatVfxUtility.SpawnImpactBurst(impactPoint, impactNormal, new Color(0.08f, 0.05f, 0.12f, 1f), 0.2f, 5);
                    GameAudioManager.PlayShieldBlock();
                    ReleaseToPool();
                    return;
                }

                _hasImpacted = true;
                player.TakeDamage(damage, transform.position);
                CombatVfxUtility.SpawnImpactBurst(impactPoint, impactNormal, new Color(0.08f, 0.05f, 0.1f, 1f), 0.2f, 5);
                ReleaseToPool();
                return;
            }

            var boss = other.GetComponentInParent<GiantBossController>();
            if (boss != null)
            {
                return;
            }

            _hasImpacted = true;
            CombatVfxUtility.SpawnDustBurst(impactPoint, impactNormal, 0.14f, 4);
            ReleaseToPool();
        }

        private static ShadowMinionProjectile Acquire()
        {
            while (Pool.Count > 0)
            {
                var candidate = Pool.Pop();
                if (candidate != null)
                {
                    return candidate;
                }
            }

            return CreateProjectile();
        }

        private static Transform _poolRoot;
        private static Transform PoolRoot
        {
            get
            {
                if (_poolRoot != null)
                {
                    return _poolRoot;
                }

                var poolObject = new GameObject("ShadowMinionProjectilePool");
                poolObject.hideFlags = HideFlags.HideInHierarchy;
                Object.DontDestroyOnLoad(poolObject);
                _poolRoot = poolObject.transform;
                return _poolRoot;
            }
        }

        private static ShadowMinionProjectile CreateProjectile()
        {
            var projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileObject.name = "ShadowEnemyBolt";
            projectileObject.hideFlags = HideFlags.HideInHierarchy;
            projectileObject.transform.SetParent(PoolRoot, false);

            var projectile = projectileObject.AddComponent<ShadowMinionProjectile>();
            projectileObject.SetActive(false);
            return projectile;
        }

        private void ReleaseToPool()
        {
            if (_isPooledRelease)
            {
                return;
            }

            _isPooledRelease = true;

            if (_sphereCollider != null && _ownerCollider != null)
            {
                Physics.IgnoreCollision(_sphereCollider, _ownerCollider, false);
            }

            _owner = null;
            _ownerCollider = null;
            _age = 0f;
            _hasImpacted = false;

            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }

            if (Pool.Count < 48)
            {
                gameObject.SetActive(false);
                transform.SetParent(PoolRoot, false);
                Pool.Push(this);
                return;
            }

            Object.Destroy(gameObject);
        }
    }
}
