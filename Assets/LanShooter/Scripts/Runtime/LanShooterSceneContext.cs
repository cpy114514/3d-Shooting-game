using System.Linq;
using UnityEngine;

namespace LanShooter
{
    public sealed class LanShooterSceneContext : MonoBehaviour
    {
        private const string PlayerPrefabResourcePath = "LanShooter/LanShooterPlayer";
        private const string ProjectilePrefabResourcePath = "LanShooter/LanShooterProjectile";
        private const string EnemyPrefabResourcePath = "LanShooter/LanShooterEnemy";
        private static LanShooterSceneContext s_Instance;

        [Header("Editable References")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private LanShooterSpawnPoint[] networkSpawnPoints;
        [SerializeField] private LanShooterEnemySpawnPoint[] enemySpawnPoints;
        [SerializeField] private Transform fallbackSpawnPoint;

        public static LanShooterSceneContext Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = FindFirstObjectByType<LanShooterSceneContext>();
                }

                return s_Instance;
            }
        }

        public GameObject PlayerPrefab => playerPrefab != null ? playerPrefab : Resources.Load<GameObject>(PlayerPrefabResourcePath);

        public GameObject ProjectilePrefab => projectilePrefab != null ? projectilePrefab : Resources.Load<GameObject>(ProjectilePrefabResourcePath);

        public GameObject EnemyPrefab => enemyPrefab != null ? enemyPrefab : Resources.Load<GameObject>(EnemyPrefabResourcePath);

        private void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                return;
            }

            s_Instance = this;
            RefreshSpawnPointsIfNeeded();
        }

        private void OnValidate()
        {
            RefreshSpawnPointsIfNeeded();
        }

        public Vector3 GetSpawnPoint(ulong clientId)
        {
            var spawn = GetSpawnPointComponent(clientId);
            if (spawn != null)
            {
                return spawn.Position;
            }

            return fallbackSpawnPoint != null ? fallbackSpawnPoint.position : Vector3.up;
        }

        public Quaternion GetSpawnRotation(ulong clientId)
        {
            var spawn = GetSpawnPointComponent(clientId);
            if (spawn != null)
            {
                return spawn.Rotation;
            }

            return fallbackSpawnPoint != null ? fallbackSpawnPoint.rotation : Quaternion.identity;
        }

        public void RefreshSpawnPointsIfNeeded()
        {
            if (networkSpawnPoints != null && networkSpawnPoints.Length > 0 && networkSpawnPoints.Any(point => point != null))
            {
            }
            else
            {
                networkSpawnPoints = FindObjectsByType<LanShooterSpawnPoint>(FindObjectsSortMode.None)
                    .OrderBy(point => point.name)
                    .ToArray();
            }

            if (enemySpawnPoints != null && enemySpawnPoints.Length > 0 && enemySpawnPoints.Any(point => point != null))
            {
                return;
            }

            enemySpawnPoints = FindObjectsByType<LanShooterEnemySpawnPoint>(FindObjectsSortMode.None)
                .OrderBy(point => point.name)
                .ToArray();
        }

        private LanShooterSpawnPoint GetSpawnPointComponent(ulong clientId)
        {
            RefreshSpawnPointsIfNeeded();

            if (networkSpawnPoints == null || networkSpawnPoints.Length == 0)
            {
                return null;
            }

            var validSpawnPoints = networkSpawnPoints.Where(point => point != null).ToArray();
            if (validSpawnPoints.Length == 0)
            {
                return null;
            }

            var index = (int)(clientId % (ulong)validSpawnPoints.Length);
            return validSpawnPoints[index];
        }

        public Vector3 GetEnemySpawnPoint(int index)
        {
            var spawn = GetEnemySpawnPointComponent(index);
            if (spawn != null)
            {
                return spawn.Position;
            }

            return fallbackSpawnPoint != null ? fallbackSpawnPoint.position : Vector3.up;
        }

        public Quaternion GetEnemySpawnRotation(int index)
        {
            var spawn = GetEnemySpawnPointComponent(index);
            if (spawn != null)
            {
                return spawn.Rotation;
            }

            return Quaternion.identity;
        }

        public int EnemySpawnPointCount
        {
            get
            {
                RefreshSpawnPointsIfNeeded();
                return enemySpawnPoints?.Count(point => point != null) ?? 0;
            }
        }

        private LanShooterEnemySpawnPoint GetEnemySpawnPointComponent(int index)
        {
            RefreshSpawnPointsIfNeeded();

            if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
            {
                return null;
            }

            var validSpawnPoints = enemySpawnPoints.Where(point => point != null).ToArray();
            if (validSpawnPoints.Length == 0)
            {
                return null;
            }

            var wrappedIndex = Mathf.Abs(index) % validSpawnPoints.Length;
            return validSpawnPoints[wrappedIndex];
        }

#if UNITY_EDITOR
        public void SetEditorReferences(
            GameObject prefab,
            GameObject projectile,
            GameObject enemy,
            LanShooterSpawnPoint[] spawnPoints,
            LanShooterEnemySpawnPoint[] enemyPoints,
            Transform fallbackPoint)
        {
            playerPrefab = prefab;
            projectilePrefab = projectile;
            enemyPrefab = enemy;
            networkSpawnPoints = spawnPoints;
            enemySpawnPoints = enemyPoints;
            fallbackSpawnPoint = fallbackPoint;
        }
#endif

        public void ApplyRuntimeDefaults(
            GameObject prefab,
            GameObject projectile,
            GameObject enemy,
            LanShooterSpawnPoint[] spawnPoints,
            LanShooterEnemySpawnPoint[] enemyPoints,
            Transform fallbackPoint)
        {
            if (playerPrefab == null)
            {
                playerPrefab = prefab;
            }

            if (projectilePrefab == null)
            {
                projectilePrefab = projectile;
            }

            if (enemyPrefab == null)
            {
                enemyPrefab = enemy;
            }

            if (networkSpawnPoints == null || networkSpawnPoints.Length == 0 || networkSpawnPoints.All(point => point == null))
            {
                networkSpawnPoints = spawnPoints;
            }

            if (enemySpawnPoints == null || enemySpawnPoints.Length == 0 || enemySpawnPoints.All(point => point == null))
            {
                enemySpawnPoints = enemyPoints;
            }

            if (fallbackSpawnPoint == null)
            {
                fallbackSpawnPoint = fallbackPoint;
            }
        }
    }
}
