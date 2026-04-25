using PlayerBlock;
using UnityEditor;
using UnityEngine;

namespace PlayerBlock.Editor
{
    public static class ShadowClonePrefabGenerator
    {
        private const string MeleePrefabPath = "Assets/Resources/PlayerBlock/ShadowClones/ShadowMelee.prefab";
        private const string RangedPrefabPath = "Assets/Resources/PlayerBlock/ShadowClones/ShadowRanged.prefab";
        private const string ShieldPrefabPath = "Assets/Resources/PlayerBlock/ShadowClones/ShadowShield.prefab";
        private const string SharedMaterialPath = "Assets/PlayerBlock/Materials/BlockPlayer.mat";

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

        [MenuItem("Tools/Block Player/Generate Shadow Prefabs")]
        public static void EnsurePrefabs()
        {
            if (Application.isPlaying)
            {
                return;
            }

            EnsureFolder("Assets", "Resources");
            EnsureFolder("Assets/Resources", "PlayerBlock");
            EnsureFolder("Assets/Resources/PlayerBlock", "ShadowClones");

            if (AssetDatabase.LoadAssetAtPath<GameObject>(MeleePrefabPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(RangedPrefabPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(ShieldPrefabPath) != null)
            {
                return;
            }

            CreateAllPrefabs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Block Player/Rebuild Shadow Prefabs")]
        public static void RebuildPrefabs()
        {
            if (Application.isPlaying)
            {
                return;
            }

            EnsureFolder("Assets", "Resources");
            EnsureFolder("Assets/Resources", "PlayerBlock");
            EnsureFolder("Assets/Resources/PlayerBlock", "ShadowClones");

            CreateAllPrefabs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateAllPrefabs()
        {
            CreatePrefab(
                ShadowCloneKind.Melee,
                MeleePrefabPath,
                includeShieldVisuals: false,
                rangedAttackAnimationDuration: 0.22f);

            CreatePrefab(
                ShadowCloneKind.Ranged,
                RangedPrefabPath,
                includeShieldVisuals: false,
                rangedAttackAnimationDuration: 0.22f);

            CreatePrefab(
                ShadowCloneKind.Shield,
                ShieldPrefabPath,
                includeShieldVisuals: true,
                rangedAttackAnimationDuration: 0.22f);
        }

        private static void CreatePrefab(
            ShadowCloneKind cloneKind,
            string prefabPath,
            bool includeShieldVisuals,
            float rangedAttackAnimationDuration)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(prefabPath);
            }

            var root = new GameObject(GetRootName(cloneKind));
            var collider = root.AddComponent<BoxCollider>();
            collider.center = cloneKind == ShadowCloneKind.Shield
                ? new Vector3(0.02f, 1.02f, 0.08f)
                : new Vector3(0f, 1f, 0f);
            collider.size = cloneKind == ShadowCloneKind.Shield
                ? new Vector3(1.48f, 2.08f, 1.05f)
                : new Vector3(1.25f, 2f, 0.75f);

            var rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.mass = 4f;
            rigidbody.useGravity = true;
            rigidbody.linearDamping = 0.8f;
            rigidbody.angularDamping = 6f;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            var target = root.AddComponent<ShadowCloneTarget>();
            ConfigureShadowTarget(target, cloneKind, rangedAttackAnimationDuration);

            var material = GetSharedMaterial();
            CreateBlock(root.transform, "Body", new Vector3(0f, 1.05f, 0f), new Vector3(0.86f, 1.08f, 0.46f), material);
            CreateBlock(root.transform, "Head", new Vector3(0f, 1.82f, 0f), new Vector3(0.56f, 0.56f, 0.56f), material);
            CreateBlock(root.transform, "LeftArm", new Vector3(-0.66f, 1.1f, 0f), new Vector3(0.24f, 0.82f, 0.24f), material);
            CreateBlock(root.transform, "RightArm", new Vector3(0.66f, 1.1f, 0f), new Vector3(0.24f, 0.82f, 0.24f), material);
            CreateBlock(root.transform, "LeftLeg", new Vector3(-0.24f, 0.34f, 0f), new Vector3(0.32f, 0.68f, 0.32f), material);
            CreateBlock(root.transform, "RightLeg", new Vector3(0.24f, 0.34f, 0f), new Vector3(0.32f, 0.68f, 0.32f), material);

            if (includeShieldVisuals)
            {
                CreateBlock(root.transform, "Shield", new Vector3(-0.52f, 1.12f, 0.36f), new Vector3(0.82f, 1.14f, 0.14f), material);
                CreateBlock(root.transform, "ShieldBoss", new Vector3(-0.16f, 0.96f, 0.42f), new Vector3(0.24f, 0.38f, 0.1f), material);
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void ConfigureShadowTarget(
            ShadowCloneTarget target,
            ShadowCloneKind cloneKind,
            float rangedAttackAnimationDuration)
        {
            var serializedTarget = new SerializedObject(target);
            SetFloat(serializedTarget, "maxHealth", 1f);
            SetFloat(serializedTarget, "lifeTime", 18f);
            SetFloat(serializedTarget, "rangedAttackRange", 18f);
            SetFloat(serializedTarget, "moveSpeed", 3.6f);
            SetFloat(serializedTarget, "shieldMoveSpeed", 2.15f);
            SetFloat(serializedTarget, "shieldHoldRange", 2.55f);
            SetFloat(serializedTarget, "rangedPreferredDistance", 14f);
            SetFloat(serializedTarget, "stopDistanceBuffer", 0.25f);
            SetFloat(serializedTarget, "moveAnimationSpeed", 8.5f);
            SetFloat(serializedTarget, "moveBobHeight", 0.08f);
            SetFloat(serializedTarget, "moveArmSwing", 48f);
            SetFloat(serializedTarget, "moveLegSwing", 42f);
            SetFloat(serializedTarget, "handHitRadius", 0.68f);
            SetFloat(serializedTarget, "meleeApproachStopDistance", 0.85f);
            SetFloat(serializedTarget, "meleeAttackStartDistance", 1.15f);
            SetFloat(serializedTarget, "meleeStrikeDuration", 0.22f);
            SetFloat(serializedTarget, "meleeRecoverDuration", 0.48f);
            SetFloat(serializedTarget, "meleeHitConfirmDistance", 1.45f);
            SetFloat(serializedTarget, "meleeHitForwardRange", 2.9f);
            SetFloat(serializedTarget, "meleeHitVerticalRange", 1.65f);
            SetFloat(serializedTarget, "meleeSpawnStunDuration", 0.25f);
            SetFloat(serializedTarget, "meleeAttackWindupDuration", 0.3f);
            SetFloat(serializedTarget, "meleeAttackDamage", 3f);
            SetFloat(serializedTarget, "rangedAttackDamage", 1f);
            SetFloat(serializedTarget, "rangedProjectileSpeed", 15f);
            SetFloat(serializedTarget, "meleeAttackCooldown", 1.0f);
            SetFloat(serializedTarget, "rangedAttackCooldown", 0.85f);
            SetFloat(serializedTarget, "rangedAttackAnimationDuration", rangedAttackAnimationDuration);
            SetFloat(serializedTarget, "crushHorizontalRadius", 1.15f);
            SetFloat(serializedTarget, "crushHeightTolerance", 0.25f);
            serializedTarget.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetFloat(SerializedObject serializedObject, string propertyName, float value)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        private static Material GetSharedMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(SharedMaterialPath);
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, SharedMaterialPath);
            }
            else if (material.shader != shader && shader != null)
            {
                material.shader = shader;
            }

            material.color = new Color(0.72f, 0.74f, 0.78f);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", material.color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", material.color);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateBlock(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = position;
            block.transform.localScale = scale;
            UnityEngine.Object.DestroyImmediate(block.GetComponent<Collider>());

            var renderer = block.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static string GetRootName(ShadowCloneKind cloneKind)
        {
            return cloneKind switch
            {
                ShadowCloneKind.Melee => "ShadowMelee",
                ShadowCloneKind.Ranged => "ShadowRanged",
                ShadowCloneKind.Shield => "ShadowShield",
                _ => "ShadowClone"
            };
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
