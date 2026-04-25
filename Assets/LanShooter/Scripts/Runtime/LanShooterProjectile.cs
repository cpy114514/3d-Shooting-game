using System;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace LanShooter
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(NetworkTransform))]
    public sealed class LanShooterProjectile : NetworkBehaviour
    {
        private readonly RaycastHit[] _hits = new RaycastHit[8];

        [SerializeField] private float visualRevealDistance = 1.1f;

        private ulong _attackerClientId;
        private Vector3 _direction;
        private float _speed;
        private float _radius;
        private float _maxDistance;
        private int _damageAmount;
        private float _distanceTravelled;
        private bool _initialized;
        private bool _visualsVisible;
        private Vector3 _spawnPosition;
        private Renderer[] _visualRenderers;

        private void Awake()
        {
            _visualRenderers = GetComponentsInChildren<Renderer>(true);
            ResetVisualState();
        }

        public override void OnNetworkSpawn()
        {
            ResetVisualState();
        }

        public void InitializeServer(Vector3 direction, ulong attackerClientId, int damageAmount, float speed, float radius, float maxDistance)
        {
            _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
            _attackerClientId = attackerClientId;
            _damageAmount = damageAmount;
            _speed = speed;
            _radius = radius;
            _maxDistance = maxDistance;
            _distanceTravelled = 0f;
            _initialized = true;
        }

        private void Update()
        {
            UpdateVisualVisibility();

            if (!IsServer || !_initialized)
            {
                return;
            }

            var stepDistance = _speed * Time.deltaTime;
            var hitCount = Physics.SphereCastNonAlloc(
                transform.position,
                _radius,
                _direction,
                _hits,
                stepDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            if (hitCount > 1)
            {
                Array.Sort(_hits, 0, hitCount, RaycastHitDistanceComparer.Instance);
            }

            for (var i = 0; i < hitCount; i++)
            {
                var hit = _hits[i];
                if (hit.collider == null)
                {
                    continue;
                }

                var player = hit.collider.GetComponentInParent<LanShooterPlayer>();
                if (player != null && player.OwnerClientId == _attackerClientId)
                {
                    continue;
                }

                transform.position = hit.point - _direction * 0.05f;

                if (player != null)
                {
                    var eliminated = player.TryApplyDamageFromServer(_damageAmount, _attackerClientId);
                    var attacker = LanShooterPlayer.FindByClientId(_attackerClientId);
                    if (attacker != null)
                    {
                        attacker.NotifyHitFeedback(eliminated);
                    }

                    NetworkObject.Despawn();
                    return;
                }

                var enemy = hit.collider.GetComponentInParent<LanShooterEnemy>();
                if (enemy != null)
                {
                    var attacker = LanShooterPlayer.FindByClientId(_attackerClientId);
                    var eliminated = enemy.ApplyDamageServer(_damageAmount, attacker);
                    if (attacker != null && !eliminated)
                    {
                        attacker.NotifyHitFeedback(false);
                    }

                    NetworkObject.Despawn();
                    return;
                }

                NetworkObject.Despawn();
                return;
            }

            transform.position += _direction * stepDistance;
            _distanceTravelled += stepDistance;

            if (_distanceTravelled >= _maxDistance)
            {
                NetworkObject.Despawn();
            }
        }

        private void ResetVisualState()
        {
            _spawnPosition = transform.position;
            _visualsVisible = visualRevealDistance <= 0f;
            SetVisualsVisible(_visualsVisible);
        }

        private void UpdateVisualVisibility()
        {
            if (_visualsVisible)
            {
                return;
            }

            if ((transform.position - _spawnPosition).sqrMagnitude >= visualRevealDistance * visualRevealDistance)
            {
                _visualsVisible = true;
                SetVisualsVisible(true);
            }
        }

        private void SetVisualsVisible(bool visible)
        {
            if (_visualRenderers == null)
            {
                return;
            }

            foreach (var visualRenderer in _visualRenderers)
            {
                if (visualRenderer != null)
                {
                    visualRenderer.enabled = visible;
                }
            }
        }

        private sealed class RaycastHitDistanceComparer : IComparer
        {
            public static readonly RaycastHitDistanceComparer Instance = new();

            public int Compare(object x, object y)
            {
                var left = (RaycastHit)x;
                var right = (RaycastHit)y;
                return left.distance.CompareTo(right.distance);
            }
        }
    }
}
