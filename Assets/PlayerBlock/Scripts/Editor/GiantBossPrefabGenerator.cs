using PlayerBlock;
using UnityEditor;
using UnityEngine;

namespace PlayerBlock.Editor
{
    public static class GiantBossPrefabGenerator
    {
        private const string BaseFolder = "Assets/PlayerBlock";
        private const string MaterialsFolder = "Assets/PlayerBlock/Materials";
        private const string PrefabPath = "Assets/PlayerBlock/GiantBoss.prefab";
        private const string MaterialPath = "Assets/PlayerBlock/Materials/GiantBoss.mat";

        [MenuItem("Tools/Block Player/Generate Giant Boss")]
        public static void EnsurePrefab()
        {
            if (Application.isPlaying)
            {
                return;
            }

            EnsureFolder("Assets", "PlayerBlock");
            EnsureFolder(BaseFolder, "Materials");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab != null)
            {
                return;
            }

            CreatePrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Block Player/Rebuild Giant Boss")]
        public static void RebuildPrefab()
        {
            if (Application.isPlaying)
            {
                return;
            }

            EnsureFolder("Assets", "PlayerBlock");
            EnsureFolder(BaseFolder, "Materials");
            CreatePrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreatePrefab()
        {
            var root = new GameObject("GiantBoss");
            var controller = root.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 2.2f, 0f);
            controller.height = 4.4f;
            controller.radius = 1.15f;
            controller.stepOffset = 0.45f;
            controller.skinWidth = 0.06f;

            root.AddComponent<GiantBossController>();

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, -0.08f, 0f);
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            var material = GetOrCreateMaterial();
            CreateBlock(visual.transform, "Body", new Vector3(0f, 2.15f, 0f), new Vector3(2.05f, 2.35f, 1.1f), material);
            CreateBlock(visual.transform, "Head", new Vector3(0f, 3.75f, 0f), new Vector3(1.25f, 1.05f, 1.05f), material);
            CreateArm(visual.transform, "LeftArm", new Vector3(-1.45f, 3.0f, 0f), new Vector3(0.55f, 2.15f, 0.55f), material);
            CreateArm(visual.transform, "RightArm", new Vector3(1.45f, 3.0f, 0f), new Vector3(0.55f, 2.15f, 0.55f), material);
            CreateBlock(visual.transform, "LeftLeg", new Vector3(-0.55f, 0.8f, 0f), new Vector3(0.6f, 1.6f, 0.62f), material);
            CreateBlock(visual.transform, "RightLeg", new Vector3(0.55f, 0.8f, 0f), new Vector3(0.6f, 1.6f, 0.62f), material);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
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

        private static void CreateArm(Transform parent, string name, Vector3 pivotPosition, Vector3 armScale, Material material)
        {
            var pivot = new GameObject(name);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = pivotPosition;
            pivot.transform.localRotation = Quaternion.identity;
            pivot.transform.localScale = Vector3.one;

            var armMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            armMesh.name = $"{name}Mesh";
            armMesh.transform.SetParent(pivot.transform, false);
            armMesh.transform.localPosition = new Vector3(0f, -armScale.y * 0.5f, 0f);
            armMesh.transform.localScale = armScale;
            Object.DestroyImmediate(armMesh.GetComponent<Collider>());

            var renderer = armMesh.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
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

            var color = new Color(0.48f, 0.34f, 0.26f);
            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            EditorUtility.SetDirty(material);
            return material;
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
