using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LanShooter
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class LanShooterPlayer : NetworkBehaviour
    {
        public const int MaxHealthValue = 100;

        private const float HitMarkerDuration = 0.16f;
        private const float KillMarkerDuration = 0.3f;

        private static readonly List<LanShooterPlayer> Players = new();

        [Header("References")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private Transform weaponRoot;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Renderer[] tintRenderers;
        [SerializeField] private Renderer[] localHiddenRenderers;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 8.5f;
        [SerializeField] private float groundAcceleration = 55f;
        [SerializeField] private float groundDeceleration = 60f;
        [SerializeField] private float airAcceleration = 20f;
        [SerializeField] private float airDeceleration = 8f;
        [SerializeField] private float airControlMultiplier = 0.85f;
        [SerializeField] private float jumpForce = 7.5f;
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float maxFallSpeed = 28f;
        [SerializeField] private float coyoteTime = 0.12f;
        [SerializeField] private float jumpBufferTime = 0.12f;
        [SerializeField] private float mouseSensitivity = 0.18f;

        [Header("Slide")]
        [SerializeField] private float slideInitialSpeed = 16.5f;
        [SerializeField] private float slideDuration = 0.62f;
        [SerializeField] private float slideDeceleration = 21f;
        [SerializeField] private float slideCooldown = 0.8f;
        [SerializeField] private float slideCameraDrop = 0.52f;
        [SerializeField] private float slideSteerControl = 4.5f;
        [SerializeField] private float slideStartSpeedThreshold = 4.2f;

        [Header("Crouch")]
        [SerializeField] private float crouchMoveSpeedMultiplier = 0.52f;
        [SerializeField] private float crouchControllerHeight = 1.28f;
        [SerializeField] private float crouchCameraDrop = 0.42f;
        [SerializeField] private float crouchTransitionSharpness = 14f;
        [SerializeField] private Vector3 crouchWeaponOffset = new(0f, -0.06f, -0.02f);

        [Header("Weapon")]
        [SerializeField] private float fireCooldown = 0.18f;
        [SerializeField] private int damagePerShot = 20;
        [SerializeField] private float respawnDelay = 3f;
        [SerializeField] private float projectileSpeed = 160f;
        [SerializeField] private float projectileRadius = 0.14f;
        [SerializeField] private float projectileMaxDistance = 120f;
        [SerializeField] private float fireOriginForwardOffset = 0.24f;
        [SerializeField] private float aimProbeRadius = 0.08f;

        [Header("Aim")]
        [SerializeField] private float adsFieldOfView = 57f;
        [SerializeField] private float adsSensitivityMultiplier = 0.7f;
        [SerializeField] private Vector3 adsWeaponPositionOffset = new(-0.13f, 0.06f, 0.15f);
        [SerializeField] private Vector3 adsWeaponRotationOffset = new(-1f, -4f, 0f);

        [Header("Feel")]
        [SerializeField] private float baseFieldOfView = 78f;
        [SerializeField] private float moveFovBoost = 3.5f;
        [SerializeField] private float shotFovKick = 1.4f;
        [SerializeField] private float fieldOfViewSharpness = 12f;
        [SerializeField] private float headBobAmplitude = 0.045f;
        [SerializeField] private float headBobFrequency = 13f;
        [SerializeField] private float landingDip = 0.085f;
        [SerializeField] private float strafeCameraTilt = 2.8f;
        [SerializeField] private float slideCameraTilt = 7.5f;
        [SerializeField] private float cameraRollSharpness = 10f;
        [SerializeField] private float weaponSwayPositionAmount = 0.0012f;
        [SerializeField] private float weaponSwayRotationAmount = 0.055f;
        [SerializeField] private float weaponSwaySharpness = 16f;
        [SerializeField] private float recoilPitchKick = 1.7f;
        [SerializeField] private float recoilWeaponKickback = 0.08f;
        [SerializeField] private float recoilViewPitchKick = 0.28f;
        [SerializeField] private float recoilViewYawKick = 0.18f;
        [SerializeField] private float recoilRecovery = 16f;

        private readonly NetworkVariable<float> _pitch = new(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        private readonly NetworkVariable<int> _health = new(
            MaxHealthValue,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<int> _score = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<bool> _isCrouchingState = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        private CharacterController _characterController;
        private Camera _localCamera;
        private Vector3 _planarVelocity;
        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private Vector3 _slideVelocity;
        private float _verticalVelocity;
        private float _nextLocalShotTime;
        private float _nextServerShotTime;
        private float _yaw;
        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private float _headBobTime;
        private float _landingOffset;
        private float _landingOffsetVelocity;
        private float _visualRecoilPitch;
        private float _visualRecoilKickback;
        private float _shotRollKick;
        private float _shotFovOffset;
        private float _cameraRoll;
        private float _crosshairPulse;
        private float _hitMarkerTimer;
        private float _killMarkerTimer;
        private float _slideTimer;
        private float _slideCooldownTimer;
        private float _aimBlend;
        private float _crouchBlend;
        private bool _isGrounded;
        private bool _isSliding;
        private bool _isAiming;
        private bool _manualCrouchRequested;
        private Vector3 _pivotBaseLocalPosition;
        private Vector3 _weaponBaseLocalPosition;
        private Quaternion _weaponBaseLocalRotation;
        private float _standingControllerHeight;
        private Vector3 _standingControllerCenter;

        public static IReadOnlyList<LanShooterPlayer> ActivePlayers => Players;

        public static LanShooterPlayer LocalPlayer
        {
            get
            {
                for (var i = 0; i < Players.Count; i++)
                {
                    if (Players[i] != null && Players[i].IsOwner)
                    {
                        return Players[i];
                    }
                }

                return null;
            }
        }

        public int Health => _health.Value;

        public int Score => _score.Value;

        public bool IsAlive => _health.Value > 0;

        public bool IsCursorLocked => Cursor.lockState == CursorLockMode.Locked;

        public string DisplayName => $"P{OwnerClientId}";

        public float CrosshairSpread
        {
            get
            {
                var speedRatio = moveSpeed > 0.01f
                    ? Mathf.Clamp01(new Vector3(_planarVelocity.x, 0f, _planarVelocity.z).magnitude / moveSpeed)
                    : 0f;

                var aimMultiplier = Mathf.Lerp(1f, 0.25f, _aimBlend);
                var slidePenalty = _isSliding ? 10f : 0f;
                var airbornePenalty = _isGrounded ? 0f : 8f;
                return (8f + speedRatio * 18f + _crosshairPulse * 16f + airbornePenalty + slidePenalty) * aimMultiplier;
            }
        }

        public float HitMarkerAlpha => Mathf.Clamp01(_hitMarkerTimer / HitMarkerDuration);

        public float KillMarkerAlpha => Mathf.Clamp01(_killMarkerTimer / KillMarkerDuration);

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            CacheViewDefaults();
            _yaw = transform.eulerAngles.y;
            CacheControllerDefaults();

            if (projectileSpeed <= 0f)
            {
                projectileSpeed = 160f;
            }

            if (slideInitialSpeed < 15.5f)
            {
                slideInitialSpeed = 16.5f;
            }

            if (slideDuration < 0.55f)
            {
                slideDuration = 0.62f;
            }

            if (slideCameraDrop < 0.48f)
            {
                slideCameraDrop = 0.52f;
            }

            if (slideDeceleration > 24f)
            {
                slideDeceleration = 21f;
            }
        }

        public override void OnNetworkSpawn()
        {
            if (!Players.Contains(this))
            {
                Players.Add(this);
            }

            _health.OnValueChanged += HandleHealthChanged;
            ApplyTint();
            CacheViewDefaults();
            _isGrounded = _characterController != null && _characterController.isGrounded;

            if (IsOwner)
            {
                CreateLocalCamera();
                SetRendererArrayVisible(localHiddenRenderers, false);
                LockCursor(true);
            }

            if (IsServer)
            {
                RespawnFromSceneContext();
            }

            ApplyPitch();
        }

        public override void OnNetworkDespawn()
        {
            _health.OnValueChanged -= HandleHealthChanged;
            Players.Remove(this);

            if (_localCamera != null)
            {
                Destroy(_localCamera.gameObject);
            }

            if (IsOwner)
            {
                SetRendererArrayVisible(localHiddenRenderers, true);
                LockCursor(false);
            }
        }

        private void Update()
        {
            if (!IsSpawned)
            {
                return;
            }

            TickFeedbackTimers();
            UpdateStanceShape();

            if (IsOwner)
            {
                HandleCursorInput();

                if (IsAlive && IsCursorLocked)
                {
                    UpdateAimState();
                    HandleLook();
                    HandleMovement();
                    HandleFire();
                }
                else
                {
                    _moveInput = Vector2.zero;
                    _lookInput = Vector2.zero;
                    _isAiming = false;
                }

                UpdateLocalPresentation();
            }

            ApplyPitch();
        }

        private void HandleMovement()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || _characterController == null)
            {
                return;
            }

            var input = Vector2.zero;
            if (keyboard.aKey.isPressed) input.x -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            if (keyboard.sKey.isPressed) input.y -= 1f;
            if (keyboard.wKey.isPressed) input.y += 1f;
            _moveInput = input.sqrMagnitude > 1f ? input.normalized : input;

            var deltaTime = Time.deltaTime;
            var wasGrounded = _characterController.isGrounded;
            _slideCooldownTimer = Mathf.Max(0f, _slideCooldownTimer - deltaTime);
            _coyoteTimer = wasGrounded ? coyoteTime : Mathf.Max(0f, _coyoteTimer - deltaTime);

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                _jumpBufferTimer = jumpBufferTime;
                _manualCrouchRequested = false;
                RefreshCrouchState();
            }
            else
            {
                _jumpBufferTimer = Mathf.Max(0f, _jumpBufferTimer - deltaTime);
            }

            if (!_isSliding && keyboard.leftShiftKey.wasPressedThisFrame)
            {
                if (!TryStartSlide() && wasGrounded)
                {
                    _manualCrouchRequested = !_manualCrouchRequested;
                    RefreshCrouchState();
                }
            }

            var desiredDirection = transform.right * _moveInput.x + transform.forward * _moveInput.y;
            var speedMultiplier = _isCrouchingState.Value && !_isSliding ? crouchMoveSpeedMultiplier : 1f;
            var desiredSpeed = moveSpeed * speedMultiplier * (wasGrounded ? 1f : airControlMultiplier);
            var desiredVelocity = desiredDirection * desiredSpeed;

            if (_isSliding)
            {
                _slideTimer -= deltaTime;
                _slideVelocity = Vector3.MoveTowards(_slideVelocity, Vector3.zero, slideDeceleration * deltaTime);
                _slideVelocity = Vector3.Lerp(_slideVelocity, desiredVelocity, slideSteerControl * deltaTime);
                _planarVelocity = _slideVelocity;

                if (_slideTimer <= 0f || _slideVelocity.magnitude < moveSpeed * 0.5f || !wasGrounded)
                {
                    _isSliding = false;
                    RefreshCrouchState();
                }
            }
            else
            {
                float acceleration;
                if (desiredDirection.sqrMagnitude > 0.01f)
                {
                    acceleration = wasGrounded ? groundAcceleration : airAcceleration;
                }
                else
                {
                    acceleration = wasGrounded ? groundDeceleration : airDeceleration;
                }

                _planarVelocity = Vector3.MoveTowards(_planarVelocity, desiredVelocity, acceleration * deltaTime);
            }

            if (!_isSliding && _jumpBufferTimer > 0f && _coyoteTimer > 0f)
            {
                _verticalVelocity = jumpForce;
                _jumpBufferTimer = 0f;
                _coyoteTimer = 0f;
                wasGrounded = false;
            }

            if (wasGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }

            _verticalVelocity = Mathf.Max(_verticalVelocity + gravity * deltaTime, -maxFallSpeed);

            var move = (_planarVelocity + Vector3.up * _verticalVelocity) * deltaTime;
            var collisionFlags = _characterController.Move(move);
            var isGroundedNow = (collisionFlags & CollisionFlags.Below) != 0 || _characterController.isGrounded;

            if (isGroundedNow && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }

            if (!wasGrounded && isGroundedNow)
            {
                _landingOffset += landingDip;
            }

            if (!isGroundedNow)
            {
                _isSliding = false;
                RefreshCrouchState();
            }

            _isGrounded = isGroundedNow;
        }

        private bool TryStartSlide()
        {
            if (_slideCooldownTimer > 0f || !_isGrounded)
            {
                return false;
            }

            var moveDirection = new Vector3(_planarVelocity.x, 0f, _planarVelocity.z);
            if (moveDirection.magnitude < slideStartSpeedThreshold)
            {
                return false;
            }

            _manualCrouchRequested = false;
            _isSliding = true;
            _slideTimer = slideDuration;
            _slideCooldownTimer = slideCooldown;
            _slideVelocity = moveDirection.normalized * Mathf.Max(slideInitialSpeed, moveDirection.magnitude * 1.35f);
            _isAiming = false;
            RefreshCrouchState();
            return true;
        }

        private void UpdateAimState()
        {
            var mouse = Mouse.current;
            _isAiming = !_isSliding && mouse != null && mouse.rightButton.isPressed;
        }

        private void HandleLook()
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            _lookInput = mouse.delta.ReadValue();
            var sensitivityMultiplier = _isAiming ? adsSensitivityMultiplier : 1f;
            var lookDelta = _lookInput * mouseSensitivity * sensitivityMultiplier;
            _yaw += lookDelta.x;
            _pitch.Value = Mathf.Clamp(_pitch.Value - lookDelta.y, -75f, 75f);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        }

        private void HandleFire()
        {
            var mouse = Mouse.current;
            if (mouse == null || cameraPivot == null || !mouse.leftButton.isPressed || Time.time < _nextLocalShotTime)
            {
                return;
            }

            var viewTransform = _localCamera != null ? _localCamera.transform : cameraPivot;
            var headOrigin = viewTransform.position;
            var viewDirection = viewTransform.forward;
            var shootOrigin = headOrigin + viewDirection * fireOriginForwardOffset;
            var shootDirection = ResolveShotDirection(headOrigin, shootOrigin, viewDirection);

            _nextLocalShotTime = Time.time + fireCooldown;
            ApplyLocalShotFeedback();
            FireWeaponRpc(shootOrigin, shootDirection);
        }

        private void HandleCursorInput()
        {
            if (!Application.isFocused)
            {
                return;
            }

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                LockCursor(false);
            }

            if (!IsCursorLocked && mouse != null && mouse.leftButton.wasPressedThisFrame && IsAlive)
            {
                LockCursor(true);
            }
        }

        private void TickFeedbackTimers()
        {
            var deltaTime = Time.deltaTime;
            _aimBlend = Mathf.MoveTowards(_aimBlend, _isAiming ? 1f : 0f, deltaTime * 8f);
            _crosshairPulse = Mathf.MoveTowards(_crosshairPulse, 0f, deltaTime * 6f);
            _visualRecoilPitch = Mathf.MoveTowards(_visualRecoilPitch, 0f, deltaTime * recoilRecovery);
            _visualRecoilKickback = Mathf.MoveTowards(_visualRecoilKickback, 0f, deltaTime * recoilRecovery * 1.3f);
            _shotRollKick = Mathf.MoveTowards(_shotRollKick, 0f, deltaTime * recoilRecovery * 1.5f);
            _shotFovOffset = Mathf.MoveTowards(_shotFovOffset, 0f, deltaTime * 12f);
            _landingOffset = Mathf.SmoothDamp(_landingOffset, 0f, ref _landingOffsetVelocity, 0.06f);
            _hitMarkerTimer = Mathf.Max(0f, _hitMarkerTimer - deltaTime);
            _killMarkerTimer = Mathf.Max(0f, _killMarkerTimer - deltaTime);
            _cameraRoll = Mathf.Lerp(_cameraRoll, ComputeTargetCameraRoll(), 1f - Mathf.Exp(-cameraRollSharpness * deltaTime));
        }

        private void UpdateLocalPresentation()
        {
            if (cameraPivot == null)
            {
                return;
            }

            var deltaTime = Time.deltaTime;
            var planarSpeed = new Vector3(_planarVelocity.x, 0f, _planarVelocity.z).magnitude;
            var speedRatio = moveSpeed > 0.01f ? Mathf.Clamp01(planarSpeed / moveSpeed) : 0f;

            Vector3 bobOffset = Vector3.zero;
            if (_isGrounded && speedRatio > 0.05f && !_isSliding)
            {
                _headBobTime += deltaTime * (headBobFrequency + speedRatio * 2.5f);
                var aimBobMultiplier = Mathf.Lerp(1f, 0.25f, _aimBlend);
                bobOffset = new Vector3(
                    Mathf.Sin(_headBobTime * 0.5f) * headBobAmplitude * 0.45f,
                    (Mathf.Abs(Mathf.Cos(_headBobTime)) - 0.5f) * headBobAmplitude,
                    0f) * speedRatio * aimBobMultiplier;
            }

            var crouchOffset = Vector3.down * (crouchCameraDrop * _crouchBlend);
            var slideOffset = _isSliding ? Vector3.down * slideCameraDrop : Vector3.zero;
            var targetPivotPosition = _pivotBaseLocalPosition + bobOffset + crouchOffset + slideOffset + Vector3.down * _landingOffset;
            cameraPivot.localPosition = Vector3.Lerp(
                cameraPivot.localPosition,
                targetPivotPosition,
                1f - Mathf.Exp(-14f * deltaTime));

            if (_localCamera != null)
            {
                var baseFov = Mathf.Lerp(baseFieldOfView, adsFieldOfView, _aimBlend);
                var speedFov = Mathf.Lerp(moveFovBoost, 0.6f, _aimBlend);
                var targetFov = baseFov + speedRatio * speedFov + _shotFovOffset;
                _localCamera.fieldOfView = Mathf.Lerp(
                    _localCamera.fieldOfView,
                    targetFov,
                    1f - Mathf.Exp(-fieldOfViewSharpness * deltaTime));
            }

            if (weaponRoot != null)
            {
                var swayPosition = new Vector3(
                    -_lookInput.x * weaponSwayPositionAmount,
                    -_lookInput.y * weaponSwayPositionAmount,
                    0f);

                swayPosition += new Vector3(
                    -_moveInput.x * 0.012f,
                    -Mathf.Abs(_moveInput.y) * 0.006f,
                    -_visualRecoilKickback);

                var adsPosition = adsWeaponPositionOffset * _aimBlend;
                var crouchPosition = crouchWeaponOffset * _crouchBlend;
                var slidePosition = _isSliding ? new Vector3(0f, -0.16f, 0.12f) : Vector3.zero;
                var targetWeaponPosition = _weaponBaseLocalPosition + swayPosition + adsPosition + crouchPosition + slidePosition;
                weaponRoot.localPosition = Vector3.Lerp(
                    weaponRoot.localPosition,
                    targetWeaponPosition,
                    1f - Mathf.Exp(-weaponSwaySharpness * deltaTime));

                var swayRotation = Quaternion.Euler(
                    _lookInput.y * weaponSwayRotationAmount - _visualRecoilPitch * 2.1f,
                    -_lookInput.x * weaponSwayRotationAmount,
                    -_moveInput.x * 4f);

                var adsRotation = Quaternion.Euler(adsWeaponRotationOffset * _aimBlend);
                var crouchRotation = Quaternion.Euler(_crouchBlend * 2.5f, 0f, 0f);
                var slideRotation = _isSliding ? Quaternion.Euler(14f, 0f, -18f) : Quaternion.identity;
                weaponRoot.localRotation = Quaternion.Slerp(
                    weaponRoot.localRotation,
                    _weaponBaseLocalRotation * swayRotation * adsRotation * crouchRotation * slideRotation,
                    1f - Mathf.Exp(-weaponSwaySharpness * deltaTime));
            }
        }

        private void ApplyLocalShotFeedback()
        {
            var aimRecoilMultiplier = _isAiming ? 0.75f : 1f;
            _visualRecoilPitch += recoilPitchKick * aimRecoilMultiplier;
            _visualRecoilKickback += recoilWeaponKickback * aimRecoilMultiplier;
            _shotFovOffset = Mathf.Min(_shotFovOffset + shotFovKick * aimRecoilMultiplier, shotFovKick * 1.8f);
            _crosshairPulse = 1f;
            _pitch.Value = Mathf.Clamp(_pitch.Value - recoilViewPitchKick * aimRecoilMultiplier, -75f, 75f);
            _yaw += Random.Range(-recoilViewYawKick, recoilViewYawKick) * aimRecoilMultiplier;
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            _shotRollKick = Mathf.Clamp(
                _shotRollKick + Random.Range(-1f, 1f) * aimRecoilMultiplier * 0.9f,
                -slideCameraTilt,
                slideCameraTilt);
        }

        private void HandleHealthChanged(int oldValue, int newValue)
        {
            if (newValue <= 0)
            {
                _planarVelocity = Vector3.zero;
                _slideVelocity = Vector3.zero;
                _verticalVelocity = 0f;
                _lookInput = Vector2.zero;
                _isSliding = false;
                _isAiming = false;
                _manualCrouchRequested = false;
                RefreshCrouchState();
                if (IsOwner)
                {
                    LockCursor(false);
                }
            }
            else if (IsOwner)
            {
                LockCursor(true);
            }
        }

        private void CreateLocalCamera()
        {
            if (_localCamera != null || cameraPivot == null)
            {
                return;
            }

            foreach (var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (camera != null)
                {
                    camera.enabled = false;
                }
            }

            var cameraObject = new GameObject("LocalPlayerCamera");
            cameraObject.transform.SetParent(cameraPivot, false);
            cameraObject.transform.localPosition = Vector3.zero;
            cameraObject.transform.localRotation = Quaternion.identity;

            _localCamera = cameraObject.AddComponent<Camera>();
            _localCamera.fieldOfView = baseFieldOfView;
            _localCamera.nearClipPlane = 0.03f;
            _localCamera.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();
        }

        private void ApplyPitch()
        {
            if (cameraPivot == null)
            {
                return;
            }

            var visualPitch = IsOwner ? _pitch.Value - _visualRecoilPitch : _pitch.Value;
            var visualRoll = IsOwner ? _cameraRoll : 0f;
            cameraPivot.localRotation = Quaternion.Euler(visualPitch, 0f, visualRoll);
        }

        private Vector3 ResolveShotDirection(Vector3 headOrigin, Vector3 shootOrigin, Vector3 viewDirection)
        {
            var targetPoint = headOrigin + viewDirection * projectileMaxDistance;
            if (Physics.SphereCast(
                    headOrigin,
                    aimProbeRadius,
                    viewDirection,
                    out var hit,
                    projectileMaxDistance,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore))
            {
                var hitPlayer = hit.collider.GetComponentInParent<LanShooterPlayer>();
                if (hitPlayer != this)
                {
                    targetPoint = hit.point;
                }
            }

            var resolvedDirection = targetPoint - shootOrigin;
            return resolvedDirection.sqrMagnitude > 0.0001f ? resolvedDirection.normalized : viewDirection;
        }

        private float ComputeTargetCameraRoll()
        {
            if (!IsOwner || !IsAlive)
            {
                return 0f;
            }

            var planarSpeed = new Vector3(_planarVelocity.x, 0f, _planarVelocity.z).magnitude;
            var speedRatio = moveSpeed > 0.01f ? Mathf.Clamp01(planarSpeed / moveSpeed) : 0f;
            var strafeRoll = -_moveInput.x * strafeCameraTilt * Mathf.Lerp(0.4f, 1f, speedRatio);
            var slideRoll = 0f;
            if (_isSliding)
            {
                var localSlideDirection = transform.InverseTransformDirection(_slideVelocity.normalized);
                var slideSign = Mathf.Abs(localSlideDirection.x) > 0.12f
                    ? Mathf.Sign(localSlideDirection.x)
                    : Mathf.Sign(_moveInput.x);
                slideRoll = slideSign == 0f ? 0f : -slideSign * slideCameraTilt;
            }

            return strafeRoll + slideRoll + _shotRollKick;
        }

        private void ApplyTint()
        {
            var palette = new[]
            {
                new Color(0.85f, 0.27f, 0.24f),
                new Color(0.2f, 0.65f, 0.92f),
                new Color(0.23f, 0.75f, 0.42f),
                new Color(0.94f, 0.72f, 0.2f),
                new Color(0.89f, 0.44f, 0.8f),
                new Color(0.95f, 0.52f, 0.18f),
            };

            var color = palette[(int)(OwnerClientId % (ulong)palette.Length)];

            if (tintRenderers == null)
            {
                return;
            }

            foreach (var renderer in tintRenderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                var material = renderer.material;
                material.color = color;
                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", color);
                }
            }
        }

        private void CacheViewDefaults()
        {
            if (cameraPivot != null)
            {
                _pivotBaseLocalPosition = cameraPivot.localPosition;
            }

            if (weaponRoot != null)
            {
                _weaponBaseLocalPosition = weaponRoot.localPosition;
                _weaponBaseLocalRotation = weaponRoot.localRotation;
            }
        }

        private void CacheControllerDefaults()
        {
            if (_characterController == null)
            {
                return;
            }

            _standingControllerHeight = _characterController.height;
            _standingControllerCenter = _characterController.center;
        }

        private void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void SetRendererArrayVisible(Renderer[] renderers, bool visible)
        {
            if (renderers == null)
            {
                return;
            }

            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = visible;
                }
            }
        }

        private void RespawnFromSceneContext()
        {
            var sceneContext = LanShooterSceneContext.Instance;
            if (sceneContext == null)
            {
                SendRespawnRpc(transform.position, transform.eulerAngles.y);
                return;
            }

            var spawnPosition = sceneContext.GetSpawnPoint(OwnerClientId);
            var spawnRotation = sceneContext.GetSpawnRotation(OwnerClientId);
            SendRespawnRpc(spawnPosition, spawnRotation.eulerAngles.y);
        }

        public static LanShooterPlayer FindByClientId(ulong clientId)
        {
            for (var i = 0; i < Players.Count; i++)
            {
                var player = Players[i];
                if (player != null && player.OwnerClientId == clientId)
                {
                    return player;
                }
            }

            return null;
        }

        public bool TryApplyDamageFromServer(int damageAmount, ulong attackerId)
        {
            return ApplyDamageServer(damageAmount, attackerId);
        }

        public void AddScore(int amount)
        {
            if (!IsServer)
            {
                return;
            }

            _score.Value += amount;
        }

        public void NotifyHitFeedback(bool eliminated)
        {
            if (!IsSpawned)
            {
                return;
            }

            if (IsOwner)
            {
                ApplyHitFeedback(eliminated);
                return;
            }

            if (IsServer)
            {
                ReceiveHitFeedbackRpc(eliminated, RpcTarget.Single(OwnerClientId, RpcTargetUse.Temp));
            }
        }

        [Rpc(SendTo.Server)]
        private void FireWeaponRpc(Vector3 shootOrigin, Vector3 shootDirection)
        {
            if (!IsServer || !IsAlive || Time.time < _nextServerShotTime)
            {
                return;
            }

            _nextServerShotTime = Time.time + fireCooldown;

            var sceneContext = LanShooterSceneContext.Instance;
            var projectilePrefab = sceneContext != null ? sceneContext.ProjectilePrefab : null;
            if (projectilePrefab == null)
            {
                return;
            }

            var projectileObject = Instantiate(projectilePrefab, shootOrigin, Quaternion.LookRotation(shootDirection.normalized, Vector3.up));
            var projectileNetworkObject = projectileObject.GetComponent<NetworkObject>();
            var projectile = projectileObject.GetComponent<LanShooterProjectile>();
            if (projectileNetworkObject == null || projectile == null)
            {
                Destroy(projectileObject);
                return;
            }

            projectileNetworkObject.Spawn();
            projectile.InitializeServer(
                shootDirection,
                OwnerClientId,
                damagePerShot,
                projectileSpeed,
                projectileRadius,
                projectileMaxDistance);
        }

        private bool ApplyDamageServer(int damageAmount, ulong attackerId)
        {
            if (!IsServer || !IsAlive)
            {
                return false;
            }

            _health.Value = Mathf.Max(0, _health.Value - damageAmount);
            if (_health.Value > 0)
            {
                return false;
            }

            var attacker = FindByClientId(attackerId);
            if (attacker != null && attacker != this)
            {
                attacker._score.Value += 1;
            }

            StartCoroutine(ServerRespawnRoutine());
            return true;
        }

        private IEnumerator ServerRespawnRoutine()
        {
            yield return new WaitForSeconds(respawnDelay);
            _health.Value = MaxHealthValue;
            RespawnFromSceneContext();
        }

        [Rpc(SendTo.Everyone)]
        private void SendRespawnRpc(Vector3 position, float yaw)
        {
            if (_characterController == null)
            {
                _characterController = GetComponent<CharacterController>();
            }

            if (_characterController != null)
            {
                _characterController.enabled = false;
            }

            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            _yaw = yaw;
            _planarVelocity = Vector3.zero;
            _slideVelocity = Vector3.zero;
            _verticalVelocity = 0f;
            _moveInput = Vector2.zero;
            _lookInput = Vector2.zero;
            _visualRecoilPitch = 0f;
            _visualRecoilKickback = 0f;
            _shotRollKick = 0f;
            _shotFovOffset = 0f;
            _cameraRoll = 0f;
            _crosshairPulse = 0f;
            _landingOffset = 0f;
            _hitMarkerTimer = 0f;
            _killMarkerTimer = 0f;
            _slideTimer = 0f;
            _slideCooldownTimer = 0f;
            _isSliding = false;
            _isAiming = false;
            _manualCrouchRequested = false;
            _crouchBlend = 0f;

            if (IsOwner)
            {
                _pitch.Value = 0f;
                _isCrouchingState.Value = false;
            }
            else if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.identity;
            }

            if (_characterController != null)
            {
                _characterController.enabled = true;
                CacheControllerDefaults();
                ApplyStanceToController(0f);
                _isGrounded = _characterController.isGrounded;
            }

            if (IsOwner)
            {
                LockCursor(true);
            }
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void ReceiveHitFeedbackRpc(bool eliminated, RpcParams rpcParams = default)
        {
            if (!IsOwner)
            {
                return;
            }

            ApplyHitFeedback(eliminated);
        }

        private void ApplyHitFeedback(bool eliminated)
        {
            _hitMarkerTimer = HitMarkerDuration;
            if (eliminated)
            {
                _killMarkerTimer = KillMarkerDuration;
            }
        }

        private void RefreshCrouchState()
        {
            if (!IsOwner)
            {
                return;
            }

            var wantsCrouch = _manualCrouchRequested || _isSliding;
            if (_isCrouchingState.Value != wantsCrouch)
            {
                _isCrouchingState.Value = wantsCrouch;
            }
        }

        private void UpdateStanceShape()
        {
            var targetBlend = _isCrouchingState.Value ? 1f : 0f;
            _crouchBlend = Mathf.Lerp(
                _crouchBlend,
                targetBlend,
                1f - Mathf.Exp(-crouchTransitionSharpness * Time.deltaTime));

            ApplyStanceToController(_crouchBlend);
        }

        private void ApplyStanceToController(float crouchBlend)
        {
            if (_characterController == null || _standingControllerHeight <= 0.01f)
            {
                return;
            }

            var targetHeight = Mathf.Lerp(_standingControllerHeight, crouchControllerHeight, crouchBlend);
            var targetCenter = new Vector3(
                _standingControllerCenter.x,
                Mathf.Lerp(_standingControllerCenter.y, crouchControllerHeight * 0.5f, crouchBlend),
                _standingControllerCenter.z);

            _characterController.height = targetHeight;
            _characterController.center = targetCenter;
        }

#if UNITY_EDITOR
        public void SetEditorReferences(
            Transform pivot,
            Transform weaponTransform,
            Transform muzzlePoint,
            Renderer[] renderers,
            Renderer[] hiddenRenderers)
        {
            cameraPivot = pivot;
            weaponRoot = weaponTransform;
            muzzle = muzzlePoint;
            tintRenderers = renderers;
            localHiddenRenderers = hiddenRenderers;
            CacheViewDefaults();
        }
#endif
    }
}
