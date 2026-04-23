using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace LanShooter
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(NetworkTransform))]
    [RequireComponent(typeof(CharacterController))]
    public sealed class LanShooterEnemy : NetworkBehaviour
    {
        private CharacterController _characterController;
        private LanShooterSoloWaveDirector _waveDirector;
        private LanShooterPlayer _targetPlayer;
        private float _moveSpeed;
        private float _attackDamage;
        private float _attackRange;
        private float _attackCooldown;
        private float _gravity;
        private int _maxHealth;
        private int _currentHealth;
        private float _attackTimer;
        private float _retargetTimer;
        private float _verticalVelocity;
        private bool _initialized;

        public int CurrentHealth => _currentHealth;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
        }

        public void InitializeServer(
            LanShooterSoloWaveDirector waveDirector,
            int maxHealth,
            float moveSpeed,
            float attackDamage,
            float attackRange,
            float attackCooldown,
            float gravity)
        {
            _waveDirector = waveDirector;
            _maxHealth = Mathf.Max(1, maxHealth);
            _currentHealth = _maxHealth;
            _moveSpeed = moveSpeed;
            _attackDamage = attackDamage;
            _attackRange = attackRange;
            _attackCooldown = attackCooldown;
            _gravity = gravity;
            _attackTimer = 0f;
            _retargetTimer = 0f;
            _initialized = true;
        }

        private void Update()
        {
            if (!IsServer || !_initialized)
            {
                return;
            }

            _attackTimer = Mathf.Max(0f, _attackTimer - Time.deltaTime);
            _retargetTimer -= Time.deltaTime;

            if (_retargetTimer <= 0f || _targetPlayer == null || !_targetPlayer.IsSpawned || !_targetPlayer.IsAlive)
            {
                _retargetTimer = 0.25f;
                _targetPlayer = FindClosestTarget();
            }

            if (_targetPlayer == null)
            {
                return;
            }

            var directionToTarget = _targetPlayer.transform.position - transform.position;
            directionToTarget.y = 0f;
            var distance = directionToTarget.magnitude;
            var direction = distance > 0.01f ? directionToTarget / distance : transform.forward;

            if (distance > _attackRange * 0.92f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(direction, Vector3.up),
                    1f - Mathf.Exp(-14f * Time.deltaTime));
            }

            if (_characterController != null)
            {
                if (_characterController.isGrounded && _verticalVelocity < 0f)
                {
                    _verticalVelocity = -2f;
                }

                _verticalVelocity += _gravity * Time.deltaTime;

                var planarVelocity = distance > _attackRange
                    ? direction * _moveSpeed
                    : Vector3.zero;

                _characterController.Move((planarVelocity + Vector3.up * _verticalVelocity) * Time.deltaTime);
            }

            if (distance <= _attackRange && _attackTimer <= 0f)
            {
                _attackTimer = _attackCooldown;
                _targetPlayer.TryApplyDamageFromServer(Mathf.RoundToInt(_attackDamage), ulong.MaxValue);
            }
        }

        private LanShooterPlayer FindClosestTarget()
        {
            LanShooterPlayer bestTarget = null;
            var bestDistanceSqr = float.MaxValue;

            foreach (var player in LanShooterPlayer.ActivePlayers)
            {
                if (player == null || !player.IsSpawned || !player.IsAlive)
                {
                    continue;
                }

                var distanceSqr = (player.transform.position - transform.position).sqrMagnitude;
                if (distanceSqr < bestDistanceSqr)
                {
                    bestDistanceSqr = distanceSqr;
                    bestTarget = player;
                }
            }

            return bestTarget;
        }

        public bool ApplyDamageServer(int damageAmount, LanShooterPlayer attacker)
        {
            if (!IsServer || !_initialized || _currentHealth <= 0)
            {
                return false;
            }

            _currentHealth = Mathf.Max(0, _currentHealth - Mathf.Max(1, damageAmount));
            if (_currentHealth > 0)
            {
                return false;
            }

            if (attacker != null)
            {
                attacker.NotifyHitFeedback(true);
                attacker.AddScore(1);
            }

            _waveDirector?.NotifyEnemyDefeated(this);
            NetworkObject.Despawn();
            return true;
        }
    }
}
