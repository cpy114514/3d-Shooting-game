using System.Linq;
using UnityEngine;

namespace LanShooter
{
    public static class LanShooterRuntimeSceneSetup
    {
        private const string PlayerPrefabResourcePath = "LanShooter/LanShooterPlayer";
        private const string EnemyPrefabResourcePath = "LanShooter/LanShooterEnemy";

        public static void EnsureReady()
        {
            var sceneContext = LanShooterSceneContext.Instance;
            if (sceneContext == null)
            {
                sceneContext = new GameObject("LanShooterSceneContext").AddComponent<LanShooterSceneContext>();
            }

            EnsureDirectionalLight();
            EnsureMenuCamera();

            var arenaRoot = FindOrCreate("EditableArena");
            if (arenaRoot.transform.childCount == 0)
            {
                CreateArenaGeometry(arenaRoot.transform);
            }

            var spawnRoot = FindOrCreate("SpawnPoints");
            var spawnPoints = EnsureSpawnPoints(spawnRoot.transform);
            var enemySpawnRoot = FindOrCreate("EnemySpawnPoints");
            var enemySpawnPoints = EnsureEnemySpawnPoints(enemySpawnRoot.transform);
            var fallbackPoint = spawnPoints.FirstOrDefault();
            var playerPrefab = Resources.Load<GameObject>(PlayerPrefabResourcePath);
            var projectilePrefab = Resources.Load<GameObject>("LanShooter/LanShooterProjectile");
            var enemyPrefab = Resources.Load<GameObject>(EnemyPrefabResourcePath);
            var waveDirectorObject = FindOrCreate("LanShooterSoloWaveDirector");
            if (waveDirectorObject.GetComponent<LanShooterSoloWaveDirector>() == null)
            {
                waveDirectorObject.AddComponent<LanShooterSoloWaveDirector>();
            }

            sceneContext.ApplyRuntimeDefaults(
                playerPrefab,
                projectilePrefab,
                enemyPrefab,
                spawnPoints,
                enemySpawnPoints,
                fallbackPoint != null ? fallbackPoint.transform : null);
        }

        private static void EnsureDirectionalLight()
        {
            if (Object.FindFirstObjectByType<Light>() != null)
            {
                return;
            }

            var lightObject = new GameObject("ArenaSun");
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

            var lightComponent = lightObject.AddComponent<Light>();
            lightComponent.type = LightType.Directional;
            lightComponent.intensity = 1.2f;
            lightComponent.shadows = LightShadows.Soft;
        }

        private static void EnsureMenuCamera()
        {
            if (Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length > 0)
            {
                return;
            }

            var cameraObject = new GameObject("MenuCamera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 18f, -22f);
            cameraObject.transform.rotation = Quaternion.Euler(24f, 0f, 0f);

            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 60f;
            camera.clearFlags = CameraClearFlags.Skybox;
            cameraObject.AddComponent<AudioListener>();
        }

        private static LanShooterSpawnPoint[] EnsureSpawnPoints(Transform spawnRoot)
        {
            var existing = spawnRoot.GetComponentsInChildren<LanShooterSpawnPoint>(true)
                .OrderBy(point => point.name)
                .ToArray();

            if (existing.Length > 0)
            {
                return existing;
            }

            CreateSpawnPoint(spawnRoot, "SpawnPoint_1", new Vector3(-10f, 1f, -10f), Quaternion.Euler(0f, 45f, 0f));
            CreateSpawnPoint(spawnRoot, "SpawnPoint_2", new Vector3(10f, 1f, 10f), Quaternion.Euler(0f, 225f, 0f));
            CreateSpawnPoint(spawnRoot, "SpawnPoint_3", new Vector3(-10f, 1f, 10f), Quaternion.Euler(0f, 135f, 0f));
            CreateSpawnPoint(spawnRoot, "SpawnPoint_4", new Vector3(10f, 1f, -10f), Quaternion.Euler(0f, -45f, 0f));

            return spawnRoot.GetComponentsInChildren<LanShooterSpawnPoint>(true)
                .OrderBy(point => point.name)
                .ToArray();
        }

        private static LanShooterEnemySpawnPoint[] EnsureEnemySpawnPoints(Transform spawnRoot)
        {
            var existing = spawnRoot.GetComponentsInChildren<LanShooterEnemySpawnPoint>(true)
                .OrderBy(point => point.name)
                .ToArray();

            if (existing.Length > 0)
            {
                return existing;
            }

            CreateEnemySpawnPoint(spawnRoot, "EnemySpawn_1", new Vector3(0f, 1f, 17f), Quaternion.Euler(0f, 180f, 0f));
            CreateEnemySpawnPoint(spawnRoot, "EnemySpawn_2", new Vector3(0f, 1f, -17f), Quaternion.identity);
            CreateEnemySpawnPoint(spawnRoot, "EnemySpawn_3", new Vector3(17f, 1f, 0f), Quaternion.Euler(0f, -90f, 0f));
            CreateEnemySpawnPoint(spawnRoot, "EnemySpawn_4", new Vector3(-17f, 1f, 0f), Quaternion.Euler(0f, 90f, 0f));

            return spawnRoot.GetComponentsInChildren<LanShooterEnemySpawnPoint>(true)
                .OrderBy(point => point.name)
                .ToArray();
        }

        private static void CreateArenaGeometry(Transform root)
        {
            CreatePrimitive(root, "Floor", PrimitiveType.Plane, Vector3.zero, new Vector3(4f, 1f, 4f), new Color(0.2f, 0.23f, 0.18f));
            CreatePrimitive(root, "NorthWall", PrimitiveType.Cube, new Vector3(0f, 2f, 20f), new Vector3(40f, 4f, 1f), new Color(0.45f, 0.5f, 0.55f));
            CreatePrimitive(root, "SouthWall", PrimitiveType.Cube, new Vector3(0f, 2f, -20f), new Vector3(40f, 4f, 1f), new Color(0.45f, 0.5f, 0.55f));
            CreatePrimitive(root, "EastWall", PrimitiveType.Cube, new Vector3(20f, 2f, 0f), new Vector3(1f, 4f, 40f), new Color(0.45f, 0.5f, 0.55f));
            CreatePrimitive(root, "WestWall", PrimitiveType.Cube, new Vector3(-20f, 2f, 0f), new Vector3(1f, 4f, 40f), new Color(0.45f, 0.5f, 0.55f));
            CreatePrimitive(root, "CenterCover", PrimitiveType.Cube, new Vector3(0f, 1.25f, 0f), new Vector3(4f, 2.5f, 4f), new Color(0.67f, 0.45f, 0.22f));
            CreatePrimitive(root, "BridgeCoverNorth", PrimitiveType.Cube, new Vector3(0f, 1.25f, 8f), new Vector3(10f, 2.5f, 2f), new Color(0.67f, 0.45f, 0.22f));
            CreatePrimitive(root, "BridgeCoverSouth", PrimitiveType.Cube, new Vector3(0f, 1.25f, -8f), new Vector3(10f, 2.5f, 2f), new Color(0.67f, 0.45f, 0.22f));
            CreatePrimitive(root, "BridgeCoverEast", PrimitiveType.Cube, new Vector3(8f, 1.25f, 0f), new Vector3(2f, 2.5f, 10f), new Color(0.67f, 0.45f, 0.22f));
            CreatePrimitive(root, "BridgeCoverWest", PrimitiveType.Cube, new Vector3(-8f, 1.25f, 0f), new Vector3(2f, 2.5f, 10f), new Color(0.67f, 0.45f, 0.22f));
        }

        private static void CreateSpawnPoint(Transform parent, string objectName, Vector3 position, Quaternion rotation)
        {
            var spawnObject = new GameObject(objectName);
            spawnObject.transform.SetParent(parent, false);
            spawnObject.transform.position = position;
            spawnObject.transform.rotation = rotation;
            spawnObject.AddComponent<LanShooterSpawnPoint>();
        }

        private static void CreateEnemySpawnPoint(Transform parent, string objectName, Vector3 position, Quaternion rotation)
        {
            var spawnObject = new GameObject(objectName);
            spawnObject.transform.SetParent(parent, false);
            spawnObject.transform.position = position;
            spawnObject.transform.rotation = rotation;
            spawnObject.AddComponent<LanShooterEnemySpawnPoint>();
        }

        private static void CreatePrimitive(Transform parent, string objectName, PrimitiveType primitiveType, Vector3 position, Vector3 scale, Color color)
        {
            var primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = objectName;
            primitive.transform.SetParent(parent, false);
            primitive.transform.position = position;
            primitive.transform.localScale = scale;

            var renderer = primitive.GetComponent<Renderer>();
            if (renderer != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                var material = new Material(shader);
                material.color = color;
                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", color);
                }

                renderer.sharedMaterial = material;
            }
        }

        private static GameObject FindOrCreate(string objectName)
        {
            return GameObject.Find(objectName) ?? new GameObject(objectName);
        }
    }
}
