using System.Collections.Generic;
using UnityEngine;

namespace PlayerBlock
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ShadowMinionController : MonoBehaviour, IShadowCombatTarget
    {
        private enum AttackState
        {
            Ready,
            Windup,
            Strike,
            Recover
        }

        [Header("Stats")]
        [SerializeField] private ShadowMinionKind minionKind = ShadowMinionKind.Grunt;
        [SerializeField] private float maxHealth = 12f;
        [SerializeField] private float moveSpeed = 2.9f;
        [SerializeField] private float attackDamage = 8f;
        [SerializeField] private float attackRange = 1.25f;
        [SerializeField] private float attackCooldown = 1.05f;
        [SerializeField] private float attackWindup = 0.32f;
        [SerializeField] private float attackStrikeDuration = 0.16f;
        [SerializeField] private float attackRecover = 0.42f;

        [Header("Ranged")]
        [SerializeField] private float rangedAttackRange = 11.5f;
        [SerializeField] private float rangedPreferredDistance = 8.2f;
        [SerializeField] private float rangedRetreatDistance = 4.6f;
        [SerializeField] private float rangedProjectileSpeed = 15f;
        [SerializeField] private float rangedProjectileScale = 0.22f;

        [Header("Tuning")]
        [SerializeField] private float targetRefreshInterval = 0.25f;
        [SerializeField] private float stopDistance = 0.82f;
        [SerializeField] private float turnSharpness = 13f;
        [SerializeField] private float hitHeightOffset = 1.05f;
        [SerializeField] private float impactScale = 0.22f;
        [SerializeField] private float rangedStopDistanceBuffer = 0.55f;

        [Header("Physics")]
        [SerializeField] private float fallGravityMultiplier = 1.8f;
        [SerializeField] private float maxFallSpeed = 24f;

        [Header("Visual")]
        [SerializeField] private float moveAnimationSpeed = 6.5f;
        [SerializeField] private float moveBobHeight = 0.07f;
        [SerializeField] private float moveArmSwing = 44f;
        [SerializeField] private float moveLegSwing = 38f;
        [SerializeField] private float shieldForwardOffset = 0.12f;
        [SerializeField] private float spearLengthMultiplier = 0.6666667f;

        private static readonly List<ShadowMinionController> ActiveMinions = new List<ShadowMinionController>(32);
        private static Material SharedMaterial;

        private Rigidbody _rigidbody;
        private Collider[] _cachedColliders = System.Array.Empty<Collider>();
        private Transform _body;
        private Transform _head;
        private Transform _leftArm;
        private Transform _rightArm;
        private Transform _leftLeg;
        private Transform _rightLeg;
        private Transform _shield;
        private Transform _spear;
        private Transform _spearTip;
        private Vector3 _bodyBasePosition;
        private Vector3 _headBasePosition;
        private Quaternion _bodyBaseRotation;
        private Quaternion _headBaseRotation;
        private Quaternion _leftArmBaseRotation;
        private Quaternion _rightArmBaseRotation;
        private Quaternion _leftLegBaseRotation;
        private Quaternion _rightLegBaseRotation;
        private Vector3 _shieldBasePosition;
        private Quaternion _shieldBaseRotation;
        private Vector3 _spearBasePosition;
        private Quaternion _spearBaseRotation;
        private BlockPlayerController _playerTarget;
        private ShadowCloneTarget _shadowTarget;
        private AttackState _attackState = AttackState.Ready;
        private float _health;
        private float _targetRefreshTimer;
        private float _attackCooldownTimer;
        private float _attackStateTimer;
        private float _attackAnimationTimer;
        private float _moveAnimationTime;
        private bool _hasHitThisAttack;
        private bool _isDead;

        public float Health => _health;
        public float MaxHealth => maxHealth;
        public ShadowMinionKind Kind => minionKind;
        public bool IsAlive => !_isDead && _health > 0f;
        public bool IsTargetAlive => IsAlive;
        public bool IsRanged => minionKind == ShadowMinionKind.Shooter;
        public bool HasShield => minionKind == ShadowMinionKind.Shielded;
        public Transform TargetTransform => transform;
        public static IReadOnlyList<ShadowMinionController> ActiveInstances => ActiveMinions;

        public void TakeDamage(float amount)
        {
            if (amount <= 0f || _isDead || _health <= 0f)
            {
                return;
            }

            _health = Mathf.Max(0f, _health - amount);
            CombatVfxUtility.SpawnDamageNumber(
                transform.position + Vector3.up * hitHeightOffset,
                amount,
                new Color(1f, 0.84f, 0.34f, 1f));
            CombatVfxUtility.SpawnImpactBurst(
                transform.position + Vector3.up * hitHeightOffset,
                Vector3.up,
                new Color(0.07f, 0.06f, 0.08f, 1f),
                impactScale,
                5);

            if (_health <= 0f)
            {
                Die();
            }
        }

        public Vector3 GetAimPoint()
        {
            return transform.position + Vector3.up * hitHeightOffset;
        }

        public bool TryGetClosestPoint(Vector3 fromPosition, out Vector3 closestPoint)
        {
            closestPoint = GetAimPoint();
            var bestDistance = float.PositiveInfinity;
            var found = false;

            for (var i = 0; i < _cachedColliders.Length; i++)
            {
                var collider = _cachedColliders[i];
                if (collider == null || !collider.enabled)
                {
                    continue;
                }

                var point = collider.ClosestPoint(fromPosition);
                var sqrDistance = (point - fromPosition).sqrMagnitude;
                if (sqrDistance < bestDistance)
                {
                    bestDistance = sqrDistance;
                    closestPoint = point;
                    found = true;
                }
            }

            return found;
        }

        public void ReceiveShadowDamage(float amount)
        {
            TakeDamage(amount);
        }

        public Vector3 GetShieldBlockPoint()
        {
            if (_shield != null)
            {
                return _shield.position + transform.forward * 0.08f;
            }

            return transform.position + transform.forward * 0.58f + Vector3.up * 1.12f;
        }

        public bool TryBlockIncomingMeleeAttack(Vector3 attackOrigin, Vector3 attackPoint)
        {
            if (!HasShield || _isDead || _health <= 0f)
            {
                return false;
            }

            var blockPoint = GetShieldBlockPoint();
            var attackVector = attackPoint - attackOrigin;
            var attackLength = attackVector.magnitude;
            if (attackLength < 0.001f)
            {
                attackVector = transform.forward;
                attackLength = 0.001f;
            }

            var direction = attackVector / attackLength;
            var toBlock = blockPoint - attackOrigin;
            var along = Vector3.Dot(toBlock, direction);
            if (along < 0f || along > attackLength)
            {
                return false;
            }

            var closestPoint = attackOrigin + direction * along;
            if (Vector3.Distance(closestPoint, blockPoint) > 0.95f)
            {
                return false;
            }

            CombatVfxUtility.SpawnImpactBurst(blockPoint, direction, new Color(0.08f, 0.1f, 0.14f, 1f), 0.22f, 6);
            return true;
        }

        private void OnEnable()
        {
            if (!ActiveMinions.Contains(this))
            {
                ActiveMinions.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveMinions.Remove(this);
        }

        private void Awake()
        {
            _health = maxHealth;
            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody != null)
            {
                ApplyRigidbodyTuning();
                _rigidbody.useGravity = true;
                _rigidbody.interpolation = RigidbodyInterpolation.None;
                _rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
                _rigidbody.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }

            RefreshCachedColliders();
            CacheBodyParts();
            ApplyVisualTheme();
        }

        private void OnTransformChildrenChanged()
        {
            RefreshCachedColliders();
        }

        private void Update()
        {
            if (_isDead)
            {
                return;
            }

            var deltaTime = Time.deltaTime;
            _targetRefreshTimer = Mathf.Max(0f, _targetRefreshTimer - deltaTime);
            _attackCooldownTimer = Mathf.Max(0f, _attackCooldownTimer - deltaTime);
            _attackStateTimer = Mathf.Max(0f, _attackStateTimer - deltaTime);
            _attackAnimationTimer = Mathf.Max(0f, _attackAnimationTimer - deltaTime);

            if (_targetRefreshTimer <= 0f || !HasValidTarget())
            {
                _targetRefreshTimer = targetRefreshInterval;
                RefreshTarget();
            }

            var targetTransform = GetCurrentTargetTransform();
            if (targetTransform == null)
            {
                StopHorizontalMovement();
                UpdateVisuals(deltaTime);
                return;
            }

            var toTarget = targetTransform.position - transform.position;
            toTarget.y = 0f;
            FaceDirection(toTarget, deltaTime);

            if (IsRanged)
            {
                UpdateRangedAttackState(toTarget);
            }
            else
            {
                UpdateMeleeAttackState(toTarget);
            }

            UpdateVisuals(deltaTime);
        }

        private void FixedUpdate()
        {
            ApplyExtraFallAcceleration();
        }

        private void RefreshCachedColliders()
        {
            _cachedColliders = GetComponentsInChildren<Collider>(true);
        }

        private void CacheBodyParts()
        {
            _body = transform.Find("Body");
            _head = transform.Find("Head");
            _leftArm = transform.Find("LeftArm");
            _rightArm = transform.Find("RightArm");
            _leftLeg = transform.Find("LeftLeg");
            _rightLeg = transform.Find("RightLeg");
            _shield = transform.Find("Shield");
            _spear = _rightArm != null ? _rightArm.Find("Spear") : null;
            _spear ??= transform.Find("Spear");
            if (HasShield && _spear != null && _rightArm != null)
            {
                if (_spear.parent != _rightArm)
                {
                    _spear.SetParent(_rightArm, false);
                }

                _spear.localPosition = GetHeldSpearLocalPosition();
                _spear.localRotation = GetHeldSpearLocalRotation();
                _spear.localScale = GetInverseLocalScale(_rightArm) * spearLengthMultiplier;
            }

            _spearTip = _spear != null ? _spear.Find("Tip") : null;

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

            if (_shield != null)
            {
                _shieldBasePosition = _shield.localPosition;
                _shieldBaseRotation = _shield.localRotation;
            }

            if (_spear != null)
            {
                _spearBasePosition = _spear.localPosition;
                _spearBaseRotation = _spear.localRotation;
            }
        }

        private void RefreshTarget()
        {
            _playerTarget = null;
            _shadowTarget = null;
            var bestSqrDistance = float.PositiveInfinity;

            var players = BlockPlayerController.ActiveInstances;
            for (var i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (player == null || player.Health <= 0f)
                {
                    continue;
                }

                var sqrDistance = (player.transform.position - transform.position).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    _playerTarget = player;
                    _shadowTarget = null;
                }
            }

            var shadows = ShadowCloneTarget.ActiveInstances;
            for (var i = 0; i < shadows.Count; i++)
            {
                var shadow = shadows[i];
                if (shadow == null || !shadow.IsAlive)
                {
                    continue;
                }

                var sqrDistance = (shadow.transform.position - transform.position).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    _shadowTarget = shadow;
                    _playerTarget = null;
                }
            }
        }

        private bool HasValidTarget()
        {
            if (_playerTarget != null && _playerTarget.Health > 0f)
            {
                return true;
            }

            return _shadowTarget != null && _shadowTarget.IsAlive;
        }

        private Transform GetCurrentTargetTransform()
        {
            if (_playerTarget != null && _playerTarget.Health > 0f)
            {
                return _playerTarget.transform;
            }

            return _shadowTarget != null && _shadowTarget.IsAlive ? _shadowTarget.transform : null;
        }

        private Vector3 GetCurrentTargetAimPoint()
        {
            if (_playerTarget != null && _playerTarget.Health > 0f)
            {
                return _playerTarget.transform.position + Vector3.up * 1.05f;
            }

            if (_shadowTarget != null && _shadowTarget.IsAlive)
            {
                return _shadowTarget.IsShield && !_shadowTarget.IsShieldBroken
                    ? _shadowTarget.GetShieldBlockPoint()
                    : _shadowTarget.transform.position + Vector3.up * 1f;
            }

            return transform.position + transform.forward * Mathf.Max(attackRange, rangedAttackRange);
        }

        private void UpdateMeleeAttackState(Vector3 flatOffsetToTarget)
        {
            switch (_attackState)
            {
                case AttackState.Windup:
                    StopHorizontalMovement();
                    if (_attackStateTimer <= 0f)
                    {
                        _attackState = AttackState.Strike;
                        _attackStateTimer = Mathf.Max(0.02f, attackStrikeDuration);
                    }
                    return;
                case AttackState.Strike:
                    StopHorizontalMovement();
                    TryHitTarget();
                    if (_attackStateTimer <= 0f)
                    {
                        _attackState = AttackState.Recover;
                        _attackStateTimer = Mathf.Max(0f, attackRecover);
                        _attackCooldownTimer = attackCooldown;
                    }
                    return;
                case AttackState.Recover:
                    StopHorizontalMovement();
                    if (_attackStateTimer <= 0f)
                    {
                        _attackState = AttackState.Ready;
                    }
                    return;
            }

            var distance = flatOffsetToTarget.magnitude;
            if (distance > stopDistance)
            {
                Move(flatOffsetToTarget);
            }
            else
            {
                StopHorizontalMovement();
            }

            if (_attackCooldownTimer <= 0f && distance <= attackRange)
            {
                BeginAttack();
            }
        }

        private void UpdateRangedAttackState(Vector3 flatOffsetToTarget)
        {
            switch (_attackState)
            {
                case AttackState.Windup:
                    StopHorizontalMovement();
                    if (_attackStateTimer <= 0f)
                    {
                        _attackState = AttackState.Strike;
                        _attackStateTimer = Mathf.Max(0.02f, attackStrikeDuration);
                    }
                    return;
                case AttackState.Strike:
                    StopHorizontalMovement();
                    FireProjectileAtTarget();
                    if (_attackStateTimer <= 0f)
                    {
                        _attackState = AttackState.Recover;
                        _attackStateTimer = Mathf.Max(0f, attackRecover);
                        _attackCooldownTimer = attackCooldown;
                    }
                    return;
                case AttackState.Recover:
                    StopHorizontalMovement();
                    if (_attackStateTimer <= 0f)
                    {
                        _attackState = AttackState.Ready;
                    }
                    return;
            }

            var distance = flatOffsetToTarget.magnitude;
            if (distance > rangedPreferredDistance + rangedStopDistanceBuffer)
            {
                Move(flatOffsetToTarget);
            }
            else if (distance < rangedRetreatDistance)
            {
                Move(-flatOffsetToTarget);
            }
            else
            {
                StopHorizontalMovement();
            }

            if (_attackCooldownTimer <= 0f && distance <= rangedAttackRange)
            {
                BeginAttack();
            }
        }

        private void BeginAttack()
        {
            _attackState = AttackState.Windup;
            _attackStateTimer = Mathf.Max(0f, attackWindup);
            _attackAnimationTimer = attackWindup + attackStrikeDuration;
            _hasHitThisAttack = false;
        }

        private void TryHitTarget()
        {
            if (_hasHitThisAttack || !HasValidTarget())
            {
                return;
            }

            var targetTransform = GetCurrentTargetTransform();
            if (targetTransform == null)
            {
                return;
            }

            if (HasShield)
            {
                TrySpearHitTarget(targetTransform);
                return;
            }

            var toTarget = targetTransform.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.magnitude > attackRange + 0.25f)
            {
                return;
            }

            if (toTarget.sqrMagnitude > 0.001f && Vector3.Dot(transform.forward, toTarget.normalized) < 0.1f)
            {
                return;
            }

            var impactPoint = GetCurrentTargetAimPoint();
            var impactDirection = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : transform.forward;
            _hasHitThisAttack = true;

            if (_shadowTarget != null && _shadowTarget.IsAlive)
            {
                if (_shadowTarget.IsShield
                    && !_shadowTarget.IsShieldBroken
                    && _shadowTarget.TryBlockIncomingAttack(transform.position + Vector3.up * hitHeightOffset, impactPoint))
                {
                    return;
                }

                _shadowTarget.TakeDamage(attackDamage);
            }
            else if (_playerTarget != null && _playerTarget.Health > 0f)
            {
                _playerTarget.TakeDamage(attackDamage, transform.position + Vector3.up * hitHeightOffset);
            }
            else
            {
                _hasHitThisAttack = false;
                return;
            }

            CombatVfxUtility.SpawnImpactBurst(
                impactPoint,
                impactDirection,
                new Color(0.08f, 0.05f, 0.1f, 1f),
                impactScale,
                5);
        }

        private void TrySpearHitTarget(Transform targetTransform)
        {
            var toTarget = targetTransform.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.magnitude > attackRange + 0.35f)
            {
                return;
            }

            if (toTarget.sqrMagnitude > 0.001f && Vector3.Dot(transform.forward, toTarget.normalized) < 0.25f)
            {
                return;
            }

            var spearStart = GetSpearStrikeStart();
            var targetPoint = GetCurrentTargetAimPoint();
            var strikeDirection = targetPoint - spearStart;
            if (strikeDirection.sqrMagnitude < 0.001f)
            {
                strikeDirection = transform.forward;
            }

            strikeDirection.Normalize();
            var spearEnd = spearStart + strikeDirection * (attackRange + 0.95f);
            var closestPoint = GetClosestPointOnSegment(spearStart, spearEnd, targetPoint);
            if (Vector3.Distance(closestPoint, targetPoint) > 0.9f)
            {
                return;
            }

            var impactDirection = strikeDirection;
            _hasHitThisAttack = true;

            if (_shadowTarget != null && _shadowTarget.IsAlive)
            {
                if (_shadowTarget.IsShield
                    && !_shadowTarget.IsShieldBroken
                    && _shadowTarget.TryBlockIncomingAttack(spearStart, closestPoint))
                {
                    return;
                }

                _shadowTarget.TakeDamage(attackDamage);
            }
            else if (_playerTarget != null && _playerTarget.Health > 0f)
            {
                _playerTarget.TakeDamage(attackDamage, spearStart);
            }
            else
            {
                _hasHitThisAttack = false;
                return;
            }

            CombatVfxUtility.SpawnImpactBurst(
                closestPoint,
                impactDirection,
                new Color(0.08f, 0.05f, 0.1f, 1f),
                impactScale,
                6);
        }

        private Vector3 GetSpearStrikeStart()
        {
            if (_spearTip != null)
            {
                var spearForward = GetSpearForwardDirection();
                return _spearTip.position - spearForward * 0.8f + transform.right * 0.1f;
            }

            if (_spear != null)
            {
                return _spear.position + _spear.forward * 0.65f + transform.right * 0.08f;
            }

            return transform.position + transform.right * 0.72f + Vector3.up * 1.28f + transform.forward * 0.45f;
        }

        private Vector3 GetSpearStrikeEnd(Vector3 spearStart)
        {
            if (_spearTip != null)
            {
                return _spearTip.position + GetSpearForwardDirection() * 0.3f;
            }

            return spearStart + transform.forward * (attackRange + 0.55f);
        }

        private Vector3 GetSpearForwardDirection()
        {
            if (_spear != null && _spearTip != null)
            {
                var direction = _spearTip.position - _spear.position;
                if (direction.sqrMagnitude > 0.001f)
                {
                    return direction.normalized;
                }
            }

            if (_spear != null)
            {
                return _spear.forward;
            }

            return transform.forward;
        }

        private static Vector3 GetClosestPointOnSegment(Vector3 start, Vector3 end, Vector3 point)
        {
            var segment = end - start;
            var lengthSquared = segment.sqrMagnitude;
            if (lengthSquared < 0.001f)
            {
                return start;
            }

            var t = Mathf.Clamp01(Vector3.Dot(point - start, segment) / lengthSquared);
            return start + segment * t;
        }

        private void FireProjectileAtTarget()
        {
            if (_hasHitThisAttack || !HasValidTarget())
            {
                return;
            }

            var spawnPosition = GetProjectileSpawnPosition();
            var targetPoint = GetCurrentTargetAimPoint();
            var direction = targetPoint - spawnPosition;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = transform.forward;
            }

            direction.Normalize();
            _hasHitThisAttack = true;

            var projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileObject.name = "ShadowEnemyBolt";
            projectileObject.transform.position = spawnPosition;
            projectileObject.transform.localScale = Vector3.one * rangedProjectileScale;

            var renderer = projectileObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CombatVfxUtility.GetBlackBulletMaterial();
            }

            var collider = projectileObject.GetComponent<SphereCollider>();
            if (collider != null)
            {
                collider.radius = 0.5f;
            }

            projectileObject.AddComponent<Rigidbody>();
            var projectile = projectileObject.AddComponent<ShadowMinionProjectile>();
            projectile.Launch(direction * rangedProjectileSpeed, attackDamage, gameObject);
            CombatVfxUtility.ConfigureTrail(projectileObject, 0.16f, Mathf.Max(0.06f, rangedProjectileScale * 0.7f));
            CombatVfxUtility.SpawnMuzzleFlash(spawnPosition, direction, 0.14f, 6);

            var selfCollider = GetComponent<Collider>();
            if (selfCollider != null && collider != null)
            {
                Physics.IgnoreCollision(collider, selfCollider, true);
            }
        }

        private Vector3 GetProjectileSpawnPosition()
        {
            if (_rightArm != null)
            {
                return _rightArm.TransformPoint(Vector3.down * 0.5f + Vector3.forward * 0.34f);
            }

            return transform.position + transform.forward * 0.85f + transform.right * 0.3f + Vector3.up * 1.05f;
        }

        private void Move(Vector3 flatDirection)
        {
            if (_rigidbody == null || flatDirection.sqrMagnitude < 0.001f)
            {
                return;
            }

            var currentVelocity = _rigidbody.linearVelocity;
            var horizontalVelocity = flatDirection.normalized * moveSpeed;
            _rigidbody.linearVelocity = new Vector3(horizontalVelocity.x, currentVelocity.y, horizontalVelocity.z);
        }

        private void StopHorizontalMovement()
        {
            if (_rigidbody == null)
            {
                return;
            }

            var currentVelocity = _rigidbody.linearVelocity;
            var horizontal = Vector3.MoveTowards(
                new Vector3(currentVelocity.x, 0f, currentVelocity.z),
                Vector3.zero,
                moveSpeed * Time.deltaTime * 5f);
            _rigidbody.linearVelocity = new Vector3(horizontal.x, currentVelocity.y, horizontal.z);
        }

        private void ApplyRigidbodyTuning()
        {
            if (_rigidbody == null)
            {
                return;
            }

            _rigidbody.mass = minionKind == ShadowMinionKind.Brute
                ? 32f
                : minionKind == ShadowMinionKind.Shielded
                    ? 22f
                    : 18f;
            _rigidbody.linearDamping = 0.15f;
            _rigidbody.angularDamping = 8f;
            _rigidbody.maxAngularVelocity = 2f;
        }

        private void ApplyExtraFallAcceleration()
        {
            if (_rigidbody == null)
            {
                return;
            }

            var velocity = _rigidbody.linearVelocity;
            if (velocity.y >= 0f)
            {
                return;
            }

            var extraGravity = Mathf.Abs(Physics.gravity.y) * Mathf.Max(0f, fallGravityMultiplier - 1f);
            velocity.y = Mathf.Max(velocity.y - extraGravity * Time.fixedDeltaTime, -maxFallSpeed);
            _rigidbody.linearVelocity = velocity;
        }

        private void FaceDirection(Vector3 flatDirection, float deltaTime)
        {
            if (flatDirection.sqrMagnitude < 0.001f)
            {
                return;
            }

            var targetRotation = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
            var rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSharpness * deltaTime);
            if (_rigidbody != null)
            {
                _rigidbody.MoveRotation(rotation);
            }
            else
            {
                transform.rotation = rotation;
            }
        }

        private void UpdateVisuals(float deltaTime)
        {
            var horizontalSpeed = 0f;
            if (_rigidbody != null)
            {
                var velocity = _rigidbody.linearVelocity;
                velocity.y = 0f;
                horizontalSpeed = velocity.magnitude;
            }

            var moveAmount = Mathf.Clamp01(horizontalSpeed / Mathf.Max(0.01f, moveSpeed));
            _moveAnimationTime += horizontalSpeed * moveAnimationSpeed * deltaTime;
            var moveSwing = Mathf.Sin(_moveAnimationTime) * moveAmount;
            var moveBob = Mathf.Abs(Mathf.Sin(_moveAnimationTime)) * moveBobHeight * moveAmount;
            var attackDuration = Mathf.Max(0.01f, attackWindup + attackStrikeDuration);
            var attackAmount = Mathf.Clamp01(_attackAnimationTimer / attackDuration);
            var swing = Mathf.Sin(attackAmount * Mathf.PI);

            ApplyPosition(_body, _bodyBasePosition + Vector3.up * moveBob);
            ApplyPosition(_head, _headBasePosition + Vector3.up * (moveBob * 0.65f));
            ApplyRotation(_body, _bodyBaseRotation * Quaternion.Euler(-swing * 8f + moveAmount * 3f, 0f, swing * 4f));
            ApplyRotation(_head, _headBaseRotation * Quaternion.Euler(-swing * 5f, moveSwing * 2f, 0f));

            if (IsRanged)
            {
                ApplyRotation(_leftArm, _leftArmBaseRotation * Quaternion.Euler(-28f - moveSwing * (moveArmSwing * 0.35f), 0f, -14f - swing * 8f));
                ApplyRotation(_rightArm, _rightArmBaseRotation * Quaternion.Euler(-112f - swing * 14f, 0f, 16f + swing * 10f));
            }
            else if (HasShield)
            {
                var spearLunge = Mathf.Clamp01(_attackAnimationTimer / Mathf.Max(0.01f, attackWindup + attackStrikeDuration));
                var spearForward = Mathf.Sin(spearLunge * Mathf.PI);
                var spearAimDirection = GetDesiredSpearAimDirection();
                ApplyRotation(_leftArm, _leftArmBaseRotation * Quaternion.Euler(-swing * 34f - moveSwing * (moveArmSwing * 0.55f), 0f, -12f - swing * 4f));
                ApplyRotation(_rightArm, _rightArmBaseRotation * Quaternion.Euler(-34f - spearForward * 48f - moveSwing * (moveArmSwing * 0.22f), 0f, 10f + spearForward * 12f));
                ApplyPosition(_shield, _shieldBasePosition + new Vector3(-0.05f - spearForward * 0.06f, moveBob * 0.3f + 0.02f, shieldForwardOffset - spearForward * 0.04f));
                ApplyRotation(_shield, _shieldBaseRotation * Quaternion.Euler(0f, 0f, Mathf.Sin(_moveAnimationTime * 0.5f) * 2f + spearForward * 3f));
                ApplyPosition(_spear, _spearBasePosition + new Vector3(0.08f + spearForward * 0.12f, 0.08f, 0.12f + spearForward * 0.5f));
                ApplyRotation(_spear, GetAimedSpearRotation(spearAimDirection, spearForward));
            }
            else
            {
                ApplyRotation(_leftArm, _leftArmBaseRotation * Quaternion.Euler(-swing * 62f - moveSwing * moveArmSwing, 0f, -swing * 14f));
                ApplyRotation(_rightArm, _rightArmBaseRotation * Quaternion.Euler(-swing * 88f + moveSwing * moveArmSwing, 0f, swing * 14f));
            }

            ApplyRotation(_leftLeg, _leftLegBaseRotation * Quaternion.Euler(moveSwing * moveLegSwing, 0f, 0f));
            ApplyRotation(_rightLeg, _rightLegBaseRotation * Quaternion.Euler(-moveSwing * moveLegSwing, 0f, 0f));
        }

        private void ApplyVisualTheme()
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            var material = GetSharedMaterial();
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].sharedMaterial = material;
                }
            }
        }

        private void Die()
        {
            if (_isDead)
            {
                return;
            }

            _isDead = true;
            CombatVfxUtility.SpawnImpactBurst(
                transform.position + Vector3.up * hitHeightOffset,
                Vector3.up,
                new Color(0.04f, 0.035f, 0.055f, 1f),
                impactScale * 1.25f,
                8);
            Destroy(gameObject);
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

        private static Vector3 GetHeldSpearLocalPosition()
        {
            return new Vector3(0.2f, -0.22f, 0.54f);
        }

        private static Quaternion GetHeldSpearLocalRotation()
        {
            return Quaternion.Euler(-8f, 8f, -2f);
        }

        private static Vector3 GetInverseLocalScale(Transform target)
        {
            var scale = target != null ? target.localScale : Vector3.one;
            return new Vector3(
                Mathf.Abs(scale.x) > 0.001f ? 1f / scale.x : 1f,
                Mathf.Abs(scale.y) > 0.001f ? 1f / scale.y : 1f,
                Mathf.Abs(scale.z) > 0.001f ? 1f / scale.z : 1f);
        }

        private Vector3 GetDesiredSpearAimDirection()
        {
            var targetTransform = GetCurrentTargetTransform();
            if (targetTransform == null)
            {
                return transform.forward;
            }

            var origin = _spear != null ? _spear.position : transform.position + Vector3.up * hitHeightOffset;
            var desiredDirection = GetCurrentTargetAimPoint() - origin;
            if (desiredDirection.sqrMagnitude < 0.001f)
            {
                return transform.forward;
            }

            return desiredDirection.normalized;
        }

        private Quaternion GetAimedSpearRotation(Vector3 desiredDirection, float spearForward)
        {
            if (_spear == null || _spear.parent == null || desiredDirection.sqrMagnitude < 0.001f)
            {
                return _spearBaseRotation * Quaternion.Euler(-6f - spearForward * 10f, 6f, -2f);
            }

            var localDirection = _spear.parent.InverseTransformDirection(desiredDirection.normalized);
            if (localDirection.sqrMagnitude < 0.001f)
            {
                return _spearBaseRotation;
            }

            var aimedRotation = Quaternion.LookRotation(localDirection.normalized, Vector3.up);
            return aimedRotation * Quaternion.Euler(-2f - spearForward * 4f, 0f, -2f);
        }

        private static Material GetSharedMaterial()
        {
            if (SharedMaterial != null)
            {
                return SharedMaterial;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var color = new Color(0.42f, 0.42f, 0.42f, 1f);
            SharedMaterial = new Material(shader)
            {
                color = color
            };

            if (SharedMaterial.HasProperty("_BaseColor"))
            {
                SharedMaterial.SetColor("_BaseColor", color);
            }

            if (SharedMaterial.HasProperty("_EmissionColor"))
            {
                SharedMaterial.EnableKeyword("_EMISSION");
                SharedMaterial.SetColor("_EmissionColor", new Color(0.02f, 0.02f, 0.02f, 1f));
            }

            return SharedMaterial;
        }
    }
}
