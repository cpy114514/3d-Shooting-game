using UnityEngine;

namespace PlayerBlock
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ShadowCloneTarget : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 1f;
        [SerializeField] private float lifeTime = 18f;
        [SerializeField] private float attackStartRange = 2.2f;
        [SerializeField] private float rangedAttackRange = 42f;
        [SerializeField] private float moveSpeed = 3.6f;
        [SerializeField] private float rangedPreferredDistance = 7f;
        [SerializeField] private float stopDistanceBuffer = 0.25f;
        [SerializeField] private float moveAnimationSpeed = 8.5f;
        [SerializeField] private float moveBobHeight = 0.08f;
        [SerializeField] private float moveArmSwing = 48f;
        [SerializeField] private float moveLegSwing = 42f;
        [SerializeField] private float handHitRadius = 0.38f;
        [SerializeField] private float meleeAttackDamage = 5f;
        [SerializeField] private float rangedAttackDamage = 1f;
        [SerializeField] private float rangedProjectileSpeed = 15f;
        [SerializeField] private float attackCooldown = 0.85f;
        [SerializeField] private float attackAnimationDuration = 0.22f;
        [SerializeField] private float crushHorizontalRadius = 1.15f;
        [SerializeField] private float crushHeightTolerance = 0.25f;

        private Transform _body;
        private Transform _head;
        private Transform _leftArm;
        private Transform _rightArm;
        private Transform _leftLeg;
        private Transform _rightLeg;
        private ShadowCloneKind _kind = ShadowCloneKind.Melee;
        private Vector3 _bodyBasePosition;
        private Vector3 _headBasePosition;
        private Quaternion _bodyBaseRotation;
        private Quaternion _headBaseRotation;
        private Quaternion _leftArmBaseRotation;
        private Quaternion _rightArmBaseRotation;
        private Quaternion _leftLegBaseRotation;
        private Quaternion _rightLegBaseRotation;
        private Rigidbody _rigidbody;
        private float _health;
        private float _age;
        private float _attackCooldownTimer;
        private float _attackAnimationTimer;
        private float _moveAnimationTime;
        private bool _hasHitThisSwing;
        private bool _isDead;

        public void SetKind(ShadowCloneKind kind)
        {
            _kind = kind;
        }

        public void TakeDamage(float amount)
        {
            if (amount <= 0f || _health <= 0f || _isDead)
            {
                return;
            }

            _health = Mathf.Max(0f, _health - amount);
            if (_health <= 0f)
            {
                Die();
            }
        }

        private void Awake()
        {
            _health = maxHealth;
            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody != null)
            {
                _rigidbody.useGravity = true;
                _rigidbody.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }
        }

        private void Start()
        {
            CacheBodyParts();
        }

        private void CacheBodyParts()
        {
            _body = transform.Find("Body");
            _head = transform.Find("Head");
            _leftArm = transform.Find("LeftArm");
            _rightArm = transform.Find("RightArm");
            _leftLeg = transform.Find("LeftLeg");
            _rightLeg = transform.Find("RightLeg");

            if (_body != null)
            {
                _bodyBasePosition = _body.localPosition;
                _bodyBaseRotation = _body.localRotation;
            }

            if (_head != null)
            {
                _headBasePosition = _head.localPosition;
                _headBaseRotation = _head.localRotation;
            }

            if (_leftArm != null)
            {
                _leftArmBaseRotation = _leftArm.localRotation;
            }

            if (_rightArm != null)
            {
                _rightArmBaseRotation = _rightArm.localRotation;
            }

            if (_leftLeg != null)
            {
                _leftLegBaseRotation = _leftLeg.localRotation;
            }

            if (_rightLeg != null)
            {
                _rightLegBaseRotation = _rightLeg.localRotation;
            }
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_age >= lifeTime)
            {
                Die();
                return;
            }

            _attackCooldownTimer = Mathf.Max(0f, _attackCooldownTimer - Time.deltaTime);
            _attackAnimationTimer = Mathf.Max(0f, _attackAnimationTimer - Time.deltaTime);

            if (TryCrushByBoss())
            {
                return;
            }

            var boss = FindBossToAttack();
            if (boss != null)
            {
                FaceBoss(boss.transform);
                MoveAroundBoss(boss.transform);
                if (_attackCooldownTimer <= 0f)
                {
                    if (_kind == ShadowCloneKind.Ranged)
                    {
                        var sqrDistance = (boss.transform.position - transform.position).sqrMagnitude;
                        if (sqrDistance <= rangedAttackRange * rangedAttackRange)
                        {
                            _attackCooldownTimer = attackCooldown;
                            _attackAnimationTimer = attackAnimationDuration;
                            _hasHitThisSwing = false;
                            FireRangedShot(boss);
                            _hasHitThisSwing = true;
                        }
                    }
                    else
                    {
                        _attackCooldownTimer = attackCooldown;
                        _attackAnimationTimer = attackAnimationDuration;
                        _hasHitThisSwing = false;
                    }
                }
            }

            if (_kind == ShadowCloneKind.Melee)
            {
                TryHitWithHand();
            }

            UpdateAttackVisual();
        }

        private GiantBossController FindBossToAttack()
        {
            GiantBossController bestBoss = null;
            var bestSqrDistance = float.PositiveInfinity;
            var bosses = FindObjectsByType<GiantBossController>(FindObjectsSortMode.None);
            for (var i = 0; i < bosses.Length; i++)
            {
                var boss = bosses[i];
                if (boss == null || boss.Health <= 0f)
                {
                    continue;
                }

                var sqrDistance = (boss.transform.position - transform.position).sqrMagnitude;
                if (sqrDistance <= bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    bestBoss = boss;
                }
            }

            return bestBoss;
        }

        private void MoveAroundBoss(Transform boss)
        {
            if (_rigidbody == null || boss == null)
            {
                return;
            }

            var offset = boss.position - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude < 0.001f)
            {
                return;
            }

            var distance = offset.magnitude;
            var directionToBoss = offset / distance;
            var moveDirection = Vector3.zero;

            if (_kind == ShadowCloneKind.Melee)
            {
                if (distance > attackStartRange - stopDistanceBuffer)
                {
                    moveDirection = directionToBoss;
                }
            }
            else
            {
                if (distance > rangedPreferredDistance)
                {
                    moveDirection = directionToBoss;
                }
            }

            var currentVelocity = _rigidbody.linearVelocity;
            var targetHorizontalVelocity = moveDirection.sqrMagnitude > 0.001f
                ? moveDirection * moveSpeed
                : Vector3.MoveTowards(new Vector3(currentVelocity.x, 0f, currentVelocity.z), Vector3.zero, moveSpeed * Time.deltaTime * 3f);
            _rigidbody.linearVelocity = new Vector3(targetHorizontalVelocity.x, currentVelocity.y, targetHorizontalVelocity.z);
        }

        private void TryHitWithHand()
        {
            if (_hasHitThisSwing || _attackAnimationTimer <= 0f)
            {
                return;
            }

            if (_attackAnimationTimer > attackAnimationDuration * 0.55f)
            {
                return;
            }

            _hasHitThisSwing = true;
            var handPosition = GetRightHandPosition();
            var colliders = Physics.OverlapSphere(handPosition, handHitRadius);
            for (var i = 0; i < colliders.Length; i++)
            {
                var boss = colliders[i].GetComponentInParent<GiantBossController>();
                if (boss != null && boss.Health > 0f)
                {
                    boss.TakeDamage(meleeAttackDamage);
                    return;
                }
            }
        }

        private void FireRangedShot(GiantBossController boss)
        {
            var spawnPosition = GetRightHandPosition();
            var targetPosition = boss.transform.position + Vector3.up * 1.5f;
            var direction = (targetPosition - spawnPosition).normalized;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = transform.forward;
            }

            var bolt = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bolt.name = "ShadowRangedBolt";
            bolt.transform.position = spawnPosition;
            bolt.transform.localScale = Vector3.one * 0.18f;

            var renderer = bolt.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = GetShardMaterial();
            }

            var collider = bolt.GetComponent<SphereCollider>();
            if (collider != null)
            {
                collider.radius = 0.5f;
            }

            bolt.AddComponent<Rigidbody>();
            var projectile = bolt.AddComponent<ShadowBoltProjectile>();
            projectile.Launch(direction * rangedProjectileSpeed, rangedAttackDamage);

            var selfCollider = GetComponent<Collider>();
            if (selfCollider != null && collider != null)
            {
                Physics.IgnoreCollision(collider, selfCollider, true);
            }
        }

        private Vector3 GetRightHandPosition()
        {
            if (_rightArm != null)
            {
                return _rightArm.TransformPoint(Vector3.down * 0.52f + Vector3.forward * 0.18f);
            }

            return transform.position + transform.forward * 0.9f + Vector3.up * 0.95f + transform.right * 0.45f;
        }

        private bool TryCrushByBoss()
        {
            var bosses = FindObjectsByType<GiantBossController>(FindObjectsSortMode.None);
            for (var i = 0; i < bosses.Length; i++)
            {
                var boss = bosses[i];
                if (boss == null || boss.Health <= 0f)
                {
                    continue;
                }

                var flatOffset = boss.transform.position - transform.position;
                flatOffset.y = 0f;
                if (flatOffset.sqrMagnitude > crushHorizontalRadius * crushHorizontalRadius)
                {
                    continue;
                }

                var bossController = boss.GetComponent<CharacterController>();
                var bossBottom = bossController != null
                    ? boss.transform.position.y + bossController.center.y - bossController.height * 0.5f
                    : boss.transform.position.y;

                if (bossBottom >= transform.position.y + crushHeightTolerance)
                {
                    Die();
                    return true;
                }
            }

            return false;
        }

        private void FaceBoss(Transform boss)
        {
            var direction = boss.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                return;
            }

            var targetRotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction.normalized, Vector3.up),
                16f * Time.deltaTime);

            if (_rigidbody != null)
            {
                _rigidbody.MoveRotation(targetRotation);
            }
            else
            {
                transform.rotation = targetRotation;
            }
        }

        private void UpdateAttackVisual()
        {
            var horizontalSpeed = 0f;
            if (_rigidbody != null)
            {
                var velocity = _rigidbody.linearVelocity;
                velocity.y = 0f;
                horizontalSpeed = velocity.magnitude;
            }

            var moveAmount = Mathf.Clamp01(horizontalSpeed / Mathf.Max(0.01f, moveSpeed));
            _moveAnimationTime += horizontalSpeed * moveAnimationSpeed * Time.deltaTime;
            var moveSwing = Mathf.Sin(_moveAnimationTime) * moveAmount;
            var moveBob = Mathf.Abs(Mathf.Sin(_moveAnimationTime)) * moveBobHeight * moveAmount;
            var attackAmount = attackAnimationDuration <= 0f ? 0f : Mathf.Clamp01(_attackAnimationTimer / attackAnimationDuration);
            var swing = Mathf.Sin(attackAmount * Mathf.PI);
            ApplyPosition(_body, _bodyBasePosition + new Vector3(0f, moveBob, 0f));
            ApplyPosition(_head, _headBasePosition + new Vector3(0f, moveBob * 0.65f, 0f));
            ApplyRotation(_body, _bodyBaseRotation * Quaternion.Euler(-swing * 5f + moveAmount * 4f, 0f, swing * 3f + moveSwing * 3f));
            ApplyRotation(_head, _headBaseRotation * Quaternion.Euler(-swing * 4f, moveSwing * 2f, 0f));
            if (_kind == ShadowCloneKind.Ranged)
            {
                ApplyRotation(_leftArm, _leftArmBaseRotation * Quaternion.Euler(-swing * 28f - moveSwing * moveArmSwing, 0f, -swing * 8f));
                ApplyRotation(_rightArm, _rightArmBaseRotation * Quaternion.Euler(-swing * 105f + moveSwing * moveArmSwing, 0f, swing * 8f));
            }
            else
            {
                ApplyRotation(_leftArm, _leftArmBaseRotation * Quaternion.Euler(-swing * 65f - moveSwing * moveArmSwing, 0f, -swing * 12f));
                ApplyRotation(_rightArm, _rightArmBaseRotation * Quaternion.Euler(-swing * 85f + moveSwing * moveArmSwing, 0f, swing * 12f));
            }

            ApplyRotation(_leftLeg, _leftLegBaseRotation * Quaternion.Euler(moveSwing * moveLegSwing, 0f, 0f));
            ApplyRotation(_rightLeg, _rightLegBaseRotation * Quaternion.Euler(-moveSwing * moveLegSwing, 0f, 0f));
        }

        private static void ApplyPosition(Transform target, Vector3 position)
        {
            if (target != null)
            {
                target.localPosition = Vector3.Lerp(target.localPosition, position, 18f * Time.deltaTime);
            }
        }

        private static void ApplyRotation(Transform target, Quaternion rotation)
        {
            if (target != null)
            {
                target.localRotation = Quaternion.Slerp(target.localRotation, rotation, 18f * Time.deltaTime);
            }
        }

        private void Die()
        {
            if (_isDead)
            {
                return;
            }

            _isDead = true;
            SpawnBreakEffect();
            Destroy(gameObject);
        }

        private void SpawnBreakEffect()
        {
            var material = GetShardMaterial();
            var shardOrigins = new[]
            {
                transform.position + Vector3.up * 1.05f,
                transform.position + Vector3.up * 1.82f,
                transform.position + transform.right * -0.66f + Vector3.up * 1.1f,
                transform.position + transform.right * 0.66f + Vector3.up * 1.1f,
                transform.position + transform.right * -0.24f + Vector3.up * 0.34f,
                transform.position + transform.right * 0.24f + Vector3.up * 0.34f,
            };

            var shardCount = 16;
            for (var i = 0; i < shardCount; i++)
            {
                var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = "ShadowShard";
                var origin = shardOrigins[i % shardOrigins.Length];
                shard.transform.position = origin + Random.insideUnitSphere * 0.12f;
                shard.transform.rotation = Random.rotation;
                shard.transform.localScale = Vector3.one * Random.Range(0.08f, 0.22f);

                var renderer = shard.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = material;
                }

                var rigidbody = shard.AddComponent<Rigidbody>();
                rigidbody.mass = 0.12f;
                rigidbody.useGravity = true;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rigidbody.linearVelocity = new Vector3(
                    Random.Range(-0.55f, 0.55f),
                    Random.Range(-0.25f, 0.15f),
                    Random.Range(-0.55f, 0.55f));
                rigidbody.angularVelocity = Random.insideUnitSphere * Random.Range(1.2f, 3.2f);
                Destroy(shard, Random.Range(1.15f, 1.8f));
            }
        }

        private static Material GetShardMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var shardColor = new Color(0.003f, 0.003f, 0.004f, 1f);
            var material = new Material(shader)
            {
                color = shardColor
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", shardColor);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(0.012f, 0.008f, 0.02f));
            }

            return material;
        }
    }
}
