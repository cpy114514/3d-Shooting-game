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
        private static Material SharedFallbackShadowMaterial;

        private Rigidbody _rigidbody;
        private GameObject _owner;
        private Vector3 _baseScale;
        private ShadowCloneKind _cloneKind = ShadowCloneKind.Melee;
        private float _age;
        private bool _hasImpacted;

        public void SetCloneKind(ShadowCloneKind cloneKind)
        {
            _cloneKind = cloneKind;
        }

        public void Launch(Vector3 velocity, GameObject owner)
        {
            _owner = owner;
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.useGravity = false;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _rigidbody.interpolation = RigidbodyInterpolation.None;
            _rigidbody.linearVelocity = velocity;
            CombatVfxUtility.ConfigureTrail(gameObject, 0.18f, Mathf.Max(0.05f, transform.localScale.x * 0.42f));

            var projectileCollider = GetComponent<Collider>();
            var ownerCollider = owner != null ? owner.GetComponent<Collider>() : null;
            if (projectileCollider != null && ownerCollider != null)
            {
                Physics.IgnoreCollision(projectileCollider, ownerCollider, true);
            }
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _baseScale = transform.localScale;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_age >= lifeTime)
            {
                Destroy(gameObject);
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
            SpawnShadowClone(contact.point, contact.normal, _cloneKind);
            Destroy(gameObject);
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
