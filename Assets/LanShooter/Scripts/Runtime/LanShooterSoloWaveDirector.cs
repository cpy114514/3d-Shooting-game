using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace LanShooter
{
    public sealed class LanShooterSoloWaveDirector : MonoBehaviour
    {
        private enum WavePhase
        {
            Idle,
            Warmup,
            Spawning,
            Clearing,
            Intermission,
        }

        [Header("Wave Timing")]
        [SerializeField] private float warmupDelay = 2.5f;
        [SerializeField] private float timeBetweenWaves = 5f;
        [SerializeField] private float spawnInterval = 0.7f;
        [SerializeField] private float waveBannerDuration = 2.2f;

        [Header("Enemy Scaling")]
        [SerializeField] private int baseEnemiesPerWave = 4;
        [SerializeField] private int extraEnemiesPerWave = 2;
        [SerializeField] private int baseEnemyHealth = 45;
        [SerializeField] private int extraHealthPerWave = 12;
        [SerializeField] private float baseEnemySpeed = 4.1f;
        [SerializeField] private float extraSpeedPerWave = 0.2f;
        [SerializeField] private float baseEnemyDamage = 9f;
        [SerializeField] private float extraDamagePerWave = 1.5f;
        [SerializeField] private float healthGrowthMultiplier = 1.08f;
        [SerializeField] private float speedGrowthMultiplier = 1.025f;
        [SerializeField] private float damageGrowthMultiplier = 1.06f;
        [SerializeField] private float enemyAttackRange = 1.55f;
        [SerializeField] private float enemyAttackCooldown = 1.1f;
        [SerializeField] private float enemyGravity = -25f;

        private readonly List<LanShooterEnemy> _aliveEnemies = new();

        private Coroutine _waveRoutine;
        private WavePhase _phase;
        private bool _running;
        private float _phaseTimer;

        public static LanShooterSoloWaveDirector Instance { get; private set; }

        public int CurrentWave { get; private set; }

        public int AliveEnemies => _aliveEnemies.Count;

        public int EnemiesRemainingToSpawn { get; private set; }

        public string WaveStatusText
        {
            get
            {
                return _phase switch
                {
                    WavePhase.Warmup => $"First wave begins in {Mathf.CeilToInt(_phaseTimer)}",
                    WavePhase.Spawning => $"Wave {CurrentWave} incoming",
                    WavePhase.Clearing => $"Clear the remaining enemies",
                    WavePhase.Intermission => $"Next wave in {Mathf.CeilToInt(_phaseTimer)}",
                    _ => "Start Solo Practice to begin",
                };
            }
        }

        public bool ShouldShowWaveBanner => _phase is WavePhase.Spawning or WavePhase.Warmup or WavePhase.Intermission;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Update()
        {
            var session = LanShooterSession.Instance;
            var shouldRun = session != null && session.IsSoloSession && session.IsHost;
            _phaseTimer = Mathf.Max(0f, _phaseTimer - Time.deltaTime);

            if (shouldRun && !_running)
            {
                StartDirector();
            }
            else if (!shouldRun && _running)
            {
                StopDirector();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void NotifyEnemyDefeated(LanShooterEnemy enemy)
        {
            _aliveEnemies.Remove(enemy);
        }

        private void StartDirector()
        {
            if (_running)
            {
                return;
            }

            _running = true;
            CurrentWave = 0;
            EnemiesRemainingToSpawn = 0;
            _aliveEnemies.Clear();
            SetPhase(WavePhase.Warmup, warmupDelay);
            _waveRoutine = StartCoroutine(WaveLoop());
        }

        private void StopDirector()
        {
            _running = false;

            if (_waveRoutine != null)
            {
                StopCoroutine(_waveRoutine);
                _waveRoutine = null;
            }

            for (var i = _aliveEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = _aliveEnemies[i];
                if (enemy != null && enemy.NetworkObject != null && enemy.NetworkObject.IsSpawned)
                {
                    enemy.NetworkObject.Despawn();
                }
            }

            _aliveEnemies.Clear();
            CurrentWave = 0;
            EnemiesRemainingToSpawn = 0;
            SetPhase(WavePhase.Idle, 0f);
        }

        private IEnumerator WaveLoop()
        {
            yield return new WaitForSeconds(warmupDelay);

            while (_running)
            {
                CurrentWave++;
                var enemiesThisWave = baseEnemiesPerWave + (CurrentWave - 1) * extraEnemiesPerWave;
                EnemiesRemainingToSpawn = enemiesThisWave;
                SetPhase(WavePhase.Spawning, waveBannerDuration);

                for (var i = 0; i < enemiesThisWave; i++)
                {
                    SpawnEnemy(i);
                    EnemiesRemainingToSpawn = enemiesThisWave - i - 1;
                    yield return new WaitForSeconds(spawnInterval);
                }

                SetPhase(WavePhase.Clearing, 0f);
                while (_running && _aliveEnemies.Count > 0)
                {
                    _aliveEnemies.RemoveAll(enemy => enemy == null || enemy.NetworkObject == null || !enemy.NetworkObject.IsSpawned);
                    yield return null;
                }

                if (!_running)
                {
                    yield break;
                }

                SetPhase(WavePhase.Intermission, timeBetweenWaves);
                yield return new WaitForSeconds(timeBetweenWaves);
            }
        }

        private void SpawnEnemy(int spawnIndex)
        {
            var sceneContext = LanShooterSceneContext.Instance;
            if (sceneContext == null || sceneContext.EnemyPrefab == null)
            {
                return;
            }

            var spawnPosition = sceneContext.GetEnemySpawnPoint(CurrentWave + spawnIndex);
            var spawnRotation = sceneContext.GetEnemySpawnRotation(CurrentWave + spawnIndex);
            var enemyObject = Instantiate(sceneContext.EnemyPrefab, spawnPosition, spawnRotation);
            var enemyNetworkObject = enemyObject.GetComponent<NetworkObject>();
            var enemy = enemyObject.GetComponent<LanShooterEnemy>();
            if (enemyNetworkObject == null || enemy == null)
            {
                Destroy(enemyObject);
                return;
            }

            enemyNetworkObject.Spawn();
            var waveIndex = CurrentWave - 1;
            var health = Mathf.RoundToInt((baseEnemyHealth + waveIndex * extraHealthPerWave) * Mathf.Pow(healthGrowthMultiplier, waveIndex));
            var speed = (baseEnemySpeed + waveIndex * extraSpeedPerWave) * Mathf.Pow(speedGrowthMultiplier, waveIndex);
            var damage = (baseEnemyDamage + waveIndex * extraDamagePerWave) * Mathf.Pow(damageGrowthMultiplier, waveIndex);

            enemy.InitializeServer(
                this,
                health,
                speed,
                damage,
                enemyAttackRange,
                Mathf.Max(0.22f, enemyAttackCooldown - waveIndex * 0.04f),
                enemyGravity);

            _aliveEnemies.Add(enemy);
        }

        private void SetPhase(WavePhase phase, float timer)
        {
            _phase = phase;
            _phaseTimer = timer;
        }
    }
}
