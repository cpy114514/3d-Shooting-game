using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PlayerBlock
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class BlockPlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 6.25f;
        [SerializeField] private float airControl = 0.7f;
        [SerializeField] private float acceleration = 28f;
        [SerializeField] private float deceleration = 30f;
        [SerializeField] private float turnSpeed = 16f;

        [Header("Jump")]
        [SerializeField] private float jumpHeight = 1.65f;
        [SerializeField] private float gravity = -28f;
        [SerializeField] private float coyoteTime = 0.12f;
        [SerializeField] private float jumpBufferTime = 0.12f;
        [SerializeField] private float fallGravityMultiplier = 1.25f;

        [Header("Dash")]
        [SerializeField] private float dashSpeed = 27f;
        [SerializeField] private float dashDuration = 0.48f;
        [SerializeField] private float dashDecay = 16f;
        [SerializeField] private float dashCooldown = 0.55f;

        [Header("Combat")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float shadowBulletSpeed = 24f;
        [SerializeField] private float maxShadowEnergy = 10f;
        [SerializeField] private float shadowEnergyRegenPerSecond = 1.5f;
        [SerializeField] private float meleeShadowEnergyCost = 1f;
        [SerializeField] private float rangedShadowEnergyCost = 2f;
        [SerializeField] private float shieldShadowEnergyCost = 2f;
        [SerializeField] private float shadowBulletRadius = 0.2f;
        [SerializeField] private float punchDamage = 5f;
        [SerializeField] private float punchRange = 0.72f;
        [SerializeField] private float punchRadius = 0.32f;
        [SerializeField] private float punchCooldown = 0.5f;
        [SerializeField] private float punchAnimationDuration = 0.46f;
        [SerializeField] private float shootAnimationDuration = 0.22f;
        [SerializeField] private float aimArmSharpness = 24f;

        [Header("Camera")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float cameraLookHeight = 1.75f;
        [SerializeField] private float cameraSensitivity = 0.14f;
        [SerializeField] private float cameraSharpness = 22f;
        [SerializeField] private float normalFieldOfView = 68f;
        [SerializeField] private float aimFieldOfView = 48f;
        [SerializeField] private float minCameraPitch = -89f;
        [SerializeField] private float maxCameraPitch = 89f;
        [SerializeField] private bool lockCursorOnPlay = true;

        [Header("Visual")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private float visualSharpness = 18f;
        [SerializeField] private float jumpVisualHeight = 0.18f;
        [SerializeField] private float jumpVisualTilt = 14f;
        [SerializeField] private float fallVisualTilt = -18f;
        [SerializeField] private float landingSquash = 0.16f;
        [SerializeField] private float landingRecoverSpeed = 8f;
        [SerializeField] private float walkAnimationSpeed = 1.85f;
        [SerializeField] private float walkBobHeight = 0.1f;

        private CharacterController _controller;
        private Vector3 _horizontalVelocity;
        private Vector3 _verticalVelocity;
        private Vector3 _dashVelocity;
        private float _dashTimer;
        private float _dashCooldownTimer;
        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private float _cameraYaw;
        private float _cameraPitch = 18f;
        private float _landImpact;
        private float _walkCycle;
        private float _shadowEnergy;
        private float _punchCooldownTimer;
        private float _punchAnimationTimer;
        private float _shootAnimationTimer;
        private float _aimBlend;
        private float _health;
        private CombatSelectionKind _selectedCombatKind = CombatSelectionKind.Melee;
        private MeleeComboAttack _currentMeleeCombo;
        private int _nextMeleeComboIndex;
        private bool _meleeComboHasHit;
        private Vector3 _cameraFollowOffset;
        private bool _isDashing;
        private bool _wasGrounded;
        private BodyPartPose[] _bodyPartPoses;

        public float Health => _health;
        public float MaxHealth => maxHealth;
        public float ShadowEnergy => _shadowEnergy;
        public float MaxShadowEnergy => maxShadowEnergy;

        public void TakeDamage(float amount)
        {
            if (amount <= 0f || _health <= 0f)
            {
                return;
            }

            _health = Mathf.Max(0f, _health - amount);
            CombatVfxUtility.SpawnDustBurst(transform.position + Vector3.up * 1.1f, Vector3.up, 0.32f, 6);
            UpdateHealthBar();
        }

        public void HealToFull()
        {
            _health = maxHealth;
            UpdateHealthBar();
        }

        private struct BodyPartPose
        {
            public string Name;
            public Transform Transform;
            public Vector3 StandPosition;
            public Quaternion StandRotation;
            public Vector3 StandScale;
            public Vector3 DashPosition;
            public Quaternion DashRotation;
            public Vector3 DashScale;
        }

        private enum MeleeComboAttack
        {
            RightPunch,
            HeavyPunch
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (visualRoot == null)
            {
                visualRoot = transform.Find("Visual");
            }

            if (cameraPivot == null)
            {
                cameraPivot = transform.Find("CameraPivot");
            }

            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }

            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            _cameraYaw = transform.eulerAngles.y;
            _health = maxHealth;
            _shadowEnergy = maxShadowEnergy;
            CacheBodyPartPoses();
        }

        private void Start()
        {
            UpdateHealthBar();
            UpdateEnergyBar();
            UpdateSelectedShadowHud();

            if (playerCamera != null)
            {
                var cameraEuler = playerCamera.transform.rotation.eulerAngles;
                _cameraYaw = cameraEuler.y;
                _cameraPitch = NormalizeAngle(cameraEuler.x);
                _cameraPitch = Mathf.Clamp(_cameraPitch, minCameraPitch, maxCameraPitch);

                var pivotPosition = transform.position + Vector3.up * cameraLookHeight;
                var cameraRotation = Quaternion.Euler(_cameraPitch, _cameraYaw, 0f);
                _cameraFollowOffset = Quaternion.Inverse(cameraRotation) * (playerCamera.transform.position - pivotPosition);
                normalFieldOfView = playerCamera.fieldOfView;
                playerCamera.transform.SetParent(null, true);
            }
            else
            {
                _cameraFollowOffset = new Vector3(0f, 1.8f, -4f);
            }

            if (cameraPivot != null)
            {
                cameraPivot.SetParent(null, true);
            }

            if (lockCursorOnPlay)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void Update()
        {
            if (_controller == null)
            {
                return;
            }

            if (_health <= 0f)
            {
                UpdateVisualPose(Time.deltaTime);
                return;
            }

            var deltaTime = Time.deltaTime;
            _dashCooldownTimer = Mathf.Max(0f, _dashCooldownTimer - deltaTime);
            _shadowEnergy = Mathf.Min(maxShadowEnergy, _shadowEnergy + Mathf.Max(0f, shadowEnergyRegenPerSecond) * deltaTime);
            _punchCooldownTimer = Mathf.Max(0f, _punchCooldownTimer - deltaTime);
            _punchAnimationTimer = Mathf.Max(0f, _punchAnimationTimer - deltaTime);
            _shootAnimationTimer = Mathf.Max(0f, _shootAnimationTimer - deltaTime);
            _aimBlend = Mathf.MoveTowards(_aimBlend, AimHeld() ? 1f : 0f, aimArmSharpness * deltaTime);
            _jumpBufferTimer = Mathf.Max(0f, _jumpBufferTimer - deltaTime);
            UpdateCameraInput();
            UpdateShadowSelectionInput();
            UpdateEnergyBar();
            UpdateSelectedShadowHud();

            var moveInput = ReadMoveInput();
            var wishDirection = GetCameraRelativeDirection(moveInput);
            wishDirection = Vector3.ClampMagnitude(wishDirection, 1f);

            var grounded = _controller.isGrounded;
            if (grounded && _verticalVelocity.y < 0f)
            {
                _verticalVelocity.y = -2f;
                _coyoteTimer = coyoteTime;
            }
            else
            {
                _coyoteTimer = Mathf.Max(0f, _coyoteTimer - deltaTime);
            }

            if (JumpPressed())
            {
                _jumpBufferTimer = jumpBufferTime;
            }

            if (_jumpBufferTimer > 0f && _coyoteTimer > 0f && !_isDashing)
            {
                _verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                _jumpBufferTimer = 0f;
                _coyoteTimer = 0f;
                _landImpact = 0f;
            }

            if (DashPressed() && CanStartDash(grounded))
            {
                StartDash(wishDirection);
            }

            if (FirePressed())
            {
                if (CanFireSelectedShadow())
                {
                    FireShadowBullet();
                }
                else if (_punchCooldownTimer <= 0f)
                {
                    StartMeleeComboAttack();
                }
            }

            TryResolveMeleeComboHit();

            if (_isDashing)
            {
                _dashTimer -= deltaTime;
                _dashVelocity = Vector3.MoveTowards(_dashVelocity, Vector3.zero, dashDecay * deltaTime);

                if (_dashTimer <= 0f || _dashVelocity.sqrMagnitude < 0.15f || !grounded)
                {
                    EndDash();
                }
            }

            var horizontalVelocity = _isDashing
                ? _dashVelocity
                : UpdateMoveVelocity(wishDirection, grounded, deltaTime);

            FaceActionDirection(wishDirection, horizontalVelocity, deltaTime);
            UpdateVisualPose(deltaTime);

            if (!_wasGrounded && grounded)
            {
                _landImpact = 1f;
            }

            _landImpact = Mathf.MoveTowards(_landImpact, 0f, landingRecoverSpeed * deltaTime);
            _wasGrounded = grounded;

            var gravityScale = _verticalVelocity.y < 0f ? fallGravityMultiplier : 1f;
            _verticalVelocity.y += gravity * gravityScale * deltaTime;

            var motion = (horizontalVelocity + _verticalVelocity) * deltaTime;
            _controller.Move(motion);
        }

        private void LateUpdate()
        {
            UpdateThirdPersonCamera();
        }

        private void StartDash(Vector3 wishDirection)
        {
            _isDashing = true;
            _dashTimer = dashDuration;
            _dashCooldownTimer = dashCooldown;
            _jumpBufferTimer = 0f;
            var dashDirection = wishDirection.sqrMagnitude > 0.0001f ? wishDirection.normalized : transform.forward;
            _dashVelocity = dashDirection * dashSpeed;
            _horizontalVelocity = _dashVelocity;
        }

        private void EndDash()
        {
            if (!_isDashing)
            {
                return;
            }

            _isDashing = false;
            _dashVelocity = Vector3.zero;
            _horizontalVelocity = Vector3.zero;
        }

        private Vector3 UpdateMoveVelocity(Vector3 wishDirection, bool grounded, float deltaTime)
        {
            var targetVelocity = wishDirection * (grounded ? moveSpeed : moveSpeed * airControl);
            var response = wishDirection.sqrMagnitude > 0.0001f ? acceleration : deceleration;
            if (!grounded)
            {
                response *= airControl;
            }

            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetVelocity, response * deltaTime);
            return _horizontalVelocity;
        }

        private Vector3 GetCameraRelativeDirection(Vector2 moveInput)
        {
            if (moveInput.sqrMagnitude < 0.0001f)
            {
                return Vector3.zero;
            }

            var yawRotation = Quaternion.Euler(0f, _cameraYaw, 0f);
            var forward = yawRotation * Vector3.forward;
            var right = yawRotation * Vector3.right;
            return right * moveInput.x + forward * moveInput.y;
        }

        private void FaceActionDirection(Vector3 wishDirection, Vector3 horizontalVelocity, float deltaTime)
        {
            var facingDirection = Vector3.zero;
            if (wishDirection.sqrMagnitude > 0.0001f)
            {
                facingDirection = wishDirection;
            }
            else if (_isDashing && horizontalVelocity.sqrMagnitude > 0.05f)
            {
                facingDirection = new Vector3(horizontalVelocity.x, 0f, horizontalVelocity.z);
            }
            else
            {
                facingDirection = GetCameraForward();
            }

            var targetRotation = Quaternion.LookRotation(facingDirection.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * deltaTime);
        }

        private void UpdateVisualPose(float deltaTime)
        {
            if (_bodyPartPoses == null || _bodyPartPoses.Length == 0)
            {
                return;
            }

            var sharpness = 1f - Mathf.Exp(-visualSharpness * deltaTime);
            var verticalSpeed = _verticalVelocity.y;
            var jumpAmount = Mathf.Clamp01(verticalSpeed / 7f);
            var fallAmount = Mathf.Clamp01(-verticalSpeed / 10f);
            var flatSpeed = new Vector3(_horizontalVelocity.x, 0f, _horizontalVelocity.z).magnitude;
            var walkAmount = !_isDashing && _controller.isGrounded ? Mathf.Clamp01(flatSpeed / moveSpeed) : 0f;
            _walkCycle += flatSpeed * walkAnimationSpeed * deltaTime;
            var walkPhase = _walkCycle;
            for (var i = 0; i < _bodyPartPoses.Length; i++)
            {
                var pose = _bodyPartPoses[i];
                if (pose.Transform == null)
                {
                    continue;
                }

                var targetPosition = _isDashing ? pose.DashPosition : pose.StandPosition;
                var targetRotation = _isDashing ? pose.DashRotation : pose.StandRotation;
                var targetScale = _isDashing ? pose.DashScale : pose.StandScale;

                if (!_isDashing)
                {
                    ApplyWalkAnimation(ref targetPosition, ref targetRotation, pose.Name, walkAmount, walkPhase);
                    ApplyJumpAnimation(ref targetPosition, ref targetRotation, ref targetScale, pose.Name, jumpAmount, fallAmount, _landImpact);
                }

                ApplyAimAnimation(ref targetPosition, ref targetRotation, pose.Name);
                ApplyShootAnimation(ref targetPosition, ref targetRotation, pose.Name);
                ApplyPunchAnimation(ref targetPosition, ref targetRotation, pose.Name);

                pose.Transform.localPosition = Vector3.Lerp(pose.Transform.localPosition, targetPosition, sharpness);
                pose.Transform.localRotation = Quaternion.Slerp(pose.Transform.localRotation, targetRotation, sharpness);
                pose.Transform.localScale = Vector3.Lerp(pose.Transform.localScale, targetScale, sharpness);
            }
        }

        private void FireShadowBullet()
        {
            SpendSelectedShadowEnergy();
            _shootAnimationTimer = shootAnimationDuration;

            var fireDirection = playerCamera != null ? playerCamera.transform.forward : GetCameraForward();
            fireDirection.Normalize();

            var spawnPosition = GetRightPalmPosition() + fireDirection * 0.32f;

            var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "ShadowBullet";
            projectile.transform.position = spawnPosition;
            projectile.transform.localScale = Vector3.one * (shadowBulletRadius * 2.75f);

            var renderer = projectile.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateShadowBulletMaterial();
            }

            var collider = projectile.GetComponent<SphereCollider>();
            if (collider != null)
            {
                collider.radius = 0.58f;
            }

            projectile.AddComponent<Rigidbody>();
            var shadowProjectile = projectile.AddComponent<ShadowProjectile>();
            shadowProjectile.SetCloneKind(GetSelectedCloneKind());
            shadowProjectile.Launch(fireDirection * shadowBulletSpeed, gameObject);

            CombatVfxUtility.ConfigureTrail(projectile, 0.22f, shadowBulletRadius * 1.9f);
            CombatVfxUtility.SpawnMuzzleFlash(spawnPosition, fireDirection, 0.16f, 7);
        }

        private void StartMeleeComboAttack()
        {
            _punchCooldownTimer = punchCooldown;
            _punchAnimationTimer = GetMeleeAnimationDuration();
            _currentMeleeCombo = _nextMeleeComboIndex < 2 ? MeleeComboAttack.RightPunch : MeleeComboAttack.HeavyPunch;
            _nextMeleeComboIndex = (_nextMeleeComboIndex + 1) % 3;
            _meleeComboHasHit = false;
        }

        private void TryResolveMeleeComboHit()
        {
            if (_punchAnimationTimer <= 0f || _meleeComboHasHit)
            {
                return;
            }

            var elapsed = GetMeleeAnimationElapsed01();
            if (elapsed < 0.48f)
            {
                return;
            }

            _meleeComboHasHit = true;
            var punchCenter = GetMeleeComboHitCenter();
            var colliders = Physics.OverlapSphere(punchCenter, punchRadius);
            for (var i = 0; i < colliders.Length; i++)
            {
                var boss = colliders[i].GetComponentInParent<GiantBossController>();
                if (boss != null && boss.Health > 0f)
                {
                    boss.TakeDamage(punchDamage);
                    CombatVfxUtility.SpawnImpactBurst(punchCenter, GetAimDirection(), new Color(0.08f, 0.08f, 0.1f, 1f), 0.26f, 6);
                    return;
                }
            }
        }

        private Vector3 GetMeleeComboHitCenter()
        {
            var aimDirection = GetAimDirection();
            switch (_currentMeleeCombo)
            {
                case MeleeComboAttack.HeavyPunch:
                    return GetRightPalmPosition() + aimDirection * (punchRange + 0.18f);
                default:
                    return GetRightPalmPosition() + aimDirection * punchRange;
            }
        }

        private static Material CreateShadowBulletMaterial()
        {
            return CombatVfxUtility.GetBlackBulletMaterial();
        }

        private void ApplyShootAnimation(ref Vector3 targetPosition, ref Quaternion targetRotation, string bodyPartName)
        {
            if (_shootAnimationTimer <= 0f)
            {
                return;
            }

            var amount = Mathf.Clamp01(_shootAnimationTimer / shootAnimationDuration);
            var kick = Mathf.Sin(amount * Mathf.PI);

            switch (bodyPartName)
            {
                case "Body":
                    targetRotation *= Quaternion.Euler(-kick * 5f, 0f, kick * 2f);
                    break;
                case "Head":
                    targetRotation *= Quaternion.Euler(-kick * 3f, 0f, 0f);
                    break;
                case "LeftArm":
                    targetRotation *= Quaternion.Euler(-kick * 22f, 0f, -kick * 10f);
                    targetPosition += new Vector3(0f, 0f, kick * 0.08f);
                    break;
                case "RightArm":
                    targetRotation = Quaternion.Slerp(targetRotation, GetArmAimRotation(), Mathf.Clamp01(kick * 1.2f));
                    targetRotation *= Quaternion.Euler(-kick * 8f, 0f, kick * 5f);
                    targetPosition += new Vector3(0f, 0f, kick * 0.14f);
                    break;
            }
        }

        private void ApplyPunchAnimation(ref Vector3 targetPosition, ref Quaternion targetRotation, string bodyPartName)
        {
            if (_punchAnimationTimer <= 0f)
            {
                return;
            }

            var elapsed = GetMeleeAnimationElapsed01();
            var windup = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 0.34f));
            var strike = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - 0.34f) / 0.2f));
            var recover = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - 0.72f) / 0.28f));
            var attack = Mathf.Clamp01(strike - recover);
            var recoil = Mathf.Sin(elapsed * Mathf.PI);

            switch (_currentMeleeCombo)
            {
                case MeleeComboAttack.RightPunch:
                    ApplyRightPunchAnimation(ref targetPosition, ref targetRotation, bodyPartName, windup, attack, recoil);
                    break;
                case MeleeComboAttack.HeavyPunch:
                    ApplyHeavyPunchAnimation(ref targetPosition, ref targetRotation, bodyPartName, windup, attack, recoil);
                    break;
            }
        }

        private void ApplyRightPunchAnimation(
            ref Vector3 targetPosition,
            ref Quaternion targetRotation,
            string bodyPartName,
            float windup,
            float attack,
            float recoil)
        {
            switch (bodyPartName)
            {
                case "Body":
                    targetRotation *= Quaternion.Euler(-attack * 9f + windup * 3f, attack * 9f - windup * 10f, attack * 5f);
                    targetPosition += new Vector3(0f, 0f, attack * 0.1f);
                    break;
                case "Head":
                    targetRotation *= Quaternion.Euler(-recoil * 4f, attack * 5f, 0f);
                    targetPosition += new Vector3(0f, 0f, attack * 0.04f);
                    break;
                case "RightArm":
                    targetRotation = Quaternion.Slerp(targetRotation, GetFlatArmPunchRotation(), Mathf.Clamp01(windup * 1.5f));
                    targetRotation *= Quaternion.Euler(0f, 0f, windup * 10f + attack * 4f);
                    targetPosition += new Vector3(0f, windup * 0.04f, windup * 0.24f + attack * 0.4f);
                    break;
                case "LeftLeg":
                    targetRotation *= Quaternion.Euler(attack * 8f, 0f, 0f);
                    break;
                case "RightLeg":
                    targetRotation *= Quaternion.Euler(-attack * 10f, 0f, 0f);
                    break;
            }
        }

        private void ApplyHeavyPunchAnimation(
            ref Vector3 targetPosition,
            ref Quaternion targetRotation,
            string bodyPartName,
            float windup,
            float attack,
            float recoil)
        {
            switch (bodyPartName)
            {
                case "Body":
                    targetRotation *= Quaternion.Euler(windup * 9f - attack * 17f, attack * 13f - windup * 16f, attack * 7f);
                    targetPosition += new Vector3(0f, -windup * 0.03f + attack * 0.03f, -windup * 0.1f + attack * 0.16f);
                    break;
                case "Head":
                    targetRotation *= Quaternion.Euler(windup * 5f - recoil * 6f, attack * 7f, 0f);
                    targetPosition += new Vector3(0f, 0f, attack * 0.06f);
                    break;
                case "RightArm":
                    targetRotation = Quaternion.Slerp(targetRotation, GetFlatArmPunchRotation(), Mathf.Clamp01(windup * 1.6f));
                    targetRotation *= Quaternion.Euler(0f, 0f, windup * 16f + attack * 6f);
                    targetPosition += new Vector3(0f, windup * 0.06f, windup * 0.24f + attack * 0.62f);
                    break;
                case "LeftLeg":
                    targetRotation *= Quaternion.Euler(-attack * 12f, 0f, 0f);
                    targetPosition += new Vector3(0f, 0f, -attack * 0.03f);
                    break;
                case "RightLeg":
                    targetRotation *= Quaternion.Euler(attack * 14f, 0f, 0f);
                    targetPosition += new Vector3(0f, 0f, attack * 0.03f);
                    break;
            }
        }

        private void ApplyAimAnimation(ref Vector3 targetPosition, ref Quaternion targetRotation, string bodyPartName)
        {
            if (_aimBlend <= 0f)
            {
                return;
            }

            switch (bodyPartName)
            {
                case "Body":
                    targetRotation *= Quaternion.Euler(-4f * _aimBlend, 0f, 2f * _aimBlend);
                    break;
                case "Head":
                    targetRotation *= Quaternion.Euler(-3f * _aimBlend, 0f, 0f);
                    break;
                case "LeftArm":
                    targetRotation *= Quaternion.Euler(-18f * _aimBlend, 0f, -8f * _aimBlend);
                    break;
                case "RightArm":
                    targetRotation = Quaternion.Slerp(targetRotation, GetArmAimRotation(), _aimBlend);
                    targetPosition += new Vector3(0f, 0f, 0.1f * _aimBlend);
                    break;
            }
        }

        private void ApplyWalkAnimation(
            ref Vector3 targetPosition,
            ref Quaternion targetRotation,
            string bodyPartName,
            float walkAmount,
            float walkPhase)
        {
            if (walkAmount <= 0f)
            {
                return;
            }

            var leftSwing = Mathf.Sin(walkPhase) * walkAmount;
            var rightSwing = -leftSwing;
            var bodySway = Mathf.Sin(walkPhase * 0.5f) * walkAmount;
            var bob = Mathf.Abs(Mathf.Sin(walkPhase)) * walkBobHeight * walkAmount;

            switch (bodyPartName)
            {
                case "Body":
                    targetPosition += new Vector3(0f, bob, 0f);
                    targetRotation *= Quaternion.Euler(5f * walkAmount, bodySway * 4f, bodySway * 5f);
                    break;
                case "Head":
                    targetPosition += new Vector3(0f, bob * 0.65f, 0f);
                    targetRotation *= Quaternion.Euler(-2f * walkAmount, bodySway * 3f, -bodySway * 2f);
                    break;
                case "LeftArm":
                    targetRotation *= Quaternion.Euler(rightSwing * 58f, 0f, -6f * walkAmount);
                    break;
                case "RightArm":
                    targetRotation *= Quaternion.Euler(leftSwing * 58f, 0f, 6f * walkAmount);
                    break;
                case "LeftLeg":
                    targetRotation *= Quaternion.Euler(leftSwing * 46f, 0f, 0f);
                    break;
                case "RightLeg":
                    targetRotation *= Quaternion.Euler(rightSwing * 46f, 0f, 0f);
                    break;
            }
        }

        private void ApplyJumpAnimation(
            ref Vector3 targetPosition,
            ref Quaternion targetRotation,
            ref Vector3 targetScale,
            string bodyPartName,
            float jumpAmount,
            float fallAmount,
            float landImpact)
        {
            var airLift = jumpAmount * jumpVisualHeight;
            var landDrop = landImpact * landingSquash;

            switch (bodyPartName)
            {
                case "Body":
                    targetPosition += new Vector3(0f, airLift - landDrop * 0.5f, 0f);
                    targetRotation *= Quaternion.Euler(
                        -jumpAmount * jumpVisualTilt + fallAmount * fallVisualTilt + landImpact * 10f,
                        0f,
                        0f);
                    targetScale = new Vector3(targetScale.x * (1f + landImpact * 0.06f), targetScale.y * (1f - landImpact * 0.08f), targetScale.z);
                    break;
                case "Head":
                    targetPosition += new Vector3(0f, airLift * 0.8f - landDrop * 0.35f, jumpAmount * 0.08f);
                    targetRotation *= Quaternion.Euler(-jumpAmount * 8f + fallAmount * (fallVisualTilt * -0.35f), 0f, 0f);
                    break;
                case "LeftArm":
                    targetRotation *= Quaternion.Euler(jumpAmount * 34f - fallAmount * 20f, 0f, -jumpAmount * 8f);
                    break;
                case "RightArm":
                    targetRotation *= Quaternion.Euler(jumpAmount * 34f - fallAmount * 20f, 0f, jumpAmount * 8f);
                    break;
                case "LeftLeg":
                case "RightLeg":
                    targetPosition += new Vector3(0f, landImpact * 0.04f, -jumpAmount * 0.08f);
                    targetRotation *= Quaternion.Euler(jumpAmount * 30f - fallAmount * 18f - landImpact * 8f, 0f, 0f);
                    break;
            }
        }

        private void UpdateCameraInput()
        {
            var lookDelta = ReadLookInput();
            _cameraYaw += lookDelta.x * cameraSensitivity;
            _cameraPitch = Mathf.Clamp(_cameraPitch - lookDelta.y * cameraSensitivity, minCameraPitch, maxCameraPitch);
        }

        private void UpdateThirdPersonCamera()
        {
            if (playerCamera == null)
            {
                return;
            }

            var lookHeight = cameraLookHeight;
            var pivotPosition = transform.position + Vector3.up * lookHeight;
            if (cameraPivot != null)
            {
                cameraPivot.position = pivotPosition;
                cameraPivot.rotation = Quaternion.Euler(_cameraPitch, _cameraYaw, 0f);
            }

            var cameraRotation = Quaternion.Euler(_cameraPitch, _cameraYaw, 0f);
            var desiredPosition = pivotPosition + cameraRotation * _cameraFollowOffset;
            var sharpness = 1f - Mathf.Exp(-cameraSharpness * Time.deltaTime);

            playerCamera.transform.position = Vector3.Lerp(playerCamera.transform.position, desiredPosition, sharpness);
            playerCamera.transform.rotation = Quaternion.Slerp(
                playerCamera.transform.rotation,
                cameraRotation,
                sharpness);
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, Mathf.Lerp(normalFieldOfView, aimFieldOfView, _aimBlend), sharpness);
        }

        private void UpdateHealthBar()
        {
            CombatHud.Instance.SetPlayerHealth(_health, maxHealth);
        }

        private void UpdateEnergyBar()
        {
            CombatHud.Instance.SetPlayerEnergy(_shadowEnergy, maxShadowEnergy);
        }

        private void UpdateSelectedShadowHud()
        {
            CombatHud.Instance.SetSelectedShadowInventory(
                _selectedCombatKind,
                meleeShadowEnergyCost,
                rangedShadowEnergyCost,
                shieldShadowEnergyCost);
        }

        private bool CanFireSelectedShadow()
        {
            return _selectedCombatKind != CombatSelectionKind.Hands && _shadowEnergy >= GetSelectedShadowEnergyCost();
        }

        private float GetSelectedShadowEnergyCost()
        {
            switch (_selectedCombatKind)
            {
                case CombatSelectionKind.Melee:
                    return meleeShadowEnergyCost;
                case CombatSelectionKind.Ranged:
                    return rangedShadowEnergyCost;
                case CombatSelectionKind.Shield:
                    return shieldShadowEnergyCost;
                default:
                    return 0f;
            }
        }

        private void SpendSelectedShadowEnergy()
        {
            if (_selectedCombatKind == CombatSelectionKind.Hands)
            {
                return;
            }

            _shadowEnergy = Mathf.Max(0f, _shadowEnergy - GetSelectedShadowEnergyCost());
            UpdateEnergyBar();
        }

        private void UpdateShadowSelectionInput()
        {
            var numberSelection = ReadShadowNumberSelection();
            if (numberSelection.HasValue)
            {
                SetSelectedCombatKind(numberSelection.Value);
                return;
            }

            var scroll = ReadShadowScroll();
            if (Mathf.Abs(scroll) > 0.01f)
            {
                SetSelectedCombatKind(scroll > 0f
                    ? GetPreviousCombatSelectionKind()
                    : GetNextCombatSelectionKind());
            }
        }

        private void SetSelectedCombatKind(CombatSelectionKind kind)
        {
            if (_selectedCombatKind == kind)
            {
                return;
            }

            _selectedCombatKind = kind;
            UpdateSelectedShadowHud();
        }

        private CombatSelectionKind GetNextCombatSelectionKind()
        {
            switch (_selectedCombatKind)
            {
                case CombatSelectionKind.Melee:
                    return CombatSelectionKind.Ranged;
                case CombatSelectionKind.Ranged:
                    return CombatSelectionKind.Shield;
                case CombatSelectionKind.Shield:
                    return CombatSelectionKind.Hands;
                default:
                    return CombatSelectionKind.Melee;
            }
        }

        private CombatSelectionKind GetPreviousCombatSelectionKind()
        {
            switch (_selectedCombatKind)
            {
                case CombatSelectionKind.Melee:
                    return CombatSelectionKind.Hands;
                case CombatSelectionKind.Ranged:
                    return CombatSelectionKind.Melee;
                case CombatSelectionKind.Shield:
                    return CombatSelectionKind.Ranged;
                default:
                    return CombatSelectionKind.Shield;
            }
        }

        private static float NormalizeAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }

        private void CacheBodyPartPoses()
        {
            if (visualRoot == null)
            {
                _bodyPartPoses = null;
                return;
            }

            _bodyPartPoses = new[]
            {
                CreateDashPose("Body", new Vector3(0f, 1.04f, 0.2f), Quaternion.Euler(-28f, 0f, 0f), new Vector3(0.92f, 1.05f, 0.5f)),
                CreateDashPose("Head", new Vector3(0f, 1.72f, 0.3f), Quaternion.Euler(-18f, 0f, 0f), new Vector3(0.55f, 0.55f, 0.55f)),
                CreateDashPose("LeftArm", new Vector3(-0.68f, 1.46f, -0.18f), Quaternion.Euler(62f, 0f, -12f), Vector3.one),
                CreateDashPose("RightArm", new Vector3(0.68f, 1.48f, 0.28f), Quaternion.Euler(-64f, 0f, 12f), Vector3.one),
                CreateDashPose("LeftLeg", new Vector3(-0.25f, 0.34f, 0.16f), Quaternion.Euler(-38f, 0f, 0f), new Vector3(0.32f, 0.7f, 0.32f)),
                CreateDashPose("RightLeg", new Vector3(0.25f, 0.35f, -0.22f), Quaternion.Euler(46f, 0f, 0f), new Vector3(0.32f, 0.7f, 0.32f)),
            };
        }

        private BodyPartPose CreateDashPose(string childName, Vector3 dashPosition, Quaternion dashRotation, Vector3 dashScale)
        {
            var bodyPart = visualRoot.Find(childName);
            if (bodyPart == null)
            {
                return new BodyPartPose();
            }

            return new BodyPartPose
            {
                Name = childName,
                Transform = bodyPart,
                StandPosition = bodyPart.localPosition,
                StandRotation = bodyPart.localRotation,
                StandScale = bodyPart.localScale,
                DashPosition = dashPosition,
                DashRotation = dashRotation,
                DashScale = dashScale,
            };
        }

        private Vector3 GetCameraForward()
        {
            return Quaternion.Euler(0f, _cameraYaw, 0f) * Vector3.forward;
        }

        private Vector3 GetAimDirection()
        {
            return playerCamera != null ? playerCamera.transform.forward.normalized : GetCameraForward();
        }

        private Quaternion GetArmAimRotation()
        {
            var aimDirection = GetAimDirection();
            var localAimDirection = visualRoot != null
                ? visualRoot.InverseTransformDirection(aimDirection).normalized
                : transform.InverseTransformDirection(aimDirection).normalized;

            return Quaternion.FromToRotation(Vector3.down, localAimDirection);
        }

        private Quaternion GetFlatArmPunchRotation()
        {
            var punchDirection = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (punchDirection.sqrMagnitude < 0.0001f)
            {
                punchDirection = transform.forward;
            }

            var localPunchDirection = visualRoot != null
                ? visualRoot.InverseTransformDirection(punchDirection.normalized).normalized
                : transform.InverseTransformDirection(punchDirection.normalized).normalized;

            return Quaternion.FromToRotation(Vector3.down, localPunchDirection);
        }

        private float GetMeleeAnimationDuration()
        {
            return Mathf.Max(0.18f, punchAnimationDuration);
        }

        private float GetMeleeAnimationElapsed01()
        {
            var duration = GetMeleeAnimationDuration();
            return 1f - Mathf.Clamp01(_punchAnimationTimer / duration);
        }

        private Vector3 GetRightPalmPosition()
        {
            if (visualRoot != null)
            {
                var rightArm = visualRoot.Find("RightArm");
                if (rightArm != null)
                {
                    return rightArm.TransformPoint(Vector3.down * 0.82f);
                }
            }

            return transform.position + Vector3.up * 1.35f + transform.right * 0.42f;
        }

        private bool CanStartDash(bool grounded)
        {
            if (!grounded || _isDashing || _dashCooldownTimer > 0f)
            {
                return false;
            }

            return true;
        }

        private static Vector2 ReadMoveInput()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            var x = 0f;
            if (keyboard.aKey.isPressed)
            {
                x -= 1f;
            }

            if (keyboard.dKey.isPressed)
            {
                x += 1f;
            }

            var y = 0f;
            if (keyboard.sKey.isPressed)
            {
                y -= 1f;
            }

            if (keyboard.wKey.isPressed)
            {
                y += 1f;
            }

            return new Vector2(x, y);
#else
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
        }

        private static bool JumpPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }

        private static bool DashPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            return keyboard != null && (keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
#endif
        }

        private static bool FirePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        private static bool AimHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.rightButton.isPressed;
#else
            return Input.GetMouseButton(1);
#endif
        }

        private static CombatSelectionKind? ReadShadowNumberSelection()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return null;
            }

            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
            {
                return CombatSelectionKind.Melee;
            }

            if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
            {
                return CombatSelectionKind.Ranged;
            }

            if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
            {
                return CombatSelectionKind.Shield;
            }

            if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame)
            {
                return CombatSelectionKind.Hands;
            }

            return null;
#else
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                return CombatSelectionKind.Melee;
            }

            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                return CombatSelectionKind.Ranged;
            }

            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                return CombatSelectionKind.Shield;
            }

            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
            {
                return CombatSelectionKind.Hands;
            }

            return null;
#endif
        }

        private ShadowCloneKind GetSelectedCloneKind()
        {
            switch (_selectedCombatKind)
            {
                case CombatSelectionKind.Melee:
                    return ShadowCloneKind.Melee;
                case CombatSelectionKind.Ranged:
                    return ShadowCloneKind.Ranged;
                case CombatSelectionKind.Shield:
                    return ShadowCloneKind.Shield;
                default:
                    return ShadowCloneKind.Melee;
            }
        }

        private static float ReadShadowScroll()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
#else
            return Input.mouseScrollDelta.y;
#endif
        }

        private static Vector2 ReadLookInput()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
#else
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 10f;
#endif
        }
    }
}
