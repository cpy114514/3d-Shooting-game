using System.Collections.Generic;
using UnityEngine;

namespace PlayerBlock
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ShadowCloneTarget : MonoBehaviour
    {
        private enum MeleeState
        {
            Ready,
            Windup,
            Strike,
            Recovery
        }

        [SerializeField] private float maxHealth = 1f;
        [SerializeField] private float lifeTime = 18f;
        [SerializeField] private float rangedAttackRange = 18f;
        [SerializeField] private float moveSpeed = 3.6f;
        [SerializeField] private float shieldMoveSpeed = 2.15f;
        [SerializeField] private float shieldHoldRange = 2.55f;
        [SerializeField] private float rangedPreferredDistance = 14f;
        [SerializeField] private float stopDistanceBuffer = 0.25f;
        [SerializeField] private float moveAnimationSpeed = 8.5f;
        [SerializeField] private float moveBobHeight = 0.08f;
        [SerializeField] private float moveArmSwing = 48f;
        [SerializeField] private float moveLegSwing = 42f;
        [SerializeField] private float handHitRadius = 0.68f;
        [SerializeField] private float meleeApproachStopDistance = 0.85f;
        [SerializeField] private float meleeAttackStartDistance = 1.15f;
        [SerializeField] private float meleeStrikeDuration = 0.22f;
        [SerializeField] private float meleeRecoverDuration = 0.48f;
        [SerializeField] private float meleeHitConfirmDistance = 1.45f;
        [SerializeField] private float meleeHitForwardRange = 2.9f;
        [SerializeField] private float meleeHitVerticalRange = 1.65f;
        [SerializeField] private float meleeSpawnStunDuration = 0.25f;
        [SerializeField] private float meleeAttackWindupDuration = 0.3f;
        [SerializeField] private float meleeAttackDamage = 3f;
        [SerializeField] private float rangedAttackDamage = 1f;
        [SerializeField] private float rangedProjectileSpeed = 15f;
        [SerializeField] private float meleeAttackCooldown = 1.0f;
        [SerializeField] private float rangedAttackCooldown = 0.85f;
        [SerializeField] private float rangedAttackAnimationDuration = 0.22f;
        [SerializeField] private float crushHorizontalRadius = 1.15f;
        [SerializeField] private float crushHeightTolerance = 0.25f;

        private static readonly List<ShadowCloneTarget> ActiveShadows = new List<ShadowCloneTarget>(32);
        private static readonly Collider[] MeleeHitBuffer = new Collider[24];
        private static Material SharedShadowMaterial;
        private static Material SharedShardMaterial;

        private Transform _body;
        private Transform _head;
        private Transform _leftArm;
        private Transform _rightArm;
        private Transform _shield;
        private Transform _leftLeg;
        private Transform _rightLeg;
        private ShadowCloneKind _kind = ShadowCloneKind.Melee;
        private Vector3 _bodyBasePosition;
        private Vector3 _headBasePosition;
        private Quaternion _bodyBaseRotation;
        private Quaternion _headBaseRotation;
        private Quaternion _leftArmBaseRotation;
        private Quaternion _rightArmBaseRotation;
        private Vector3 _shieldBasePosition;
        private Quaternion _shieldBaseRotation;
        private Quaternion _leftLegBaseRotation;
        private Quaternion _rightLegBaseRotation;
        private Rigidbody _rigidbody;
        private Material _shadowMaterial;
        private float _spawnStunTimer;
        private float _health;
        private float _age;
        private float _attackCooldownTimer;
        private float _attackAnimationTimer;
        private float _meleeStateTimer;
        private float _moveAnimationTime;
        private MeleeState _meleeState = MeleeState.Ready;
        private bool _hasHitThisSwing;
        private bool _shieldBroken;
        private bool _isDead;

        public void SetKind(ShadowCloneKind kind)
        {
            _kind = kind;
            _spawnStunTimer = _kind == ShadowCloneKind.Melee ? meleeSpawnStunDuration : 0f;
            _meleeState = MeleeState.Ready;
            _meleeStateTimer = 0f;
            _hasHitThisSwing = false;
        }

        public bool IsShield => _kind == ShadowCloneKind.Shield;

        public bool IsShieldBroken => _shieldBroken;

        public bool IsAlive => !_isDead && _health > 0f;
        public static IReadOnlyList<ShadowCloneTarget> ActiveInstances => ActiveShadows;

        public Vector3 GetShieldBlockPoint()
        {
            if (_shield != null)
            {
                return _shield.position + transform.forward * 0.08f;
            }

            return transform.position + transform.forward * 0.58f + Vector3.up * 1.12f;
        }

        public bool TryBlockIncomingAttack(Vector3 attackOrigin, Vector3 attackPoint)
        {
            if (!IsShield || _shieldBroken)
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

            CombatVfxUtility.SpawnImpactBurst(blockPoint, direction, new Color(0.08f, 0.1f, 0.14f, 1f), 0.28f, 7);
            BreakShieldVisuals(blockPoint, direction);
            _shieldBroken = true;
            return true;
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

        private void OnEnable()
        {
            if (!ActiveShadows.Contains(this))
            {
                ActiveShadows.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveShadows.Remove(this);
        }

        private void Awake()
        {
            _health = maxHealth;
            _spawnStunTimer = _kind == ShadowCloneKind.Melee ? meleeSpawnStunDuration : 0f;
            _meleeState = MeleeState.Ready;
            _meleeStateTimer = 0f;
            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody != null)
            {
                _rigidbody.useGravity = true;
                _rigidbody.interpolation = RigidbodyInterpolation.None;
                _rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
                _rigidbody.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }

            ApplyShadowVisualTheme();
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
            _shield = transform.Find("Shield");
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

            if (_shield != null)
            {
                _shieldBasePosition = _shield.localPosition;
                _shieldBaseRotation = _shield.localRotation;
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
            _spawnStunTimer = Mathf.Max(0f, _spawnStunTimer - Time.deltaTime);
            _meleeStateTimer = Mathf.Max(0f, _meleeStateTimer - Time.deltaTime);

            if (TryCrushByBoss())
            {
                return;
            }

            if (_kind == ShadowCloneKind.Melee && _spawnStunTimer > 0f)
            {
                StopHorizontalMovement();
                UpdateAttackVisual();
                return;
            }

            var target = ShadowCombatTargetUtility.FindClosestTarget(transform.position);
            if (target != null)
            {
                FaceTarget(target.TargetTransform);
                if (_kind == ShadowCloneKind.Melee)
                {
                    UpdateMeleeCombat(target);
                }
                else
                {
                    MoveAroundTarget(target.TargetTransform);
                    if (_attackCooldownTimer <= 0f && _kind == ShadowCloneKind.Ranged)
                    {
                        var sqrDistance = (target.TargetTransform.position - transform.position).sqrMagnitude;
                        // Ranged shadows keep their distance, then fire once the target is inside their attack window.
                        if (sqrDistance <= rangedAttackRange * rangedAttackRange)
                        {
                            _attackCooldownTimer = rangedAttackCooldown;
                            _attackAnimationTimer = rangedAttackAnimationDuration;
                            _hasHitThisSwing = false;
                            FireRangedShot(target);
                            _hasHitThisSwing = true;
                        }
                    }
                }
            }
            else
            {
                StopHorizontalMovement();
                _meleeState = MeleeState.Ready;
            }

            UpdateAttackVisual();
        }

        private void UpdateMeleeCombat(IShadowCombatTarget target)
        {
            if (target == null || !target.IsTargetAlive)
            {
                StopHorizontalMovement();
                _meleeState = MeleeState.Ready;
                return;
            }

            switch (_meleeState)
            {
                case MeleeState.Windup:
                    StopHorizontalMovement();
                    if (_meleeStateTimer <= 0f)
                    {
                        BeginMeleeStrike();
                    }
                    return;
                case MeleeState.Strike:
                    StopHorizontalMovement();
                    TryHitWithHand(target);
                    if (_meleeStateTimer <= 0f)
                    {
                        BeginMeleeRecovery();
                    }
                    return;
                case MeleeState.Recovery:
                    StopHorizontalMovement();
                    if (_meleeStateTimer <= 0f)
                    {
                        _meleeState = MeleeState.Ready;
                    }
                    return;
            }

            var targetPoint = GetMeleeTargetPoint(target);
            var flatOffset = targetPoint - transform.position;
            flatOffset.y = 0f;
            var distanceToTarget = flatOffset.magnitude;

            if (distanceToTarget > meleeApproachStopDistance)
            {
                MoveTowardPoint(targetPoint);
                return;
            }

            StopHorizontalMovement();
            if (_attackCooldownTimer <= 0f && distanceToTarget <= meleeAttackStartDistance)
            {
                BeginMeleeWindup();
            }
        }

        private Vector3 GetMeleeTargetPoint(IShadowCombatTarget target)
        {
            var referencePoint = transform.position + Vector3.up * 1.05f + transform.forward * 0.25f;
            return target != null && target.TryGetClosestPoint(referencePoint, out var closestPoint)
                ? closestPoint
                : target != null
                    ? target.GetAimPoint()
                    : transform.position + transform.forward * meleeAttackStartDistance;
        }

        private void BeginMeleeWindup()
        {
            _meleeState = MeleeState.Windup;
            _meleeStateTimer = Mathf.Max(0f, meleeAttackWindupDuration);
            _attackAnimationTimer = meleeAttackWindupDuration + meleeStrikeDuration;
            _hasHitThisSwing = false;
        }

        private void BeginMeleeStrike()
        {
            _meleeState = MeleeState.Strike;
            _meleeStateTimer = Mathf.Max(0.02f, meleeStrikeDuration);
            _attackAnimationTimer = Mathf.Max(_attackAnimationTimer, meleeStrikeDuration);
        }

        private void BeginMeleeRecovery()
        {
            _meleeState = MeleeState.Recovery;
            _meleeStateTimer = Mathf.Max(0f, meleeRecoverDuration);
            _attackCooldownTimer = meleeAttackCooldown;
            _attackAnimationTimer = 0f;
        }

        private void MoveAroundTarget(Transform targetTransform)
        {
            if (_rigidbody == null || targetTransform == null)
            {
                return;
            }

            var offset = targetTransform.position - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude < 0.001f)
            {
                return;
            }

            var distance = offset.magnitude;
            var directionToBoss = offset / distance;
            var moveDirection = Vector3.zero;

            if (_kind == ShadowCloneKind.Shield)
            {
                if (distance > shieldHoldRange - stopDistanceBuffer)
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
                ? moveDirection * GetMoveSpeed()
                : Vector3.MoveTowards(new Vector3(currentVelocity.x, 0f, currentVelocity.z), Vector3.zero, GetMoveSpeed() * Time.deltaTime * 3f);
            _rigidbody.linearVelocity = new Vector3(targetHorizontalVelocity.x, currentVelocity.y, targetHorizontalVelocity.z);
        }

        private void MoveTowardPoint(Vector3 worldPoint)
        {
            if (_rigidbody == null)
            {
                return;
            }

            var offset = worldPoint - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude < 0.001f)
            {
                StopHorizontalMovement();
                return;
            }

            var currentVelocity = _rigidbody.linearVelocity;
            var targetVelocity = offset.normalized * GetMoveSpeed();
            _rigidbody.linearVelocity = new Vector3(targetVelocity.x, currentVelocity.y, targetVelocity.z);
        }

        private void StopHorizontalMovement()
        {
            if (_rigidbody == null)
            {
                return;
            }

            var currentVelocity = _rigidbody.linearVelocity;
            var targetHorizontal = Vector3.MoveTowards(
                new Vector3(currentVelocity.x, 0f, currentVelocity.z),
                Vector3.zero,
                GetMoveSpeed() * Time.deltaTime * 5f);
            _rigidbody.linearVelocity = new Vector3(targetHorizontal.x, currentVelocity.y, targetHorizontal.z);
        }

        private void BreakShieldVisuals(Vector3 blockPoint, Vector3 incomingDirection)
        {
            CombatVfxUtility.SpawnImpactBurst(blockPoint, incomingDirection, new Color(0.09f, 0.11f, 0.16f, 1f), 0.34f, 8);

            if (_shield != null)
            {
                Destroy(_shield.gameObject);
                _shield = null;
            }
        }

        private void ApplyShadowVisualTheme()
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                return;
            }

            _shadowMaterial = CreateShadowMaterial();
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].sharedMaterial = _shadowMaterial;
                }
            }
        }

        private void TryHitWithHand(IShadowCombatTarget target)
        {
            if (_hasHitThisSwing || _meleeState != MeleeState.Strike)
            {
                return;
            }

            var handPosition = GetRightHandPosition();
            if (TryDamageTargetFromDirectHandOverlap(target, handPosition))
            {
                return;
            }

            if (TryConfirmMeleeHit(target, handPosition, out var impactPoint))
            {
                ApplyMeleeDamage(target, impactPoint);
            }
        }

        private bool TryDamageTargetFromDirectHandOverlap(IShadowCombatTarget target, Vector3 handPosition)
        {
            var hitCount = Physics.OverlapSphereNonAlloc(
                handPosition,
                handHitRadius,
                MeleeHitBuffer,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Collide);

            for (var i = 0; i < hitCount; i++)
            {
                var collider = MeleeHitBuffer[i];
                if (collider != null && collider.GetComponentInParent<ShadowMinionShield>() != null)
                {
                    CombatVfxUtility.SpawnImpactBurst(collider.bounds.center, transform.forward, new Color(0.08f, 0.05f, 0.12f, 1f), 0.18f, 5);
                    return true;
                }

                var hitTarget = ShadowCombatTargetUtility.ResolveTarget(collider);
                if (hitTarget != null && ReferenceEquals(hitTarget, target) && hitTarget.IsTargetAlive)
                {
                    var impactPoint = collider.ClosestPoint(handPosition);
                    ApplyMeleeDamage(hitTarget, impactPoint);
                    return true;
                }
            }

            return false;
        }

        private bool TryConfirmMeleeHit(IShadowCombatTarget target, Vector3 handPosition, out Vector3 impactPoint)
        {
            impactPoint = handPosition;
            if (target == null || !target.IsTargetAlive)
            {
                return false;
            }

            if (!target.TryGetClosestPoint(handPosition, out impactPoint))
            {
                impactPoint = target.GetAimPoint();
            }

            if (target is ShadowMinionController minion && minion.HasShield)
            {
                var flatFromShadowToTarget = minion.transform.position - transform.position;
                flatFromShadowToTarget.y = 0f;
                if (flatFromShadowToTarget.sqrMagnitude > 0.001f && Vector3.Dot(minion.transform.forward, flatFromShadowToTarget.normalized) > 0.15f)
                {
                    return false;
                }
            }

            var handToBoss = impactPoint - handPosition;
            if (Mathf.Abs(handToBoss.y) > meleeHitVerticalRange)
            {
                return false;
            }

            var flatFromHand = handToBoss;
            flatFromHand.y = 0f;
            if (flatFromHand.magnitude > meleeHitConfirmDistance)
            {
                return false;
            }

            var flatFromShadow = impactPoint - transform.position;
            flatFromShadow.y = 0f;
            if (flatFromShadow.magnitude > meleeHitForwardRange)
            {
                return false;
            }

            if (flatFromShadow.sqrMagnitude <= 0.001f)
            {
                return true;
            }

            return Vector3.Dot(transform.forward, flatFromShadow.normalized) > 0.2f;
        }

        private void ApplyMeleeDamage(IShadowCombatTarget target, Vector3 impactPoint)
        {
            if (_hasHitThisSwing)
            {
                return;
            }

            _hasHitThisSwing = true;
            target.ReceiveShadowDamage(meleeAttackDamage);
            CombatVfxUtility.SpawnImpactBurst(impactPoint, transform.forward, new Color(0.09f, 0.09f, 0.11f, 1f), 0.22f, 5);
        }

        private void FireRangedShot(IShadowCombatTarget target)
        {
            var spawnPosition = GetRightHandPosition();
            var targetPosition = target != null ? target.GetAimPoint() : transform.position + transform.forward * rangedAttackRange;
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
            CombatVfxUtility.SpawnMuzzleFlash(spawnPosition, direction, 0.14f, 6);

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
                return _rightArm.TransformPoint(Vector3.down * 0.46f + Vector3.forward * 0.32f);
            }

            return transform.position + transform.forward * 0.9f + Vector3.up * 0.95f + transform.right * 0.45f;
        }

        private bool TryCrushByBoss()
        {
            var bosses = GiantBossController.ActiveInstances;
            for (var i = 0; i < bosses.Count; i++)
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

                var bossController = boss.Controller;
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

        private void FaceTarget(Transform targetTransform)
        {
            if (targetTransform == null)
            {
                return;
            }

            var direction = targetTransform.position - transform.position;
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

            var moveAmount = Mathf.Clamp01(horizontalSpeed / Mathf.Max(0.01f, GetMoveSpeed()));
            _moveAnimationTime += horizontalSpeed * GetMoveAnimationSpeed() * Time.deltaTime;
            var moveSwing = Mathf.Sin(_moveAnimationTime) * moveAmount;
            var moveBob = Mathf.Abs(Mathf.Sin(_moveAnimationTime)) * moveBobHeight * moveAmount;
            var attackDuration = GetAttackAnimationDuration();
            var attackAmount = attackDuration <= 0f ? 0f : Mathf.Clamp01(_attackAnimationTimer / attackDuration);
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
            else if (_kind == ShadowCloneKind.Shield)
            {
                ApplyRotation(_leftArm, _leftArmBaseRotation * Quaternion.Euler(-84f + moveSwing * 0.18f, 0f, 112f));
                ApplyRotation(_rightArm, _rightArmBaseRotation * Quaternion.Euler(-18f - moveSwing * 0.08f, 0f, -14f));
                if (!_shieldBroken)
                {
                    ApplyPosition(_shield, _shieldBasePosition + new Vector3(0f, moveBob * 0.2f, 0.02f));
                }
                ApplyRotation(_shield, _shieldBaseRotation * Quaternion.Euler(0f, 0f, 0f));
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

        private float GetMoveSpeed()
        {
            return _kind == ShadowCloneKind.Shield ? shieldMoveSpeed : moveSpeed;
        }

        private float GetMoveAnimationSpeed()
        {
            return _kind == ShadowCloneKind.Shield ? moveAnimationSpeed * 0.75f : moveAnimationSpeed;
        }

        private float GetAttackAnimationDuration()
        {
            return _kind == ShadowCloneKind.Melee ? meleeAttackWindupDuration + meleeStrikeDuration : rangedAttackAnimationDuration;
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

        private void OnDestroy()
        {
            _shadowMaterial = null;
        }

        private void SpawnBreakEffect()
        {
            CombatVfxUtility.SpawnImpactBurst(
                transform.position + Vector3.up * 1.05f,
                Vector3.up,
                new Color(0.015f, 0.015f, 0.018f, 1f),
                0.32f,
                8);
        }

        private static Material GetShardMaterial()
        {
            if (SharedShardMaterial != null)
            {
                return SharedShardMaterial;
            }

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

            SharedShardMaterial = material;
            return SharedShardMaterial;
        }

        private static Material CreateShadowMaterial()
        {
            if (SharedShadowMaterial != null)
            {
                return SharedShadowMaterial;
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

            SharedShadowMaterial = material;
            return SharedShadowMaterial;
        }
    }
}
