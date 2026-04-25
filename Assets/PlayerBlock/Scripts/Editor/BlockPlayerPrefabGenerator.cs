using PlayerBlock;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerBlock.Editor
{
    [InitializeOnLoad]
    public static class BlockPlayerPrefabGenerator
    {
        private const string BaseFolder = "Assets/PlayerBlock";
        private const string MaterialsFolder = "Assets/PlayerBlock/Materials";
        private const string PrefabPath = "Assets/PlayerBlock/BlockPlayer.prefab";
        private const string MaterialPath = "Assets/PlayerBlock/Materials/BlockPlayer.mat";

        static BlockPlayerPrefabGenerator()
        {
            EditorApplication.delayCall += EnsurePrefab;
        }

        [MenuItem("Tools/Block Player/Generate Prefab")]
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

        [MenuItem("Tools/Block Player/Rebuild Prefab")]
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
            var root = new GameObject("BlockPlayer");
            var controller = root.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 1f, 0f);
            controller.height = 2f;
            controller.radius = 0.35f;
            controller.stepOffset = 0.35f;
            controller.skinWidth = 0.03f;

            root.AddComponent<BlockPlayerController>();

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            var material = GetOrCreateMaterial();
            CreateBodyBlock(visual.transform, "Body", new Vector3(0f, 1.1f, 0f), new Vector3(0.85f, 1.05f, 0.45f), material);
            CreateBodyBlock(visual.transform, "Head", new Vector3(0f, 1.85f, 0f), new Vector3(0.55f, 0.55f, 0.55f), material);
            CreateArm(visual.transform, "LeftArm", new Vector3(-0.68f, 1.58f, 0f), new Vector3(0.24f, 0.82f, 0.24f), material);
            CreateArm(visual.transform, "RightArm", new Vector3(0.68f, 1.58f, 0f), new Vector3(0.24f, 0.82f, 0.24f), material);
            CreateBodyBlock(visual.transform, "LeftLeg", new Vector3(-0.25f, 0.35f, 0f), new Vector3(0.32f, 0.7f, 0.32f), material);
            CreateBodyBlock(visual.transform, "RightLeg", new Vector3(0.25f, 0.35f, 0f), new Vector3(0.32f, 0.7f, 0.32f), material);

            CreateCamera(root.transform);
            CreateCrosshair(root.transform);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
        }

        private static void CreateBodyBlock(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
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

        private static void CreateCamera(Transform parent)
        {
            var cameraObject = new GameObject("ThirdPersonCamera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.localPosition = new Vector3(0.85f, 1.95f, -2.8f);
            cameraObject.transform.localRotation = Quaternion.Euler(14f, 0f, 0f);

            var camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.fieldOfView = 68f;
        }

        private static void CreateCrosshair(Transform parent)
        {
            var canvasObject = new GameObject("CrosshairCanvas");
            canvasObject.transform.SetParent(parent, false);

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();

            var color = new Color(0.72f, 0.92f, 1f, 0.92f);
            CreateCrosshairBar(canvasObject.transform, "Horizontal", new Vector2(22f, 3f), color);
            CreateCrosshairBar(canvasObject.transform, "Vertical", new Vector2(3f, 22f), color);
            CreateCrosshairDot(canvasObject.transform, color);
        }

        private static void CreateCrosshairBar(Transform parent, string name, Vector2 size, Color color)
        {
            var bar = new GameObject(name);
            bar.transform.SetParent(parent, false);

            var rectTransform = bar.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = size;

            var image = bar.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private static void CreateCrosshairDot(Transform parent, Color color)
        {
            var dot = new GameObject("Dot");
            dot.transform.SetParent(parent, false);

            var rectTransform = dot.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(5f, 5f);

            var image = dot.AddComponent<Image>();
            image.color = new Color(color.r, color.g, color.b, 1f);
            image.raycastTarget = false;
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

            var color = new Color(0.72f, 0.74f, 0.78f);
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
