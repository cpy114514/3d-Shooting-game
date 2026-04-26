using System.Collections.Generic;
using UnityEngine;

namespace PlayerBlock
{
    public static class CombatVfxUtility
    {
        private const int MaxActiveBurstPieces = 32;
        private const int MaxActiveFlashLights = 0;
        private const int MaxActiveDamageNumbers = 16;

        private static readonly Dictionary<string, Material> MaterialCache = new();
        private static int ActiveBurstPieces;
        private static int ActiveFlashLights;
        private static int ActiveDamageNumbers;
        private static Font DamageNumberFont;

        public static Material GetBlackBulletMaterial()
        {
            return GetOrCreateMaterial(
                "black-bullet",
                new Color(0.02f, 0.02f, 0.025f, 1f),
                null);
        }

        public static Material GetTrailMaterial()
        {
            return GetOrCreateMaterial(
                "trail",
                new Color(0.05f, 0.05f, 0.06f, 1f),
                null);
        }

        public static Material GetImpactMaterial()
        {
            return GetOrCreateMaterial(
                "impact",
                new Color(0.10f, 0.10f, 0.12f, 1f),
                new Color(0.03f, 0.015f, 0.05f));
        }

        public static TrailRenderer ConfigureTrail(GameObject projectile, float time, float startWidth, float endWidth = 0f)
        {
            if (projectile == null)
            {
                return null;
            }

            var trail = projectile.GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = projectile.AddComponent<TrailRenderer>();
            }

            trail.time = time;
            trail.startWidth = startWidth;
            trail.endWidth = endWidth;
            trail.minVertexDistance = 0.04f;
            trail.numCapVertices = 4;
            trail.numCornerVertices = 2;
            trail.alignment = LineAlignment.View;
            trail.sharedMaterial = GetTrailMaterial();
            return trail;
        }

        public static void SpawnMuzzleFlash(Vector3 position, Vector3 direction, float scale = 0.18f, int pieceCount = 6)
        {
            SpawnBurst(
                position,
                direction,
                GetOrCreateMaterial(
                    "muzzle",
                    new Color(0.84f, 0.82f, 0.92f, 1f),
                    new Color(0.16f, 0.1f, 0.22f)),
                scale,
                pieceCount,
                5.2f,
                9.8f,
                0.18f,
                0.36f,
                2.8f);
        }

        public static void SpawnImpactBurst(Vector3 position, Vector3 normal, Color color, float scale = 0.3f, int pieceCount = 7)
        {
            SpawnBurst(
                position,
                normal,
                GetOrCreateMaterial($"impact-{ColorKey(color)}", color, new Color(color.r * 0.35f, color.g * 0.2f, color.b * 0.45f)),
                scale,
                pieceCount,
                2.25f,
                6.2f,
                0.3f,
                0.65f,
                0.9f);
        }

        public static void SpawnDustBurst(Vector3 position, Vector3 normal, float scale = 0.3f, int pieceCount = 6)
        {
            SpawnBurst(
                position,
                normal,
                GetOrCreateMaterial("dust", new Color(0.16f, 0.16f, 0.17f, 1f), null),
                scale,
                pieceCount,
                1.6f,
                4.1f,
                0.35f,
                0.8f,
                0.45f);
        }

        public static void SpawnDamageNumber(Vector3 position, float amount, Color color)
        {
            if (ActiveDamageNumbers >= MaxActiveDamageNumbers)
            {
                return;
            }

            var numberObject = new GameObject("DamageNumber");
            numberObject.transform.position = position;

            var textMesh = numberObject.AddComponent<TextMesh>();
            textMesh.text = Mathf.CeilToInt(Mathf.Max(0f, amount)).ToString();
            DamageNumberFont ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textMesh.font = DamageNumberFont;
            textMesh.fontSize = 84;
            textMesh.characterSize = 0.08f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = color;

            var renderer = numberObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 5000;
            }

            var popup = numberObject.AddComponent<DamageNumberPopup>();
            popup.Initialize(color, amount);
            ActiveDamageNumbers++;
            numberObject.AddComponent<CombatVfxCounter>().Initialize(CounterKind.DamageNumber);
        }

        private static void SpawnBurst(
            Vector3 position,
            Vector3 direction,
            Material material,
            float scale,
            int pieceCount,
            float minSpeed,
            float maxSpeed,
            float minLifetime,
            float maxLifetime,
            float lightIntensity)
        {
            var baseDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.up;
            SpawnFlashLight(position, material != null ? material.color : Color.white, scale, lightIntensity);

            var availablePieces = Mathf.Max(0, MaxActiveBurstPieces - ActiveBurstPieces);
            if (availablePieces <= 0)
            {
                return;
            }

            pieceCount = Mathf.Min(pieceCount, availablePieces);

            for (var i = 0; i < pieceCount; i++)
            {
                var primitiveType = Random.value > 0.5f ? PrimitiveType.Cube : PrimitiveType.Sphere;
                var shard = GameObject.CreatePrimitive(primitiveType);
                shard.name = "CombatVfxShard";
                shard.transform.position = position + Random.insideUnitSphere * scale * 0.12f;
                shard.transform.rotation = Random.rotation;
                shard.transform.localScale = Vector3.one * Random.Range(scale * 0.08f, scale * 0.2f);

                var collider = shard.GetComponent<Collider>();
                if (collider != null)
                {
                    Object.Destroy(collider);
                }

                var renderer = shard.GetComponent<Renderer>();
                if (renderer != null && material != null)
                {
                    renderer.sharedMaterial = material;
                }

                var burstDirection = (baseDirection + Random.insideUnitSphere * 0.85f).normalized;
                var velocity = burstDirection * Random.Range(minSpeed, maxSpeed) + Vector3.up * Random.Range(0.4f, 1.5f);
                var angularVelocity = Random.insideUnitSphere * Random.Range(220f, 520f);
                ActiveBurstPieces++;
                shard.AddComponent<CombatVfxLifetime>().Initialize(
                    Random.Range(minLifetime, maxLifetime),
                    isLight: false,
                    velocity,
                    angularVelocity);
            }
        }

        private static void SpawnFlashLight(Vector3 position, Color color, float scale, float intensity)
        {
            if (intensity <= 0f || ActiveFlashLights >= MaxActiveFlashLights)
            {
                return;
            }

            var lightObject = new GameObject("CombatVfxLight");
            lightObject.transform.position = position;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = Mathf.Max(1.25f, scale * 6.5f);
            light.intensity = intensity;
            ActiveFlashLights++;
            lightObject.AddComponent<CombatVfxLifetime>().Initialize(0.11f, isLight: true);
        }

        private static Material GetOrCreateMaterial(string key, Color color, Color? emissionColor)
        {
            var cacheKey = $"{key}|{ColorKey(color)}|{(emissionColor.HasValue ? ColorKey(emissionColor.Value) : "none")}";
            if (MaterialCache.TryGetValue(cacheKey, out var cached) && cached != null)
            {
                return cached;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader)
            {
                color = color
            };

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
                if (emissionColor.HasValue)
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", emissionColor.Value);
                }
                else
                {
                    material.DisableKeyword("_EMISSION");
                }
            }

            MaterialCache[cacheKey] = material;
            return material;
        }

        private static string ColorKey(Color color)
        {
            var c = (Color32)color;
            return $"{c.r:D3}-{c.g:D3}-{c.b:D3}-{c.a:D3}";
        }

        private sealed class CombatVfxLifetime : MonoBehaviour
        {
            private float _lifeTime;
            private Vector3 _velocity;
            private Vector3 _angularVelocity;
            private bool _isLight;
            private bool _released;

            public void Initialize(float lifeTime, bool isLight)
            {
                Initialize(lifeTime, isLight, Vector3.zero, Vector3.zero);
            }

            public void Initialize(float lifeTime, bool isLight, Vector3 velocity, Vector3 angularVelocity)
            {
                _lifeTime = Mathf.Max(0.01f, lifeTime);
                _isLight = isLight;
                _velocity = velocity;
                _angularVelocity = angularVelocity;
            }

            private void Update()
            {
                if (!_isLight)
                {
                    _velocity += Physics.gravity * (0.35f * Time.deltaTime);
                    transform.position += _velocity * Time.deltaTime;
                    transform.Rotate(_angularVelocity * Time.deltaTime, Space.Self);
                }

                _lifeTime -= Time.deltaTime;
                if (_lifeTime <= 0f)
                {
                    Release();
                    Destroy(gameObject);
                }
            }

            private void OnDestroy()
            {
                Release();
            }

            private void Release()
            {
                if (_released)
                {
                    return;
                }

                _released = true;
                if (_isLight)
                {
                    ActiveFlashLights = Mathf.Max(0, ActiveFlashLights - 1);
                }
                else
                {
                    ActiveBurstPieces = Mathf.Max(0, ActiveBurstPieces - 1);
                }
            }
        }

        private enum CounterKind
        {
            DamageNumber
        }

        private sealed class CombatVfxCounter : MonoBehaviour
        {
            private CounterKind _kind;
            private bool _released;

            public void Initialize(CounterKind kind)
            {
                _kind = kind;
            }

            private void OnDestroy()
            {
                if (_released)
                {
                    return;
                }

                _released = true;
                if (_kind == CounterKind.DamageNumber)
                {
                    ActiveDamageNumbers = Mathf.Max(0, ActiveDamageNumbers - 1);
                }
            }
        }
    }
}
