using System.Collections.Generic;
using UnityEngine;

namespace PlayerBlock
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public sealed class ShadowProjectile : MonoBehaviour
    {
        [SerializeField] private float lifeTime = 4f;
        [SerializeField] private float pulseSpeed = 16f;
        [SerializeField] private float pulseAmount = 0.16f;

        private static readonly RaycastHit[] GroundHitBuffer = new RaycastHit[12];
        private static readonly Stack<ShadowProjectile> Pool = new Stack<ShadowProjectile>(24);
        private static Material SharedFallbackShadowMaterial;
        private static Transform PoolRoot
        {
            get
            {
                if (_poolRoot != null)
                {
                    return _poolRoot;
                }

                var poolObject = new GameObject("ShadowProjectilePool");
                poolObject.hideFlags = HideFlags.HideInHierarchy;
                Object.DontDestroyOnLoad(poolObject);
                _poolRoot = poolObject.transform;
                return _poolRoot;
            }
        }

        private static Transform _poolRoot;

        private Rigidbody _rigidbody;
        private SphereCollider _sphereCollider;
        private Renderer _renderer;
        private GameObject _owner;
        private Collider _ownerCollider;
        private Collider[] _ownerColliders;
        private Vector3 _baseScale;
        private ShadowCloneKind _cloneKind = ShadowCloneKind.Melee;
        private float _age;
        private bool _hasImpacted;
        private bool _isPooledRelease;

        public static ShadowProjectile Spawn(
            Vector3 position,
            Vector3 velocity,
            GameObject owner,
            ShadowCloneKind cloneKind,
            Material material,
            float scale,
            float trailTime,
            float trailWidth,
            float colliderRadius)
        {
            var projectile = Acquire();
            var projectileTransform = projectile.transform;
            projectileTransform.SetParent(null, true);
            projectileTransform.SetPositionAndRotation(position, Quaternion.identity);
            projectileTransform.localScale = Vector3.one * scale;
            projectile.gameObject.SetActive(true);
            projectile.Launch(velocity, owner, cloneKind, material, trailTime, trailWidth, colliderRadius);
            return projectile;
        }

        public void SetCloneKind(ShadowCloneKind cloneKind)
        {
            _cloneKind = cloneKind;
        }

        public void Launch(Vector3 velocity, GameObject owner, ShadowCloneKind cloneKind, Material material, float trailTime, float trailWidth, float colliderRadius)
        {
            _isPooledRelease = false;
            _owner = owner;
            _ownerCollider = owner != null ? owner.GetComponent<Collider>() : null;
            _ownerColliders = owner != null ? owner.GetComponentsInChildren<Collider>(true) : null;
            _cloneKind = cloneKind;
            _rigidbody = GetComponent<Rigidbody>();
            _sphereCollider = GetComponent<SphereCollider>();
            _renderer = GetComponent<Renderer>();
            _age = 0f;
            _hasImpacted = false;
            _baseScale = transform.localScale;
            _rigidbody.useGravity = false;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _rigidbody.interpolation = RigidbodyInterpolation.None;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.linearVelocity = velocity;
            if (_renderer != null && material != null)
            {
                _renderer.sharedMaterial = material;
            }

            if (_sphereCollider != null)
            {
                _sphereCollider.radius = colliderRadius;
            }

            CombatVfxUtility.ConfigureTrail(gameObject, trailTime, Mathf.Max(0.05f, transform.localScale.x * trailWidth));
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
            _renderer = GetComponent<Renderer>();
            _baseScale = transform.localScale;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_age >= lifeTime)
            {
                ReleaseToPool();
                return;
            }

            var pulse = 1f + Mathf.Sin(_age * pulseSpeed) * pulseAmount;
            transform.localScale = _baseScale * pulse;
            transform.Rotate(0f, 360f * Time.deltaTime, 180f * Time.deltaTime, Space.Self);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_hasImpacted || collision.gameObject == _owner)
            {
                return;
            }

            _hasImpacted = true;
            var contact = collision.GetContact(0);
            var burstColor = _cloneKind == ShadowCloneKind.Ranged
                ? new Color(0.09f, 0.05f, 0.12f, 1f)
                : _cloneKind == ShadowCloneKind.Shield
                    ? new Color(0.07f, 0.09f, 0.12f, 1f)
                    : new Color(0.11f, 0.11f, 0.12f, 1f);
            CombatVfxUtility.SpawnImpactBurst(contact.point, contact.normal, burstColor, 0.24f, 6);
            GameAudioManager.PlayPlayerHit();
            SpawnShadowClone(contact.point, contact.normal, _cloneKind);
            ReleaseToPool();
        }

        private static ShadowProjectile Acquire()
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

        private static ShadowProjectile CreateProjectile()
        {
            var projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileObject.name = "ShadowBullet";
            projectileObject.hideFlags = HideFlags.HideInHierarchy;
            projectileObject.transform.SetParent(PoolRoot, false);

            var projectile = projectileObject.AddComponent<ShadowProjectile>();
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
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _age = 0f;
            _hasImpacted = false;
            if (Pool.Count < 48)
            {
                gameObject.SetActive(false);
                transform.SetParent(PoolRoot, false);
                Pool.Push(this);
                return;
            }

            Object.Destroy(gameObject);
        }

        private static void SpawnShadowClone(Vector3 position, Vector3 normal, ShadowCloneKind cloneKind)
        {
            var prefab = ShadowClonePrefabLibrary.GetPrefab(cloneKind);
            if (prefab != null)
            {
                var cloneRoot = Object.Instantiate(prefab);
                cloneRoot.name = "ShadowClone";
                cloneRoot.transform.SetPositionAndRotation(
                    FindGroundedSpawnPosition(position, normal),
                    Quaternion.identity);

                var shadow = cloneRoot.GetComponent<ShadowCloneTarget>();
                if (shadow != null)
                {
                    shadow.SetKind(cloneKind);
                }

                return;
            }

            BuildProceduralShadowClone(position, normal, cloneKind);
        }

        private static void BuildProceduralShadowClone(Vector3 position, Vector3 normal, ShadowCloneKind cloneKind)
        {
            var cloneRoot = new GameObject("ShadowClone");
            cloneRoot.transform.position = FindGroundedSpawnPosition(position, normal);
            cloneRoot.transform.rotation = Quaternion.identity;

            var cloneCollider = cloneRoot.AddComponent<BoxCollider>();
            cloneCollider.center = cloneKind == ShadowCloneKind.Shield
                ? new Vector3(0.02f, 1.02f, 0.08f)
                : new Vector3(0f, 1f, 0f);
            cloneCollider.size = cloneKind == ShadowCloneKind.Shield
                ? new Vector3(1.48f, 2.08f, 1.05f)
                : new Vector3(1.25f, 2f, 0.75f);

            var cloneRigidbody = cloneRoot.AddComponent<Rigidbody>();
            cloneRigidbody.useGravity = true;
            cloneRigidbody.mass = 4f;
            cloneRigidbody.linearDamping = 0.8f;
            cloneRigidbody.angularDamping = 6f;
            cloneRigidbody.interpolation = RigidbodyInterpolation.None;
            cloneRigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
            cloneRigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            var material = CreateShadowMaterial();
            CreateBlock(cloneRoot.transform, "Body", new Vector3(0f, 1.05f, 0f), new Vector3(0.86f, 1.08f, 0.46f), material);
            CreateBlock(cloneRoot.transform, "Head", new Vector3(0f, 1.82f, 0f), new Vector3(0.56f, 0.56f, 0.56f), material);
            CreateBlock(cloneRoot.transform, "LeftArm", new Vector3(-0.66f, 1.1f, 0f), new Vector3(0.24f, 0.82f, 0.24f), material);
            CreateBlock(cloneRoot.transform, "RightArm", new Vector3(0.66f, 1.1f, 0f), new Vector3(0.24f, 0.82f, 0.24f), material);
            CreateBlock(cloneRoot.transform, "LeftLeg", new Vector3(-0.24f, 0.34f, 0f), new Vector3(0.32f, 0.68f, 0.32f), material);
            CreateBlock(cloneRoot.transform, "RightLeg", new Vector3(0.24f, 0.34f, 0f), new Vector3(0.32f, 0.68f, 0.32f), material);

            if (cloneKind == ShadowCloneKind.Shield)
            {
                CreateBlock(cloneRoot.transform, "Shield", new Vector3(-0.52f, 1.12f, 0.36f), new Vector3(0.82f, 1.14f, 0.14f), material);
                CreateBlock(cloneRoot.transform, "ShieldBoss", new Vector3(-0.16f, 0.96f, 0.42f), new Vector3(0.24f, 0.38f, 0.1f), material);
            }

            var shadow = cloneRoot.AddComponent<ShadowCloneTarget>();
            shadow.SetKind(cloneKind);
        }

        private static Vector3 FindGroundedSpawnPosition(Vector3 position, Vector3 normal)
        {
            if (normal.y > 0.45f)
            {
                return position;
            }

            var rayStart = position + Vector3.up * 2f;
            var hitCount = Physics.RaycastNonAlloc(rayStart, Vector3.down, GroundHitBuffer, 7f);
            var bestDistance = float.PositiveInfinity;
            var bestPoint = position;
            var foundGround = false;

            for (var i = 0; i < hitCount; i++)
            {
                var hit = GroundHitBuffer[i];
                if (hit.collider == null
                    || hit.collider.GetComponentInParent<GiantBossController>() != null
                    || hit.collider.GetComponentInParent<BlockPlayerController>() != null
                    || hit.collider.GetComponentInParent<ShadowCloneTarget>() != null)
                {
                    continue;
                }

                if (hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    bestPoint = hit.point;
                    foundGround = true;
                }
            }

            return foundGround ? bestPoint : position;
        }

        private static void CreateBlock(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = localPosition;
            block.transform.localScale = localScale;
            Object.Destroy(block.GetComponent<Collider>());

            var renderer = block.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static Material CreateShadowMaterial()
        {
            if (SharedFallbackShadowMaterial != null)
            {
                return SharedFallbackShadowMaterial;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var shadowColor = new Color(0.005f, 0.005f, 0.006f, 1f);
            var material = new Material(shader)
            {
                color = shadowColor
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", shadowColor);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(0.015f, 0.01f, 0.025f));
            }

            SharedFallbackShadowMaterial = material;
            return SharedFallbackShadowMaterial;
        }
    }
}
