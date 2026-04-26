using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PlayerBlock
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class GiantBossController : MonoBehaviour, IShadowCombatTarget
    {
        private enum BossState
        {
            Chase,
            ArmWindup,
            ArmSlam,
            SweepWindup,
            SweepSlam,
            JumpWindup,
            JumpAirborne,
            JumpLand,
            ChargeWindup,
            Charging,
            Stunned
        }

        private enum DefeatSequenceState
        {
            None,
            Rising,
            Falling,
            ReadyForClear,
            Cleared
        }

        [Header("Health")]
        [SerializeField] private float maxHealth = 180f;
        [SerializeField] private float phaseTwoHealthPercent = 0.66f;
        [SerializeField] private float phaseThreeHealthPercent = 0.33f;

        [Header("Movement")]
        [SerializeField] private float phaseOneSpeed = 3.2f;
        [SerializeField] private float phaseTwoSpeed = 3.8f;
        [SerializeField] private float phaseThreeSpeed = 4.4f;
        [SerializeField] private float turnSpeed = 7f;
        [SerializeField] private float gravity = -32f;

        [Header("Phase 1 Arm Slam")]
        [SerializeField] private float armSlamRange = 2.45f;
        [SerializeField] private float armSlamRadius = 1.1f;
        [SerializeField] private float armSlamWindup = 0.65f;
        [SerializeField] private float armSlamRecover = 0.55f;
        [SerializeField] private float armSlamCooldown = 1.4f;

        [Header("Combo Sweep")]
        [SerializeField] private float sweepWindup = 0.75f;
        [SerializeField] private float sweepRecover = 0.7f;
        [SerializeField] private float sweepRange = 4.65f;
        [SerializeField] private float sweepRadius = 3.25f;
        [SerializeField] private float sweepForwardDot = -0.35f;
        [SerializeField] private float sweepCenterHeight = 0.58f;
        [SerializeField] private float sweepForwardOffset = 1.2f;
        [SerializeField] private float sweepHitMaxHeight = 1.45f;

        [Header("Phase 2 Jump Slam")]
        [SerializeField] private float jumpSlamRange = 12f;
        [SerializeField] private float jumpSlamRadius = 4.5f;
        [SerializeField] private float jumpWindup = 0.55f;
        [SerializeField] private float jumpTravelTime = 0.85f;
        [SerializeField] private float jumpHeight = 7.5f;
        [SerializeField] private float jumpRecover = 0.75f;
        [SerializeField] private float jumpCooldown = 3.2f;

        [Header("Phase 3 Charge")]
        [SerializeField] private float chargeWindup = 1f;
        [SerializeField] private float chargeSpeed = 18f;
        [SerializeField] private float chargeDuration = 1.35f;
        [SerializeField] private float chargeWidth = 3.8f;
        [SerializeField] private float chargeCooldown = 3.5f;

        [Header("Visual")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform rightArm;
        [SerializeField] private Transform body;
        [SerializeField] private Transform head;
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform rightLeg;
        [SerializeField] private float visualSharpness = 10f;

        [Header("Defeat")]
        [SerializeField] private Transform defeatSeal;
        [SerializeField] private float headRiseHeight = 10.5f;
        [SerializeField] private float headRiseSpeed = 15f;
        [SerializeField] private float headDropSpeed = 22f;
        [SerializeField] private float headLandingHeight = 0.92f;
        [SerializeField] private float clearInteractDistance = 3.2f;

        private static readonly List<GiantBossController> ActiveBosses = new List<GiantBossController>(2);
        private static readonly Collider[] TargetHitBuffer = new Collider[64];

        private readonly HashSet<Collider> _chargeHits = new HashSet<Collider>();
        private CharacterController _controller;
        private Collider[] _cachedColliders = System.Array.Empty<Collider>();
        private BossState _state;
        private Transform _target;
        private Vector3 _verticalVelocity;
        private Vector3 _jumpStart;
        private Vector3 _jumpEnd;
        private Vector3 _chargeDirection;
        private Vector3 _bodyBasePosition;
        private Vector3 _headBasePosition;
        private Vector3 _bodyBaseScale = Vector3.one;
        private Quaternion _leftArmBaseRotation = Quaternion.identity;
        private Quaternion _rightArmBaseRotation = Quaternion.identity;
        private Quaternion _bodyBaseRotation = Quaternion.identity;
        private Quaternion _headBaseRotation = Quaternion.identity;
        private Quaternion _leftLegBaseRotation = Quaternion.identity;
        private Quaternion _rightLegBaseRotation = Quaternion.identity;
        private float _health;
        private float _stateTimer;
        private float _armCooldownTimer;
        private float _jumpCooldownTimer;
        private float _chargeCooldownTimer;
        private float _walkCycle;
        private float _walkAmount;
        private int _normalArmSlams;
        private int _phase = 1;
        private DefeatSequenceState _defeatSequenceState;
        private GameObject _fallenHeadObject;
        private Transform _fallenHeadTransform;
        private Vector3 _headRiseTarget;
        private Vector3 _headLandTarget;
        private bool _deathSequenceStarted;

        public float Health => _health;
        public float MaxHealth => maxHealth;
        public CharacterController Controller => _controller;
        public Collider[] CachedColliders => _cachedColliders;
        public static IReadOnlyList<GiantBossController> ActiveInstances => ActiveBosses;
        public bool IsTargetAlive => _health > 0f;
        public Transform TargetTransform => transform;

        public void TakeDamage(float amount)
        {
            if (amount <= 0f || _health <= 0f)
            {
                return;
            }

            _health = Mathf.Max(0f, _health - amount);
            CombatVfxUtility.SpawnDamageNumber(GetDamageNumberPosition(), amount, new Color(1f, 0.84f, 0.34f, 1f));
            UpdateHealthBar();
            UpdatePhase();

            if (_health <= 0f)
            {
                BeginDefeatSequence();
            }
        }

        public Vector3 GetAimPoint()
        {
            return GetDamageNumberPosition();
        }

        public bool TryGetClosestPoint(Vector3 fromPosition, out Vector3 closestPoint)
        {
            closestPoint = fromPosition;
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

        private Vector3 GetDamageNumberPosition()
        {
            if (head != null)
            {
                return head.position + Vector3.up * 0.72f;
            }

            return transform.position + Vector3.up * 2.35f;
        }

        private void OnEnable()
        {
            if (!ActiveBosses.Contains(this))
            {
                ActiveBosses.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveBosses.Remove(this);
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            RefreshCachedColliders();
            _health = maxHealth;

            if (visualRoot == null)
            {
                visualRoot = transform.Find("Visual");
            }

            if (visualRoot != null)
            {
                body = body != null ? body : visualRoot.Find("Body");
                head = head != null ? head : visualRoot.Find("Head");
                leftArm = leftArm != null ? leftArm : visualRoot.Find("LeftArm");
                rightArm = rightArm != null ? rightArm : visualRoot.Find("RightArm");
                leftLeg = leftLeg != null ? leftLeg : visualRoot.Find("LeftLeg");
                rightLeg = rightLeg != null ? rightLeg : visualRoot.Find("RightLeg");
            }

            if (body != null)
            {
                _bodyBasePosition = body.localPosition;
                _bodyBaseScale = body.localScale;
                _bodyBaseRotation = body.localRotation;
            }

            if (head != null)
            {
                _headBasePosition = head.localPosition;
                _headBaseRotation = head.localRotation;
            }

            if (leftArm != null)
            {
                _leftArmBaseRotation = leftArm.localRotation;
            }

            if (rightArm != null)
            {
                _rightArmBaseRotation = rightArm.localRotation;
            }

            if (leftLeg != null)
            {
                _leftLegBaseRotation = leftLeg.localRotation;
            }

            if (rightLeg != null)
            {
                _rightLegBaseRotation = rightLeg.localRotation;
            }

            UpdateHealthBar();
            UpdatePhase();
        }

        private void OnTransformChildrenChanged()
        {
            RefreshCachedColliders();
        }

        private void RefreshCachedColliders()
        {
            _cachedColliders = GetComponentsInChildren<Collider>(true);
        }

        private void Update()
        {
            if (_health <= 0f)
            {
                UpdateDefeatSequence(Time.deltaTime);
                UpdateVisuals(Time.deltaTime);
                return;
            }

            var deltaTime = Time.deltaTime;
            _armCooldownTimer = Mathf.Max(0f, _armCooldownTimer - deltaTime);
            _jumpCooldownTimer = Mathf.Max(0f, _jumpCooldownTimer - deltaTime);
            _chargeCooldownTimer = Mathf.Max(0f, _chargeCooldownTimer - deltaTime);
            _walkAmount = 0f;

            _target = FindBestTarget();
            _stateTimer += deltaTime;

            switch (_state)
            {
                case BossState.Chase:
                    UpdateChase(deltaTime);
                    break;
                case BossState.ArmWindup:
                    UpdateArmWindup();
                    break;
                case BossState.ArmSlam:
                    UpdateRecover(BossState.Chase, armSlamRecover);
                    break;
                case BossState.SweepWindup:
                    UpdateSweepWindup();
                    break;
                case BossState.SweepSlam:
                    UpdateRecover(BossState.Chase, sweepRecover);
                    break;
                case BossState.JumpWindup:
                    UpdateJumpWindup();
                    break;
                case BossState.JumpAirborne:
                    UpdateJumpAirborne();
                    break;
                case BossState.JumpLand:
                    UpdateRecover(BossState.Chase, jumpRecover);
                    break;
                case BossState.ChargeWindup:
                    UpdateChargeWindup();
                    break;
                case BossState.Charging:
                    UpdateCharging(deltaTime);
                    break;
                case BossState.Stunned:
                    UpdateRecover(BossState.Chase, 0.5f);
                    break;
            }

            ApplyGravity(deltaTime);
            UpdateVisuals(deltaTime);
        }

        private void UpdateChase(float deltaTime)
        {
            if (_target == null)
            {
                return;
            }

            var toTarget = Flatten(_target.position - transform.position);
            if (toTarget.sqrMagnitude < 0.01f)
            {
                return;
            }

            FaceDirection(toTarget, deltaTime);

            var distance = toTarget.magnitude;
            if (_phase >= 3 && _chargeCooldownTimer <= 0f && distance > 5f)
            {
                EnterChargeWindup(toTarget.normalized);
                return;
            }

            if (_phase >= 2 && _jumpCooldownTimer <= 0f && distance <= jumpSlamRange && distance > armSlamRange * 0.75f)
            {
                EnterJumpWindup();
                return;
            }

            if (_armCooldownTimer <= 0f && TargetIsInArmReach(_target))
            {
                if (_normalArmSlams >= 2)
                {
                    EnterSweepWindup();
                }
                else
                {
                    EnterArmWindup();
                }

                return;
            }

            var move = toTarget.normalized * GetMoveSpeed() * deltaTime;
            _controller.Move(move);
            _walkAmount = Mathf.Clamp01(move.magnitude / Mathf.Max(0.001f, GetMoveSpeed() * deltaTime));
            _walkCycle += GetMoveSpeed() * 1.7f * deltaTime;
        }

        private void EnterArmWindup()
        {
            _state = BossState.ArmWindup;
            _stateTimer = 0f;
            _armCooldownTimer = armSlamCooldown;
        }

        private void UpdateArmWindup()
        {
            if (_target != null)
            {
                FaceDirection(Flatten(_target.position - transform.position), Time.deltaTime);
            }

            if (_stateTimer >= armSlamWindup)
            {
                if (TargetIsInArmReach(_target))
                {
                    var impactCenter = transform.position + transform.forward * 1.55f + Vector3.up * 1.25f;
                    HitTargetsInRadius(impactCenter, armSlamRadius);
                }

                _normalArmSlams++;
                _state = BossState.ArmSlam;
                _stateTimer = 0f;
            }
        }

        private void EnterSweepWindup()
        {
            _state = BossState.SweepWindup;
            _stateTimer = 0f;
            _armCooldownTimer = armSlamCooldown;
        }

        private void UpdateSweepWindup()
        {
            if (_target != null)
            {
                FaceDirection(Flatten(_target.position - transform.position), Time.deltaTime);
            }

            if (_stateTimer >= sweepWindup)
            {
                HitTargetsInSweepArc();
                _normalArmSlams = 0;
                _state = BossState.SweepSlam;
                _stateTimer = 0f;
            }
        }

        private void EnterJumpWindup()
        {
            _state = BossState.JumpWindup;
            _stateTimer = 0f;
            _jumpCooldownTimer = jumpCooldown;
            _jumpStart = transform.position;
            _jumpEnd = _target != null ? _target.position : transform.position + transform.forward * 6f;
        }

        private void UpdateJumpWindup()
        {
            if (_target != null)
            {
                _jumpEnd = _target.position;
                FaceDirection(Flatten(_jumpEnd - transform.position), Time.deltaTime);
            }

            if (_stateTimer >= jumpWindup)
            {
                _state = BossState.JumpAirborne;
                _stateTimer = 0f;
            }
        }

        private void UpdateJumpAirborne()
        {
            var t = Mathf.Clamp01(_stateTimer / jumpTravelTime);
            var arc = Mathf.Sin(t * Mathf.PI) * jumpHeight;
            var nextPosition = Vector3.Lerp(_jumpStart, _jumpEnd, t) + Vector3.up * arc;
            var delta = nextPosition - transform.position;
            _controller.Move(delta);

            if (t >= 1f)
            {
                HitTargetsInRadius(transform.position, jumpSlamRadius);
                _state = BossState.JumpLand;
                _stateTimer = 0f;
                _verticalVelocity = Vector3.zero;
            }
        }

        private void EnterChargeWindup(Vector3 direction)
        {
            _state = BossState.ChargeWindup;
            _stateTimer = 0f;
            _chargeCooldownTimer = chargeCooldown;
            _chargeDirection = direction;
            _chargeHits.Clear();
        }

        private void UpdateChargeWindup()
        {
            if (_target != null)
            {
                _chargeDirection = Flatten(_target.position - transform.position).normalized;
            }

            FaceDirection(_chargeDirection, Time.deltaTime);
            if (_stateTimer >= chargeWindup)
            {
                _state = BossState.Charging;
                _stateTimer = 0f;
            }
        }

        private void UpdateCharging(float deltaTime)
        {
            FaceDirection(_chargeDirection, deltaTime);
            _controller.Move(_chargeDirection * chargeSpeed * deltaTime);
            HitTargetsInChargeLine();

            if (_stateTimer >= chargeDuration)
            {
                _state = BossState.Stunned;
                _stateTimer = 0f;
            }
        }

        private void UpdateRecover(BossState nextState, float recoverTime)
        {
            if (_stateTimer >= recoverTime)
            {
                _state = nextState;
                _stateTimer = 0f;
            }
        }

        private void BeginDefeatSequence()
        {
            if (_deathSequenceStarted)
            {
                return;
            }

            _deathSequenceStarted = true;
            _state = BossState.Stunned;
            _stateTimer = 0f;
            _walkAmount = 0f;
            _verticalVelocity = Vector3.zero;
            _target = null;
            CombatHud.Instance.SetBossHealth(0f, 0f);
            CombatHud.Instance.SetStatusMessage("THE GIANT HAS FALLEN", true);

            if (defeatSeal == null)
            {
                var sealObject = GameObject.Find("seal");
                if (sealObject != null)
                {
                    defeatSeal = sealObject.transform;
                }
            }

            _fallenHeadTransform = defeatSeal;
            _fallenHeadObject = _fallenHeadTransform != null ? _fallenHeadTransform.gameObject : null;
            var sealStartHeight = _fallenHeadTransform != null ? _fallenHeadTransform.position.y : transform.position.y + headRiseHeight;
            _headRiseTarget = new Vector3(
                transform.position.x,
                Mathf.Max(transform.position.y + headRiseHeight, sealStartHeight),
                transform.position.z);
            _headLandTarget = transform.position + Vector3.up * headLandingHeight;
            _defeatSequenceState = _fallenHeadTransform != null
                ? DefeatSequenceState.Rising
                : DefeatSequenceState.ReadyForClear;
        }

        private void UpdateDefeatSequence(float deltaTime)
        {
            switch (_defeatSequenceState)
            {
                case DefeatSequenceState.Rising:
                    UpdateFallenHeadRise(deltaTime);
                    break;
                case DefeatSequenceState.Falling:
                    UpdateFallenHeadFall(deltaTime);
                    break;
                case DefeatSequenceState.ReadyForClear:
                    UpdateClearInteraction();
                    break;
                case DefeatSequenceState.Cleared:
                    break;
            }
        }

        private void UpdateFallenHeadRise(float deltaTime)
        {
            if (_fallenHeadTransform == null)
            {
                _defeatSequenceState = DefeatSequenceState.ReadyForClear;
                return;
            }

            _fallenHeadTransform.position = Vector3.MoveTowards(
                _fallenHeadTransform.position,
                _headRiseTarget,
                headRiseSpeed * deltaTime);
            _fallenHeadTransform.rotation = Quaternion.Slerp(_fallenHeadTransform.rotation, Quaternion.identity, 8f * deltaTime);

            if (Vector3.Distance(_fallenHeadTransform.position, _headRiseTarget) <= 0.08f)
            {
                _defeatSequenceState = DefeatSequenceState.Falling;
            }
        }

        private void UpdateFallenHeadFall(float deltaTime)
        {
            if (_fallenHeadTransform == null)
            {
                _defeatSequenceState = DefeatSequenceState.ReadyForClear;
                return;
            }

            _fallenHeadTransform.position = Vector3.MoveTowards(
                _fallenHeadTransform.position,
                _headLandTarget,
                headDropSpeed * deltaTime);
            _fallenHeadTransform.rotation = Quaternion.Slerp(_fallenHeadTransform.rotation, Quaternion.identity, 12f * deltaTime);

            if (Vector3.Distance(_fallenHeadTransform.position, _headLandTarget) <= 0.06f)
            {
                _fallenHeadTransform.position = _headLandTarget;
                _fallenHeadTransform.rotation = Quaternion.identity;
                CombatVfxUtility.SpawnDustBurst(_headLandTarget, Vector3.up, 0.8f, 14);
                CombatHud.Instance.SetStatusMessage("APPROACH THE SEAL AND PRESS E", true);
                _defeatSequenceState = DefeatSequenceState.ReadyForClear;
            }
        }

        private void UpdateClearInteraction()
        {
            var interactionPoint = _fallenHeadTransform != null ? _fallenHeadTransform.position : _headLandTarget;
            var hasNearbyPlayer = false;
            var players = BlockPlayerController.ActiveInstances;
            for (var i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (player == null || player.Health <= 0f)
                {
                    continue;
                }

                if (Vector3.Distance(player.transform.position, interactionPoint) <= clearInteractDistance)
                {
                    hasNearbyPlayer = true;
                    break;
                }
            }

            CombatHud.Instance.SetStatusMessage(hasNearbyPlayer ? "PRESS E TO CLEAR" : "APPROACH THE SEAL AND PRESS E", true);
            if (hasNearbyPlayer && InteractPressed())
            {
                CombatHud.Instance.SetStatusMessage("STAGE CLEAR", true);
                _defeatSequenceState = DefeatSequenceState.Cleared;
            }
        }

        private void ApplyGravity(float deltaTime)
        {
            if (_state == BossState.JumpAirborne)
            {
                return;
            }

            if (_controller.isGrounded && _verticalVelocity.y < 0f)
            {
                _verticalVelocity.y = -2f;
            }

            _verticalVelocity.y += gravity * deltaTime;
            _controller.Move(_verticalVelocity * deltaTime);
        }

        private Transform FindBestTarget()
        {
            Transform bestTarget = null;
            var bestSqrDistance = float.PositiveInfinity;

            var players = BlockPlayerController.ActiveInstances;
            for (var i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (player != null && player.Health > 0f)
                {
                    ConsiderTarget(player.transform, ref bestTarget, ref bestSqrDistance);
                }
            }

            var shadows = ShadowCloneTarget.ActiveInstances;
            for (var i = 0; i < shadows.Count; i++)
            {
                var shadow = shadows[i];
                if (shadow != null && shadow.IsAlive)
                {
                    ConsiderTarget(shadow.transform, ref bestTarget, ref bestSqrDistance);
                }
            }

            return bestTarget;
        }

        private void ConsiderTarget(Transform candidate, ref Transform bestTarget, ref float bestSqrDistance)
        {
            if (candidate == null)
            {
                return;
            }

            var sqrDistance = (candidate.position - transform.position).sqrMagnitude;
            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                bestTarget = candidate;
            }
        }

        private void HitTargetsInRadius(Vector3 center, float radius)
        {
            var hitCount = Physics.OverlapSphereNonAlloc(center, radius, TargetHitBuffer, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (TryBlockByShield(TargetHitBuffer, hitCount))
            {
                return;
            }

            for (var i = 0; i < hitCount; i++)
            {
                HitTargetCollider(TargetHitBuffer[i]);
            }
        }

        private void HitTargetsInSweepArc()
        {
            var center = transform.position + Vector3.up * sweepCenterHeight + transform.forward * sweepForwardOffset;
            var hitCount = Physics.OverlapSphereNonAlloc(center, sweepRadius, TargetHitBuffer, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (TryBlockByShield(TargetHitBuffer, hitCount))
            {
                return;
            }

            for (var i = 0; i < hitCount; i++)
            {
                var targetCollider = TargetHitBuffer[i];
                if (targetCollider == null || targetCollider.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (targetCollider.bounds.min.y > transform.position.y + sweepHitMaxHeight)
                {
                    continue;
                }

                var toTarget = targetCollider.transform.position - transform.position;
                var flatTarget = Flatten(toTarget);
                if (flatTarget.magnitude > sweepRange)
                {
                    continue;
                }

                if (flatTarget.sqrMagnitude > 0.001f && Vector3.Dot(transform.forward, flatTarget.normalized) < sweepForwardDot)
                {
                    continue;
                }

                HitTargetCollider(targetCollider);
            }
        }

        private void HitTargetsInChargeLine()
        {
            var center = transform.position + transform.forward * 1.4f + Vector3.up * 1.2f;
            var halfExtents = new Vector3(chargeWidth * 0.5f, 1.5f, 1.4f);
            var hitCount = Physics.OverlapBoxNonAlloc(center, halfExtents, TargetHitBuffer, transform.rotation, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (TryBlockByShield(TargetHitBuffer, hitCount))
            {
                return;
            }

            for (var i = 0; i < hitCount; i++)
            {
                if (_chargeHits.Add(TargetHitBuffer[i]))
                {
                    HitTargetCollider(TargetHitBuffer[i]);
                }
            }
        }

        private void HitTargetCollider(Collider targetCollider)
        {
            if (targetCollider == null || targetCollider.transform.IsChildOf(transform))
            {
                return;
            }

            if (targetCollider.GetComponentInParent<ShadowMinionShield>() != null)
            {
                CombatVfxUtility.SpawnImpactBurst(targetCollider.bounds.center, transform.forward, new Color(0.08f, 0.05f, 0.12f, 1f), 0.18f, 5);
                return;
            }

            var shadow = targetCollider.GetComponentInParent<ShadowCloneTarget>();
            if (shadow != null)
            {
                if (shadow.IsShield && !shadow.IsShieldBroken)
                {
                    shadow.TryBlockIncomingAttack(transform.position, targetCollider.bounds.center);
                    return;
                }

                shadow.TakeDamage(1f);
                CombatVfxUtility.SpawnImpactBurst(targetCollider.bounds.center, transform.forward, new Color(0.08f, 0.05f, 0.12f, 1f), 0.22f, 5);
                return;
            }

            var player = targetCollider.GetComponentInParent<BlockPlayerController>();
            if (player != null)
            {
                player.TakeDamage(GetContactDamage());
                var pushDirection = Flatten(player.transform.position - transform.position).normalized;
                if (pushDirection.sqrMagnitude > 0.01f)
                {
                    player.transform.position += pushDirection * 1.2f;
                }

                CombatVfxUtility.SpawnDustBurst(targetCollider.bounds.center, Flatten(transform.forward), 0.28f, 6);
            }
        }

        private bool TryBlockByShield(Collider[] colliders, int hitCount)
        {
            for (var i = 0; i < hitCount; i++)
            {
                var targetCollider = colliders[i];
                if (targetCollider == null)
                {
                    continue;
                }

                var shadow = targetCollider.GetComponentInParent<ShadowCloneTarget>();
                if (shadow != null
                    && shadow.IsShield
                    && !shadow.IsShieldBroken
                    && shadow.IsAlive
                    && shadow.TryBlockIncomingAttack(transform.position, targetCollider.bounds.center))
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdatePhase()
        {
            var healthPercent = maxHealth <= 0f ? 0f : _health / maxHealth;
            if (healthPercent <= phaseThreeHealthPercent)
            {
                _phase = 3;
            }
            else if (healthPercent <= phaseTwoHealthPercent)
            {
                _phase = 2;
            }
            else
            {
                _phase = 1;
            }

            CombatHud.Instance.SetBossPhase(_phase);
        }

        private void UpdateVisuals(float deltaTime)
        {
            var sharpness = 1f - Mathf.Exp(-visualSharpness * deltaTime);
            var bodyRotation = _bodyBaseRotation;
            var bodyPosition = _bodyBasePosition;
            var bodyScale = _bodyBaseScale;
            var headPosition = _headBasePosition;
            var headRotation = _headBaseRotation;
            var leftArmRotation = _leftArmBaseRotation;
            var rightArmRotation = _rightArmBaseRotation;
            var leftLegRotation = _leftLegBaseRotation;
            var rightLegRotation = _rightLegBaseRotation;

            switch (_state)
            {
                case BossState.Chase:
                    ApplyWalkVisuals(
                        ref bodyPosition,
                        ref bodyRotation,
                        ref headPosition,
                        ref headRotation,
                        ref leftArmRotation,
                        ref rightArmRotation,
                        ref leftLegRotation,
                        ref rightLegRotation);
                    break;
                case BossState.ArmWindup:
                    leftArmRotation *= Quaternion.Euler(-120f, 0f, -18f);
                    rightArmRotation *= Quaternion.Euler(-120f, 0f, 18f);
                    bodyRotation *= Quaternion.Euler(-10f, 0f, 0f);
                    break;
                case BossState.ArmSlam:
                    leftArmRotation *= Quaternion.Euler(35f, 0f, -16f);
                    rightArmRotation *= Quaternion.Euler(35f, 0f, 16f);
                    bodyRotation *= Quaternion.Euler(12f, 0f, 0f);
                    break;
                case BossState.SweepWindup:
                    bodyPosition += new Vector3(0f, -0.34f, 0.2f);
                    headPosition += new Vector3(0f, -0.28f, 0.3f);
                    bodyRotation *= Quaternion.Euler(34f, -24f, -7f);
                    headRotation *= Quaternion.Euler(22f, -14f, 0f);
                    leftArmRotation *= Quaternion.Euler(-112f, -24f, 84f);
                    rightArmRotation *= Quaternion.Euler(-112f, -24f, 84f);
                    break;
                case BossState.SweepSlam:
                    bodyPosition += new Vector3(0f, -0.42f, 0.36f);
                    headPosition += new Vector3(0f, -0.34f, 0.42f);
                    bodyRotation *= Quaternion.Euler(38f, 36f, 8f);
                    headRotation *= Quaternion.Euler(24f, 18f, 0f);
                    leftArmRotation *= Quaternion.Euler(-86f, 28f, -112f);
                    rightArmRotation *= Quaternion.Euler(-86f, 28f, -112f);
                    break;
                case BossState.JumpWindup:
                    bodyScale = new Vector3(_bodyBaseScale.x * 1.12f, _bodyBaseScale.y * 0.75f, _bodyBaseScale.z * 1.18f);
                    leftArmRotation *= Quaternion.Euler(-40f, 0f, -20f);
                    rightArmRotation *= Quaternion.Euler(-40f, 0f, 20f);
                    break;
                case BossState.JumpAirborne:
                    bodyRotation *= Quaternion.Euler(-18f, 0f, 0f);
                    leftArmRotation *= Quaternion.Euler(-95f, 0f, -24f);
                    rightArmRotation *= Quaternion.Euler(-95f, 0f, 24f);
                    break;
                case BossState.JumpLand:
                    bodyScale = new Vector3(_bodyBaseScale.x * 1.2f, _bodyBaseScale.y * 0.8f, _bodyBaseScale.z * 1.2f);
                    leftArmRotation *= Quaternion.Euler(25f, 0f, -10f);
                    rightArmRotation *= Quaternion.Euler(25f, 0f, 10f);
                    break;
                case BossState.ChargeWindup:
                    bodyRotation *= Quaternion.Euler(-22f, 0f, 0f);
                    headRotation *= Quaternion.Euler(-12f, 0f, 0f);
                    leftArmRotation *= Quaternion.Euler(70f, 0f, -24f);
                    rightArmRotation *= Quaternion.Euler(70f, 0f, 24f);
                    break;
                case BossState.Charging:
                    bodyRotation *= Quaternion.Euler(-34f, 0f, 0f);
                    headRotation *= Quaternion.Euler(-18f, 0f, 0f);
                    leftArmRotation *= Quaternion.Euler(95f, 0f, -28f);
                    rightArmRotation *= Quaternion.Euler(95f, 0f, 28f);
                    break;
                case BossState.Stunned:
                    bodyRotation *= Quaternion.Euler(15f, 0f, 0f);
                    break;
            }

            ApplyTransform(body, bodyPosition, bodyRotation, bodyScale, sharpness, applyPosition: true);
            ApplyTransform(head, headPosition, headRotation, Vector3.one, sharpness, applyPosition: true);
            ApplyTransform(leftArm, Vector3.zero, leftArmRotation, Vector3.one, sharpness, applyPosition: false);
            ApplyTransform(rightArm, Vector3.zero, rightArmRotation, Vector3.one, sharpness, applyPosition: false);
            ApplyTransform(leftLeg, Vector3.zero, leftLegRotation, Vector3.one, sharpness, applyPosition: false);
            ApplyTransform(rightLeg, Vector3.zero, rightLegRotation, Vector3.one, sharpness, applyPosition: false);
        }

        private void ApplyWalkVisuals(
            ref Vector3 bodyPosition,
            ref Quaternion bodyRotation,
            ref Vector3 headPosition,
            ref Quaternion headRotation,
            ref Quaternion leftArmRotation,
            ref Quaternion rightArmRotation,
            ref Quaternion leftLegRotation,
            ref Quaternion rightLegRotation)
        {
            if (_walkAmount <= 0f)
            {
                return;
            }

            var stride = Mathf.Sin(_walkCycle) * _walkAmount;
            var counterStride = -stride;
            var heavyStep = Mathf.Abs(Mathf.Sin(_walkCycle)) * _walkAmount;
            var sway = Mathf.Sin(_walkCycle * 0.5f) * _walkAmount;

            bodyPosition += new Vector3(0f, heavyStep * 0.08f, 0f);
            headPosition += new Vector3(0f, heavyStep * 0.045f, 0f);
            bodyRotation *= Quaternion.Euler(heavyStep * 3f, sway * 4f, -sway * 5f);
            headRotation *= Quaternion.Euler(-heavyStep * 2f, sway * 3f, sway * 2f);
            leftArmRotation *= Quaternion.Euler(counterStride * 34f, 0f, -8f * _walkAmount);
            rightArmRotation *= Quaternion.Euler(stride * 34f, 0f, 8f * _walkAmount);
            leftLegRotation *= Quaternion.Euler(stride * 26f, 0f, 0f);
            rightLegRotation *= Quaternion.Euler(counterStride * 26f, 0f, 0f);
        }

        private void UpdateHealthBar()
        {
            CombatHud.Instance.SetBossHealth(_health, maxHealth);
        }

        private static bool InteractPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.E);
#endif
        }

        private float GetContactDamage()
        {
            return _state == BossState.Charging ? 35f : _state == BossState.JumpLand ? 28f : _state == BossState.SweepSlam ? 24f : 18f;
        }

        private static void ApplyTransform(Transform target, Vector3 position, Quaternion rotation, Vector3 scale, float sharpness, bool applyPosition)
        {
            if (target == null)
            {
                return;
            }

            if (applyPosition)
            {
                target.localPosition = Vector3.Lerp(target.localPosition, position, sharpness);
            }

            target.localRotation = Quaternion.Slerp(target.localRotation, rotation, sharpness);
            target.localScale = Vector3.Lerp(target.localScale, scale, sharpness);
        }

        private void FaceDirection(Vector3 direction, float deltaTime)
        {
            direction = Flatten(direction);
            if (direction.sqrMagnitude < 0.001f)
            {
                return;
            }

            var targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * deltaTime);
        }

        private bool TargetIsInArmReach(Transform target)
        {
            if (target == null)
            {
                return false;
            }

            var toTarget = target.position - transform.position;
            var flatDistance = Flatten(toTarget).magnitude;
            if (flatDistance <= 0.35f)
            {
                return true;
            }

            if (flatDistance > armSlamRange)
            {
                return false;
            }

            var forwardDot = Vector3.Dot(transform.forward, Flatten(toTarget).normalized);
            return forwardDot > 0.62f;
        }

        private float GetMoveSpeed()
        {
            if (_phase >= 3)
            {
                return phaseThreeSpeed;
            }

            return _phase >= 2 ? phaseTwoSpeed : phaseOneSpeed;
        }

        private static Vector3 Flatten(Vector3 value)
        {
            value.y = 0f;
            return value;
        }
    }
}
