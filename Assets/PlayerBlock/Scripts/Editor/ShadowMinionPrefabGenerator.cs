using PlayerBlock;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PlayerBlock.Editor
{
    public static class ShadowMinionPrefabGenerator
    {
        private const string GruntPrefabPath = "Assets/Resources/PlayerBlock/Enemies/ShadowGrunt.prefab";
        private const string RunnerPrefabPath = "Assets/Resources/PlayerBlock/Enemies/ShadowRunner.prefab";
        private const string BrutePrefabPath = "Assets/Resources/PlayerBlock/Enemies/ShadowBrute.prefab";
        private const string ShooterPrefabPath = "Assets/Resources/PlayerBlock/Enemies/ShadowShooter.prefab";
        private const string ShieldedPrefabPath = "Assets/Resources/PlayerBlock/Enemies/ShadowShielded.prefab";
        private const string VisibleGruntPrefabPath = "Assets/PlayerBlock/Enemies/ShadowGrunt.prefab";
        private const string VisibleRunnerPrefabPath = "Assets/PlayerBlock/Enemies/ShadowRunner.prefab";
        private const string VisibleBrutePrefabPath = "Assets/PlayerBlock/Enemies/ShadowBrute.prefab";
        private const string VisibleShooterPrefabPath = "Assets/PlayerBlock/Enemies/ShadowShooter.prefab";
        private const string VisibleShieldedPrefabPath = "Assets/PlayerBlock/Enemies/ShadowShielded.prefab";
        private const string MaterialPath = "Assets/PlayerBlock/Materials/ShadowMinion.mat";

        [InitializeOnLoadMethod]
        private static void AutoGenerateOnLoad()
        {
            EditorApplication.delayCall += () =>
            {
                if (!Application.isPlaying)
                {
                    EnsurePrefabs();
                }
            };
        }

        [MenuItem("Tools/Block Player/Generate Shadow Minion Prefabs")]
        public static void EnsurePrefabs()
        {
            if (Application.isPlaying)
            {
                return;
            }

            EnsureFolder("Assets", "Resources");
            EnsureFolder("Assets/Resources", "PlayerBlock");
            EnsureFolder("Assets/Resources/PlayerBlock", "Enemies");
            EnsureFolder("Assets", "PlayerBlock");
            EnsureFolder("Assets/PlayerBlock", "Enemies");

            if (AssetDatabase.LoadAssetAtPath<GameObject>(GruntPrefabPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(RunnerPrefabPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(BrutePrefabPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(ShooterPrefabPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(VisibleGruntPrefabPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(VisibleRunnerPrefabPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(VisibleBrutePrefabPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(VisibleShooterPrefabPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(ShieldedPrefabPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(VisibleShieldedPrefabPath) != null
                && HasHeldSpear(ShieldedPrefabPath)
                && HasHeldSpear(VisibleShieldedPrefabPath))
            {
                return;
            }

            CreateAllPrefabs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Block Player/Rebuild Shadow Minion Prefabs")]
        public static void RebuildPrefabs()
        {
            if (Application.isPlaying)
            {
                return;
            }

            EnsureFolder("Assets", "Resources");
            EnsureFolder("Assets/Resources", "PlayerBlock");
            EnsureFolder("Assets/Resources/PlayerBlock", "Enemies");
            EnsureFolder("Assets", "PlayerBlock");
            EnsureFolder("Assets/PlayerBlock", "Enemies");
            CreateAllPrefabs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateAllPrefabs()
        {
            CreatePrefab(
                ShadowMinionKind.Grunt,
                GruntPrefabPath,
                VisibleGruntPrefabPath,
                "ShadowGrunt",
                maxHealth: 12f,
                moveSpeed: 2.9f,
                attackDamage: 8f,
                attackRange: 1.25f,
                attackCooldown: 1.05f,
                attackWindup: 0.32f,
                colliderSize: new Vector3(1.16f, 1.82f, 0.72f),
                colliderCenter: new Vector3(0f, 0.91f, 0f),
                scaleMultiplier: 0.92f);

            CreatePrefab(
                ShadowMinionKind.Runner,
                RunnerPrefabPath,
                VisibleRunnerPrefabPath,
                "ShadowRunner",
                maxHealth: 8f,
                moveSpeed: 4.15f,
                attackDamage: 6f,
                attackRange: 1.12f,
                attackCooldown: 0.82f,
                attackWindup: 0.22f,
                colliderSize: new Vector3(0.9f, 1.62f, 0.62f),
                colliderCenter: new Vector3(0f, 0.81f, 0f),
                scaleMultiplier: 0.78f);

            CreatePrefab(
                ShadowMinionKind.Brute,
                BrutePrefabPath,
                VisibleBrutePrefabPath,
                "ShadowBrute",
                maxHealth: 26f,
                moveSpeed: 2.05f,
                attackDamage: 14f,
                attackRange: 1.55f,
                attackCooldown: 1.35f,
                attackWindup: 0.48f,
                colliderSize: new Vector3(1.55f, 2.25f, 0.95f),
                colliderCenter: new Vector3(0f, 1.12f, 0f),
                scaleMultiplier: 1.16f);

            CreatePrefab(
                ShadowMinionKind.Shooter,
                ShooterPrefabPath,
                VisibleShooterPrefabPath,
                "ShadowShooter",
                maxHealth: 10f,
                moveSpeed: 2.65f,
                attackDamage: 7f,
                attackRange: 8.8f,
                attackCooldown: 1.2f,
                attackWindup: 0.4f,
                colliderSize: new Vector3(1.02f, 1.74f, 0.66f),
                colliderCenter: new Vector3(0f, 0.87f, 0f),
                scaleMultiplier: 0.84f);

            CreatePrefab(
                ShadowMinionKind.Shielded,
                ShieldedPrefabPath,
                VisibleShieldedPrefabPath,
                "ShadowShielded",
                maxHealth: 34f,
                moveSpeed: 1.95f,
                attackDamage: 11f,
                attackRange: 1.4f,
                attackCooldown: 1.2f,
                attackWindup: 0.34f,
                colliderSize: new Vector3(1.42f, 2.42f, 1.08f),
                colliderCenter: new Vector3(0f, 1.21f, 0f),
                scaleMultiplier: 1.22f);
        }

        private static void CreatePrefab(
            ShadowMinionKind kind,
            string prefabPath,
            string visiblePrefabPath,
            string rootName,
            float maxHealth,
            float moveSpeed,
            float attackDamage,
            float attackRange,
            float attackCooldown,
            float attackWindup,
            Vector3 colliderSize,
            Vector3 colliderCenter,
            float scaleMultiplier)
        {
            DeleteAssetFiles(prefabPath);
            DeleteAssetFiles(visiblePrefabPath);

            var root = new GameObject(rootName);
            var collider = root.AddComponent<BoxCollider>();
            collider.size = colliderSize;
            collider.center = colliderCenter;

            var rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.mass = kind == ShadowMinionKind.Brute ? 32f : kind == ShadowMinionKind.Shielded ? 22f : 18f;
            rigidbody.useGravity = true;
            rigidbody.linearDamping = 0.15f;
            rigidbody.angularDamping = 8f;
            rigidbody.interpolation = RigidbodyInterpolation.None;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            var minion = root.AddComponent<ShadowMinionController>();
            ConfigureMinion(minion, kind, maxHealth, moveSpeed, attackDamage, attackRange, attackCooldown, attackWindup);

            var material = GetOrCreateMaterial();
            CreateBlock(root.transform, "Body", new Vector3(0f, 1.05f, 0f) * scaleMultiplier, new Vector3(0.86f, 1.08f, 0.46f) * scaleMultiplier, material);
            CreateBlock(root.transform, "Head", new Vector3(0f, 1.82f, 0f) * scaleMultiplier, new Vector3(0.56f, 0.56f, 0.56f) * scaleMultiplier, material);
            CreateBlock(root.transform, "LeftArm", new Vector3(-0.66f, 1.1f, 0f) * scaleMultiplier, new Vector3(0.24f, 0.82f, 0.24f) * scaleMultiplier, material);
            CreateBlock(root.transform, "RightArm", new Vector3(0.66f, 1.1f, 0f) * scaleMultiplier, new Vector3(0.24f, 0.82f, 0.24f) * scaleMultiplier, material);
            CreateBlock(root.transform, "LeftLeg", new Vector3(-0.24f, 0.34f, 0f) * scaleMultiplier, new Vector3(0.32f, 0.68f, 0.32f) * scaleMultiplier, material);
            CreateBlock(root.transform, "RightLeg", new Vector3(0.24f, 0.34f, 0f) * scaleMultiplier, new Vector3(0.32f, 0.68f, 0.32f) * scaleMultiplier, material);
            if (kind == ShadowMinionKind.Shooter)
            {
                CreateBlock(root.transform, "Focus", new Vector3(0.44f, 1.18f, 0.24f) * scaleMultiplier, new Vector3(0.12f, 0.12f, 0.46f) * scaleMultiplier, material);
            }
            else if (kind == ShadowMinionKind.Shielded)
            {
                CreateBlock(root.transform, "Shield", new Vector3(-0.48f, 1.15f, 0.44f) * scaleMultiplier, new Vector3(0.9f, 1.28f, 0.18f) * scaleMultiplier, material);
                CreateBlock(root.transform, "ShieldGrip", new Vector3(-0.2f, 1.0f, 0.48f) * scaleMultiplier, new Vector3(0.24f, 0.46f, 0.12f) * scaleMultiplier, material);
                EnsureShieldCollider(root.transform.Find("Shield"));
                var rightArm = root.transform.Find("RightArm");
                CreateSpear(rightArm != null ? rightArm : root.transform, scaleMultiplier, material);
                root.transform.Find("Shield")?.gameObject.AddComponent<ShadowMinionShield>();
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.SaveAsPrefabAsset(root, visiblePrefabPath);
            Object.DestroyImmediate(root);
        }

        private static void ConfigureMinion(
            ShadowMinionController minion,
            ShadowMinionKind kind,
            float maxHealth,
            float moveSpeed,
            float attackDamage,
            float attackRange,
            float attackCooldown,
            float attackWindup)
        {
            var serializedMinion = new SerializedObject(minion);
            var kindProperty = serializedMinion.FindProperty("minionKind");
            if (kindProperty != null)
            {
                kindProperty.enumValueIndex = (int)kind;
            }

            SetFloat(serializedMinion, "maxHealth", maxHealth);
            SetFloat(serializedMinion, "moveSpeed", moveSpeed);
            SetFloat(serializedMinion, "attackDamage", attackDamage);
            SetFloat(serializedMinion, "attackRange", attackRange);
            SetFloat(serializedMinion, "attackCooldown", attackCooldown);
            SetFloat(serializedMinion, "attackWindup", attackWindup);
            SetFloat(serializedMinion, "attackStrikeDuration", 0.16f);
            SetFloat(serializedMinion, "attackRecover", kind == ShadowMinionKind.Brute ? 0.55f : kind == ShadowMinionKind.Shooter ? 0.52f : 0.42f);
            SetFloat(serializedMinion, "targetRefreshInterval", 0.25f);
            SetFloat(serializedMinion, "stopDistance", kind == ShadowMinionKind.Shooter ? 2.8f : Mathf.Max(0.68f, attackRange - 0.35f));
            SetFloat(serializedMinion, "turnSharpness", kind == ShadowMinionKind.Brute ? 9f : kind == ShadowMinionKind.Shooter ? 12f : 14f);
            SetFloat(serializedMinion, "hitHeightOffset", kind == ShadowMinionKind.Brute ? 1.25f : 1.05f);
            SetFloat(serializedMinion, "impactScale", kind == ShadowMinionKind.Brute ? 0.3f : 0.22f);
            SetFloat(serializedMinion, "moveAnimationSpeed", kind == ShadowMinionKind.Runner ? 9f : kind == ShadowMinionKind.Shooter ? 5.8f : 6.5f);
            SetFloat(serializedMinion, "moveBobHeight", kind == ShadowMinionKind.Brute ? 0.055f : kind == ShadowMinionKind.Shooter ? 0.05f : 0.07f);
            SetFloat(serializedMinion, "moveArmSwing", 44f);
            SetFloat(serializedMinion, "moveLegSwing", kind == ShadowMinionKind.Runner ? 54f : kind == ShadowMinionKind.Shooter ? 30f : 38f);
            SetFloat(serializedMinion, "rangedAttackRange", kind == ShadowMinionKind.Shooter ? 12f : 11.5f);
            SetFloat(serializedMinion, "rangedPreferredDistance", kind == ShadowMinionKind.Shooter ? 8.4f : 8.2f);
            SetFloat(serializedMinion, "rangedRetreatDistance", kind == ShadowMinionKind.Shooter ? 4.8f : 4.6f);
            SetFloat(serializedMinion, "rangedProjectileSpeed", kind == ShadowMinionKind.Shooter ? 16f : 15f);
            SetFloat(serializedMinion, "rangedProjectileScale", kind == ShadowMinionKind.Shooter ? 0.24f : 0.22f);
            SetFloat(serializedMinion, "rangedStopDistanceBuffer", kind == ShadowMinionKind.Shooter ? 0.7f : 0.55f);
            if (kind == ShadowMinionKind.Shielded)
            {
                SetFloat(serializedMinion, "attackRecover", 0.56f);
                SetFloat(serializedMinion, "attackRange", 1.95f);
                SetFloat(serializedMinion, "stopDistance", 1.18f);
                SetFloat(serializedMinion, "turnSharpness", 10f);
                SetFloat(serializedMinion, "hitHeightOffset", 1.22f);
                SetFloat(serializedMinion, "impactScale", 0.28f);
                SetFloat(serializedMinion, "moveAnimationSpeed", 5.4f);
                SetFloat(serializedMinion, "moveBobHeight", 0.05f);
                SetFloat(serializedMinion, "moveArmSwing", 28f);
                SetFloat(serializedMinion, "moveLegSwing", 26f);
            }
            serializedMinion.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(minion);
        }

        private static void SetFloat(SerializedObject serializedObject, string propertyName, float value)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        private static void CreateBlock(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = position;
            block.transform.localScale = scale;
            Object.DestroyImmediate(block.GetComponent<Collider>());

            var renderer = block.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static void CreateSpear(Transform parent, float scaleMultiplier, Material material)
        {
            var spearRoot = new GameObject("Spear");
            spearRoot.transform.SetParent(parent, false);
            spearRoot.transform.localPosition = new Vector3(0.2f, -0.3f, 0.54f);
            spearRoot.transform.localRotation = Quaternion.Euler(-4f, 8f, -2f);
            spearRoot.transform.localScale = GetInverseLocalScale(parent);

            var shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shaft.name = "Shaft";
            shaft.transform.SetParent(spearRoot.transform, false);
            shaft.transform.localPosition = new Vector3(0f, 0f, 0.78f);
            shaft.transform.localScale = new Vector3(0.1f, 0.1f, 1.72f) * scaleMultiplier;
            Object.DestroyImmediate(shaft.GetComponent<Collider>());
            var shaftRenderer = shaft.GetComponent<Renderer>();
            if (shaftRenderer != null)
            {
                shaftRenderer.sharedMaterial = material;
            }

            var tip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tip.name = "Tip";
            tip.transform.SetParent(spearRoot.transform, false);
            tip.transform.localPosition = new Vector3(0f, 0f, 1.72f);
            tip.transform.localScale = new Vector3(0.2f, 0.2f, 0.38f) * scaleMultiplier;
            Object.DestroyImmediate(tip.GetComponent<Collider>());
            var tipRenderer = tip.GetComponent<Renderer>();
            if (tipRenderer != null)
            {
                tipRenderer.sharedMaterial = material;
            }
        }

        private static void EnsureShieldCollider(Transform shield)
        {
            if (shield == null)
            {
                return;
            }

            var collider = shield.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = shield.gameObject.AddComponent<BoxCollider>();
            }

            collider.isTrigger = false;

            var renderer = shield.GetComponent<Renderer>();
            if (renderer != null)
            {
                var bounds = renderer.bounds;
                var scale = shield.lossyScale;
                var localCenter = shield.InverseTransformPoint(bounds.center);
                var localSize = new Vector3(
                    Mathf.Abs(scale.x) > 0.0001f ? bounds.size.x / Mathf.Abs(scale.x) : 1f,
                    Mathf.Abs(scale.y) > 0.0001f ? bounds.size.y / Mathf.Abs(scale.y) : 1f,
                    Mathf.Abs(scale.z) > 0.0001f ? bounds.size.z / Mathf.Abs(scale.z) : 1f);

                localSize.x = Mathf.Max(localSize.x * 1.05f, 1.15f);
                localSize.y = Mathf.Max(localSize.y * 1.05f, 1.65f);
                localSize.z = Mathf.Max(localSize.z * 4.5f, 0.7f);

                collider.center = localCenter + new Vector3(0f, 0f, 0.12f);
                collider.size = localSize;
                return;
            }

            collider.center = new Vector3(0f, 0f, 0.12f);
            collider.size = new Vector3(1.25f, 1.85f, 2.2f);
        }

        private static Vector3 GetInverseLocalScale(Transform target)
        {
            var scale = target != null ? target.localScale : Vector3.one;
            return new Vector3(
                Mathf.Abs(scale.x) > 0.001f ? 1f / scale.x : 1f,
                Mathf.Abs(scale.y) > 0.001f ? 1f / scale.y : 1f,
                Mathf.Abs(scale.z) > 0.001f ? 1f / scale.z : 1f);
        }

        private static bool HasHeldSpear(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            return prefab != null && prefab.transform.Find("RightArm/Spear") != null;
        }

        private static Material GetOrCreateMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else if (material.shader != shader && shader != null)
            {
                material.shader = shader;
            }

            var color = new Color(0.42f, 0.42f, 0.42f, 1f);
            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(0.02f, 0.02f, 0.02f, 1f));
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void DeleteAssetFiles(string assetPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
            else if (File.Exists(assetPath))
            {
                File.Delete(assetPath);
            }

            var metaPath = $"{assetPath}.meta";
            if (File.Exists(metaPath))
            {
                File.Delete(metaPath);
            }
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
