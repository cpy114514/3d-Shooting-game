using System.Collections.Generic;
using UnityEngine;

namespace PlayerBlock
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public sealed class ShadowBoltProjectile : MonoBehaviour
    {
        [SerializeField] private float damage = 1f;
        [SerializeField] private float lifeTime = 2.2f;

        private static readonly Stack<ShadowBoltProjectile> Pool = new Stack<ShadowBoltProjectile>(24);
        private Rigidbody _rigidbody;
        private SphereCollider _sphereCollider;
        private GameObject _owner;
        private Collider _ownerCollider;
        private Collider[] _ownerColliders;
        private float _age;
        private bool _hasImpacted;
        private bool _isPooledRelease;

        public static ShadowBoltProjectile Spawn(
            Vector3 position,
            Vector3 velocity,
            float boltDamage,
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
            projectile.Launch(velocity, boltDamage, owner, trailTime, trailWidth, colliderRadius);
            return projectile;
        }

        public void Launch(Vector3 velocity, float boltDamage, GameObject owner, float trailTime, float trailWidth, float colliderRadius)
        {
            _isPooledRelease = false;
            damage = boltDamage;
            _owner = owner;
            _ownerCollider = owner != null ? owner.GetComponent<Collider>() : null;
            _ownerColliders = owner != null ? owner.GetComponentsInChildren<Collider>(true) : null;
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
            GameAudioManager.PlayEnemyShot();
            if (_sphereCollider != null && _ownerColliders != null)
            {
                for (var i = 0; i < _ownerColliders.Length; i++)
                {
                    var ownerCollider = _ownerColliders[i];
                    if (ownerCollider != null && ownerCollider.enabled)
                    {
                        Physics.IgnoreCollision(_sphereCollider, ownerCollider, true);
                    }
                }
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
                GameAudioManager.PlayShieldBlock();
                ReleaseToPool();
                return;
            }

            var boss = other.GetComponentInParent<GiantBossController>();
            if (boss != null)
            {
                _hasImpacted = true;
                boss.TakeDamage(damage);
                CombatVfxUtility.SpawnImpactBurst(impactPoint, impactNormal, new Color(0.1f, 0.06f, 0.14f, 1f), 0.18f, 5);
                ReleaseToPool();
                return;
            }

            var minion = other.GetComponentInParent<ShadowMinionController>();
            if (minion != null && minion.IsAlive)
            {
                _hasImpacted = true;
                minion.TakeDamage(damage);
                CombatVfxUtility.SpawnImpactBurst(impactPoint, impactNormal, new Color(0.08f, 0.05f, 0.12f, 1f), 0.18f, 5);
                ReleaseToPool();
                return;
            }

            _hasImpacted = true;
            CombatVfxUtility.SpawnDustBurst(impactPoint, impactNormal, 0.14f, 4);
            ReleaseToPool();
        }

        private static ShadowBoltProjectile Acquire()
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

                var poolObject = new GameObject("ShadowBoltProjectilePool");
                poolObject.hideFlags = HideFlags.HideInHierarchy;
                Object.DontDestroyOnLoad(poolObject);
                _poolRoot = poolObject.transform;
                return _poolRoot;
            }
        }

        private static ShadowBoltProjectile CreateProjectile()
        {
            var projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileObject.name = "ShadowRangedBolt";
            projectileObject.hideFlags = HideFlags.HideInHierarchy;
            projectileObject.transform.SetParent(PoolRoot, false);

            var projectile = projectileObject.AddComponent<ShadowBoltProjectile>();
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
            _age = 0f;
            _hasImpacted = false;
            if (_sphereCollider != null && _ownerColliders != null)
            {
                for (var i = 0; i < _ownerColliders.Length; i++)
                {
                    var ownerCollider = _ownerColliders[i];
                    if (ownerCollider != null && ownerCollider.enabled)
                    {
                        Physics.IgnoreCollision(_sphereCollider, ownerCollider, false);
                    }
                }
            }
            _owner = null;
            _ownerCollider = null;
            _ownerColliders = null;

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
