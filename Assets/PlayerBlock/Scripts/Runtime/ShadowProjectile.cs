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
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.linearVelocity = velocity;

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
            SpawnShadowClone(contact.point, contact.normal, _cloneKind);
            Destroy(gameObject);
        }

        private static void SpawnShadowClone(Vector3 position, Vector3 normal, ShadowCloneKind cloneKind)
        {
            var cloneRoot = new GameObject("ShadowClone");
            cloneRoot.transform.position = FindGroundedSpawnPosition(position, normal);
            cloneRoot.transform.rotation = Quaternion.identity;

            var cloneCollider = cloneRoot.AddComponent<BoxCollider>();
            cloneCollider.center = new Vector3(0f, 1f, 0f);
            cloneCollider.size = new Vector3(1.25f, 2f, 0.75f);

            var cloneRigidbody = cloneRoot.AddComponent<Rigidbody>();
            cloneRigidbody.useGravity = true;
            cloneRigidbody.mass = 4f;
            cloneRigidbody.linearDamping = 0.8f;
            cloneRigidbody.angularDamping = 6f;
            cloneRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            cloneRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            cloneRigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            var material = CreateShadowMaterial();
            CreateBlock(cloneRoot.transform, "Body", new Vector3(0f, 1.05f, 0f), new Vector3(0.86f, 1.08f, 0.46f), material);
            CreateBlock(cloneRoot.transform, "Head", new Vector3(0f, 1.82f, 0f), new Vector3(0.56f, 0.56f, 0.56f), material);
            CreateBlock(cloneRoot.transform, "LeftArm", new Vector3(-0.66f, 1.1f, 0f), new Vector3(0.24f, 0.82f, 0.24f), material);
            CreateBlock(cloneRoot.transform, "RightArm", new Vector3(0.66f, 1.1f, 0f), new Vector3(0.24f, 0.82f, 0.24f), material);
            CreateBlock(cloneRoot.transform, "LeftLeg", new Vector3(-0.24f, 0.34f, 0f), new Vector3(0.32f, 0.68f, 0.32f), material);
            CreateBlock(cloneRoot.transform, "RightLeg", new Vector3(0.24f, 0.34f, 0f), new Vector3(0.32f, 0.68f, 0.32f), material);
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
            var hits = Physics.RaycastAll(rayStart, Vector3.down, 7f);
            var bestDistance = float.PositiveInfinity;
            var bestPoint = position;
            var foundGround = false;

            for (var i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
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

            return material;
        }
    }
}
