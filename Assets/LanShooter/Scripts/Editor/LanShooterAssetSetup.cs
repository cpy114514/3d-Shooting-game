using System.Linq;
using LanShooter;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LanShooter.Editor
{
    public static class LanShooterAssetSetup
    {
        private const string ResourcesFolder = "Assets/Resources";
        private const string PlayerPrefabPath = "Assets/Resources/LanShooter/LanShooterPlayer.prefab";
        private const string ProjectilePrefabPath = "Assets/Resources/LanShooter/LanShooterProjectile.prefab";
        private const string EnemyPrefabPath = "Assets/Resources/LanShooter/LanShooterEnemy.prefab";

        [MenuItem("Tools/LAN Shooter/Regenerate Editable Assets")]
        public static void EnsureAssets()
        {
            if (Application.isPlaying)
            {
                return;
            }

            EnsureFolder("Assets", "Resources");
            EnsureFolder(ResourcesFolder, "LanShooter");
            CreateOrUpdatePlayerPrefab();
            CreateOrUpdateProjectilePrefab();
            CreateOrUpdateEnemyPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/LAN Shooter/Setup Editable Scene")]
        public static void SetupEditableScene()
        {
            EnsureAssets();

            var sceneContextObject = FindOrCreate("LanShooterSceneContext");
            var sceneContext = sceneContextObject.GetComponent<LanShooterSceneContext>() ?? sceneContextObject.AddComponent<LanShooterSceneContext>();

            var networkManagerObject = FindOrCreate("LanShooterNetworkManager");
            var networkManager = networkManagerObject.GetComponent<NetworkManager>() ?? networkManagerObject.AddComponent<NetworkManager>();
            var transport = networkManagerObject.GetComponent<UnityTransport>() ?? networkManagerObject.AddComponent<UnityTransport>();

            var systemsObject = FindOrCreate("LanShooterSystems");
            var session = systemsObject.GetComponent<LanShooterSession>() ?? systemsObject.AddComponent<LanShooterSession>();
            var hud = systemsObject.GetComponent<LanShooterHud>() ?? systemsObject.AddComponent<LanShooterHud>();

            EnsureMenuCamera();
            EnsureDirectionalLight();

            var arenaRoot = FindOrCreate("EditableArena");
            if (arenaRoot.transform.childCount == 0)
            {
                CreateArenaGeometry(arenaRoot.transform);
            }

            var spawnRoot = FindOrCreate("SpawnPoints");
            var spawnPoints = EnsureSpawnPoints(spawnRoot.transform);
            var enemySpawnRoot = FindOrCreate("EnemySpawnPoints");
            var enemySpawnPoints = EnsureEnemySpawnPoints(enemySpawnRoot.transform);
            var waveDirectorObject = FindOrCreate("LanShooterSoloWaveDirector");
            var waveDirector = waveDirectorObject.GetComponent<LanShooterSoloWaveDirector>();
            if (waveDirector == null)
            {
                waveDirector = waveDirectorObject.AddComponent<LanShooterSoloWaveDirector>();
            }

            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            var projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
            var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
            var fallbackPoint = spawnPoints.FirstOrDefault();

            sceneContext.SetEditorReferences(
                playerPrefab,
                projectilePrefab,
                enemyPrefab,
                spawnPoints,
                enemySpawnPoints,
                fallbackPoint != null ? fallbackPoint.transform : null);
            session.SetEditorReferences(networkManager, transport, sceneContext);

            EditorUtility.SetDirty(sceneContext);
            EditorUtility.SetDirty(session);
            EditorUtility.SetDirty(hud);
            EditorUtility.SetDirty(networkManager);
            EditorUtility.SetDirty(transport);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = sceneContextObject;
        }

        private static void CreateOrUpdatePlayerPrefab()
        {
            var root = new GameObject("LanShooterPlayer");
            root.AddComponent<NetworkObject>();

            var controller = root.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 1f, 0f);
            controller.height = 2f;
            controller.radius = 0.38f;
            controller.stepOffset = 0.35f;
            controller.skinWidth = 0.03f;

            root.AddComponent<LanShooterOwnerNetworkTransform>();
            var player = root.AddComponent<LanShooterPlayer>();

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader);
            var baseColor = new Color(0.68f, 0.7f, 0.76f);
            material.color = baseColor;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", baseColor);
            }

            var bodyRoot = new GameObject("BodyRoot");
            bodyRoot.transform.SetParent(root.transform, false);

            var torso = CreateBodyPart(bodyRoot.transform, "Torso", PrimitiveType.Cube, new Vector3(0f, 1.08f, 0f), new Vector3(0.56f, 0.72f, 0.3f), material);
            var hips = CreateBodyPart(bodyRoot.transform, "Hips", PrimitiveType.Cube, new Vector3(0f, 0.64f, 0f), new Vector3(0.42f, 0.24f, 0.24f), material);
            var head = CreateBodyPart(bodyRoot.transform, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.63f, 0.02f), Vector3.one * 0.28f, material);
            var leftArm = CreateBodyPart(bodyRoot.transform, "LeftArm", PrimitiveType.Cube, new Vector3(-0.42f, 1.06f, 0f), new Vector3(0.16f, 0.62f, 0.16f), material);
            var rightArm = CreateBodyPart(bodyRoot.transform, "RightArm", PrimitiveType.Cube, new Vector3(0.42f, 1.06f, 0f), new Vector3(0.16f, 0.62f, 0.16f), material);
            var leftLeg = CreateBodyPart(bodyRoot.transform, "LeftLeg", PrimitiveType.Cube, new Vector3(-0.14f, 0.18f, 0f), new Vector3(0.18f, 0.7f, 0.18f), material);
            var rightLeg = CreateBodyPart(bodyRoot.transform, "RightLeg", PrimitiveType.Cube, new Vector3(0.14f, 0.18f, 0f), new Vector3(0.18f, 0.7f, 0.18f), material);

            var pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(root.transform, false);
            pivot.transform.localPosition = new Vector3(0f, 1.55f, 0f);

            var weapon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            weapon.name = "Weapon";
            weapon.transform.SetParent(pivot.transform, false);
            weapon.transform.localPosition = new Vector3(0.28f, -0.2f, 0.42f);
            weapon.transform.localRotation = Quaternion.Euler(8f, -6f, 0f);
            weapon.transform.localScale = new Vector3(0.14f, 0.12f, 0.72f);
            Object.DestroyImmediate(weapon.GetComponent<Collider>());

            var muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(pivot.transform, false);
            muzzle.transform.localPosition = new Vector3(0.32f, -0.18f, 0.86f);

            var tintRenderers = new[]
            {
                torso.GetComponent<Renderer>(),
                hips.GetComponent<Renderer>(),
                head.GetComponent<Renderer>(),
                leftArm.GetComponent<Renderer>(),
                rightArm.GetComponent<Renderer>(),
                leftLeg.GetComponent<Renderer>(),
                rightLeg.GetComponent<Renderer>(),
                weapon.GetComponent<Renderer>(),
            };

            var localHiddenRenderers = new[]
            {
                head.GetComponent<Renderer>(),
            };

            player.SetEditorReferences(
                pivot.transform,
                weapon.transform,
                muzzle.transform,
                tintRenderers,
                localHiddenRenderers);

            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static GameObject CreateBodyPart(
            Transform parent,
            string objectName,
            PrimitiveType primitiveType,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            var part = GameObject.CreatePrimitive(primitiveType);
            part.name = objectName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = localScale;
            Object.DestroyImmediate(part.GetComponent<Collider>());

            var renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            return part;
        }

        private static void CreateOrUpdateProjectilePrefab()
        {
            var root = new GameObject("LanShooterProjectile");
            root.AddComponent<NetworkObject>();
            root.AddComponent<NetworkTransform>();
            root.AddComponent<LanShooterProjectile>();

            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one * 0.16f;
            Object.DestroyImmediate(visual.GetComponent<Collider>());

            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
                var material = new Material(shader);
                var color = new Color(1f, 0.8f, 0.2f);
                material.color = color;
                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", color);
                }

                renderer.sharedMaterial = material;
            }

            PrefabUtility.SaveAsPrefabAsset(root, ProjectilePrefabPath);
            Object.DestroyImmediate(root);
        }

        private static void CreateOrUpdateEnemyPrefab()
        {
            var root = new GameObject("LanShooterEnemy");
            root.AddComponent<NetworkObject>();
            root.AddComponent<NetworkTransform>();

            var controller = root.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 1f, 0f);
            controller.height = 2f;
            controller.radius = 0.36f;
            controller.stepOffset = 0.35f;
            controller.skinWidth = 0.03f;

            root.AddComponent<LanShooterEnemy>();

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            body.transform.localScale = new Vector3(0.9f, 1f, 0.9f);
            Object.DestroyImmediate(body.GetComponent<Collider>());

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.78f, 0f);
            head.transform.localScale = Vector3.one * 0.48f;
            Object.DestroyImmediate(head.GetComponent<Collider>());

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader);
            var color = new Color(0.86f, 0.22f, 0.16f);
            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            body.GetComponent<Renderer>().sharedMaterial = material;
            head.GetComponent<Renderer>().sharedMaterial = material;

            PrefabUtility.SaveAsPrefabAsset(root, EnemyPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static LanShooterSpawnPoint[] EnsureSpawnPoints(Transform spawnRoot)
        {
            if (spawnRoot.childCount == 0)
            {
                CreateSpawnPoint(spawnRoot, "SpawnPoint_1", new Vector3(-10f, 1f, -10f), Quaternion.Euler(0f, 45f, 0f));
                CreateSpawnPoint(spawnRoot, "SpawnPoint_2", new Vector3(10f, 1f, 10f), Quaternion.Euler(0f, 225f, 0f));
                CreateSpawnPoint(spawnRoot, "SpawnPoint_3", new Vector3(-10f, 1f, 10f), Quaternion.Euler(0f, 135f, 0f));
                CreateSpawnPoint(spawnRoot, "SpawnPoint_4", new Vector3(10f, 1f, -10f), Quaternion.Euler(0f, -45f, 0f));
            }

            return spawnRoot.GetComponentsInChildren<LanShooterSpawnPoint>(true)
                .OrderBy(point => point.name)
                .ToArray();
        }

        private static LanShooterEnemySpawnPoint[] EnsureEnemySpawnPoints(Transform spawnRoot)
        {
            if (spawnRoot.childCount == 0)
            {
                CreateEnemySpawnPoint(spawnRoot, "EnemySpawn_1", new Vector3(0f, 1f, 17f), Quaternion.Euler(0f, 180f, 0f));
                CreateEnemySpawnPoint(spawnRoot, "EnemySpawn_2", new Vector3(0f, 1f, -17f), Quaternion.identity);
                CreateEnemySpawnPoint(spawnRoot, "EnemySpawn_3", new Vector3(17f, 1f, 0f), Quaternion.Euler(0f, -90f, 0f));
                CreateEnemySpawnPoint(spawnRoot, "EnemySpawn_4", new Vector3(-17f, 1f, 0f), Quaternion.Euler(0f, 90f, 0f));
            }

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

        private static void EnsureMenuCamera()
        {
            if (Object.FindFirstObjectByType<Camera>() != null)
            {
                return;
            }

            var cameraObject = FindOrCreate("MenuCamera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 18f, -22f);
            cameraObject.transform.rotation = Quaternion.Euler(24f, 0f, 0f);

            if (cameraObject.GetComponent<Camera>() == null)
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.fieldOfView = 60f;
            }

            if (cameraObject.GetComponent<AudioListener>() == null)
            {
                cameraObject.AddComponent<AudioListener>();
            }
        }

        private static void EnsureDirectionalLight()
        {
            if (Object.FindFirstObjectByType<Light>() != null)
            {
                return;
            }

            var lightObject = FindOrCreate("ArenaSun");
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

            var light = lightObject.GetComponent<Light>() ?? lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
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
            var existing = GameObject.Find(objectName);
            if (existing != null)
            {
                return existing;
            }

            var created = new GameObject(objectName);
            Undo.RegisterCreatedObjectUndo(created, $"Create {objectName}");
            return created;
        }

        private static void EnsureFolder(string parentFolder, string childFolder)
        {
            var path = $"{parentFolder}/{childFolder}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parentFolder, childFolder);
            }
        }
    }
}
