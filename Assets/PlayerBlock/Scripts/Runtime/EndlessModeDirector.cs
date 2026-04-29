using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PlayerBlock
{
    public sealed class EndlessModeDirector : MonoBehaviour
    {
        public const string SceneName = "Endless";

        public static bool IsUpgradeSelectionOpen { get; private set; }

        [Header("Wave")]
        [SerializeField] private float firstWaveDelay = 1.2f;
        [SerializeField] private float nextWaveDelay = 0.8f;
        [SerializeField] private int baseEnemyCount = 3;
        [SerializeField] private int enemiesPerWave = 1;
        [SerializeField] private int maxEnemiesPerWave = 28;
        [SerializeField] private float spawnRadius = 17f;
        [SerializeField] private float spawnRadiusVariance = 7f;
        [SerializeField] private float spawnHeight = 4f;
        [SerializeField] private int spawnSearchAttempts = 24;
        [SerializeField] private float spawnEdgePadding = 1.2f;
        [SerializeField] private Vector3 spawnClearanceHalfExtents = new Vector3(0.85f, 1.05f, 0.85f);
        [SerializeField] private float spawnClearanceHeight = 1.15f;

        [Header("Enemy Scaling")]
        [SerializeField] private float healthGrowthPerWave = 0.12f;
        [SerializeField] private float damageGrowthPerWave = 0.06f;
        [SerializeField] private float speedGrowthPerWave = 0.025f;
        [SerializeField] private float attackSpeedGrowthPerWave = 0.025f;

        [Header("UI")]
        [SerializeField] private float upgradeUnlockDelay = 0.5f;
        [SerializeField] private GameObject upgradePanel;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text waveLabel;
        [SerializeField] private Button[] upgradeButtons = new Button[3];
        [SerializeField] private Text[] upgradeButtonLabels = new Text[3];

        private readonly List<GameObject> _enemyPrefabs = new List<GameObject>(5);
        private readonly List<UpgradeChoice> _currentChoices = new List<UpgradeChoice>(3);
        private BlockPlayerController _player;
        private bool _waitingForUpgrade;
        private bool _waveActive;
        private bool _upgradeSelectionUnlocked;
        private CursorLockMode _cursorLockBeforeUpgrade;
        private bool _cursorVisibleBeforeUpgrade;
        private int _wave;
        private float _checkTimer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            TryCreateForActiveScene(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryCreateForActiveScene(scene);
        }

        private static void TryCreateForActiveScene(Scene scene)
        {
            if (!scene.IsValid() || scene.name != SceneName)
            {
                return;
            }

            if (FindFirstObjectByType<EndlessModeDirector>() != null)
            {
                return;
            }

            var directorObject = new GameObject("EndlessModeDirector");
            directorObject.AddComponent<EndlessModeDirector>();
        }

        private void Awake()
        {
            EndlessUpgradeState.Reset();
            LoadEnemyPrefabs();
            EnsureUi();
            HideUpgradePanel();
            DisableRegularClearFlow();
        }

        private void Start()
        {
            _player = FindFirstObjectByType<BlockPlayerController>();
            RemoveSceneMinions();
            StartCoroutine(StartNextWaveAfterDelay(firstWaveDelay));
        }

        private void Update()
        {
            if (!_waveActive || _waitingForUpgrade)
            {
                return;
            }

            _checkTimer -= Time.deltaTime;
            if (_checkTimer > 0f)
            {
                return;
            }

            _checkTimer = 0.35f;
            if (!HasAliveMinions())
            {
                CompleteWave();
            }
        }

        private IEnumerator StartNextWaveAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            StartNextWave();
        }

        private void StartNextWave()
        {
            _wave++;
            _waveActive = true;
            _waitingForUpgrade = false;
            _checkTimer = 0.6f;

            var hud = CombatHud.Instance;
            hud.SetBossHealth(0f, 0f);
            hud.SetStatusMessage($"WAVE {_wave}", true);
            SpawnWave();
        }

        private void CompleteWave()
        {
            _waveActive = false;
            _waitingForUpgrade = true;
            CombatHud.Instance.SetStatusMessage($"WAVE {_wave} CLEARED", true);
            ShowUpgradeChoices();
        }

        private void SpawnWave()
        {
            if (_enemyPrefabs.Count == 0)
            {
                Debug.LogWarning("Endless mode has no enemy prefabs to spawn.");
                return;
            }

            var count = Mathf.Min(maxEnemiesPerWave, baseEnemyCount + (_wave - 1) * enemiesPerWave);
            var healthMultiplier = 1f + Mathf.Max(0, _wave - 1) * healthGrowthPerWave;
            var damageMultiplier = 1f + Mathf.Max(0, _wave - 1) * damageGrowthPerWave;
            var speedMultiplier = 1f + Mathf.Max(0, _wave - 1) * speedGrowthPerWave;
            var attackSpeedMultiplier = 1f + Mathf.Max(0, _wave - 1) * attackSpeedGrowthPerWave;

            for (var i = 0; i < count; i++)
            {
                var prefab = ChooseEnemyPrefab();
                if (prefab == null)
                {
                    continue;
                }

                var enemy = Instantiate(prefab, GetSpawnPosition(i, count), Quaternion.identity);
                enemy.name = $"{prefab.name}_Wave{_wave}";
                var minion = enemy.GetComponent<ShadowMinionController>();
                if (minion != null)
                {
                    minion.ApplyEndlessScaling(healthMultiplier, damageMultiplier, speedMultiplier, attackSpeedMultiplier);
                }
            }
        }

        private GameObject ChooseEnemyPrefab()
        {
            var unlockedKinds = GetUnlockedEnemyKinds();
            return _enemyPrefabs[UnityEngine.Random.Range(0, unlockedKinds)];
        }

        private Vector3 GetSpawnPosition(int index, int count)
        {
            var center = _player != null ? _player.transform.position : Vector3.zero;
            for (var attempt = 0; attempt < Mathf.Max(1, spawnSearchAttempts); attempt++)
            {
                var angle = (Mathf.PI * 2f * index / Mathf.Max(1, count)) + UnityEngine.Random.Range(-0.9f, 0.9f);
                var radius = spawnRadius + UnityEngine.Random.Range(-spawnRadiusVariance, spawnRadiusVariance);
                var candidate = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * Mathf.Max(4f, radius);
                var rayStart = candidate + Vector3.up * spawnHeight;

                if (TryFindPlaneSpawnPoint(rayStart, out var spawnPoint))
                {
                    return spawnPoint;
                }
            }

            if (TryFindNearestPlanePoint(center, out var fallbackPoint))
            {
                return fallbackPoint;
            }

            return center + Vector3.up * 0.5f;
        }

        private bool TryFindPlaneSpawnPoint(Vector3 rayStart, out Vector3 spawnPoint)
        {
            var hits = Physics.RaycastAll(
                rayStart,
                Vector3.down,
                spawnHeight + 24f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (var i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit.collider == null || !IsValidSpawnPlane(hit.collider) || IsTooCloseToPlaneEdge(hit))
                {
                    continue;
                }

                var candidate = hit.point + Vector3.up * 0.08f;
                if (IsSpawnAreaClear(candidate, hit.collider))
                {
                    spawnPoint = candidate;
                    return true;
                }
            }

            spawnPoint = default;
            return false;
        }

        private bool TryFindNearestPlanePoint(Vector3 center, out Vector3 spawnPoint)
        {
            var colliders = FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var bestDistance = float.PositiveInfinity;
            var bestPoint = Vector3.zero;
            var found = false;

            for (var i = 0; i < colliders.Length; i++)
            {
                var candidate = colliders[i];
                if (candidate == null || !IsValidSpawnPlane(candidate))
                {
                    continue;
                }

                var bounds = candidate.bounds;
                var point = new Vector3(
                    Mathf.Clamp(center.x, bounds.min.x + spawnEdgePadding, bounds.max.x - spawnEdgePadding),
                    bounds.max.y + 0.08f,
                    Mathf.Clamp(center.z, bounds.min.z + spawnEdgePadding, bounds.max.z - spawnEdgePadding));
                if (!IsSpawnAreaClear(point, candidate))
                {
                    continue;
                }

                var distance = (point - center).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestPoint = point;
                    found = true;
                }
            }

            spawnPoint = bestPoint;
            return found;
        }

        private bool IsSpawnAreaClear(Vector3 spawnPoint, Collider planeCollider)
        {
            var center = spawnPoint + Vector3.up * spawnClearanceHeight;
            var overlaps = Physics.OverlapBox(
                center,
                spawnClearanceHalfExtents,
                Quaternion.identity,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            for (var i = 0; i < overlaps.Length; i++)
            {
                var collider = overlaps[i];
                if (collider == null)
                {
                    continue;
                }

                if (collider == planeCollider || collider.transform.IsChildOf(planeCollider.transform) || planeCollider.transform.IsChildOf(collider.transform))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private bool IsValidSpawnPlane(Collider collider)
        {
            var current = collider.transform;
            while (current != null)
            {
                var lowerName = current.name.ToLowerInvariant();
                if (lowerName.Contains("ground") || lowerName.Contains("plane") || lowerName.Contains("floor") || lowerName.Contains("arena"))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private bool IsTooCloseToPlaneEdge(RaycastHit hit)
        {
            var bounds = hit.collider.bounds;
            return hit.point.x < bounds.min.x + spawnEdgePadding
                || hit.point.x > bounds.max.x - spawnEdgePadding
                || hit.point.z < bounds.min.z + spawnEdgePadding
                || hit.point.z > bounds.max.z - spawnEdgePadding;
        }

        private bool HasAliveMinions()
        {
            var minions = ShadowMinionController.ActiveInstances;
            for (var i = 0; i < minions.Count; i++)
            {
                if (minions[i] != null && minions[i].IsAlive)
                {
                    return true;
                }
            }

            return false;
        }

        private void ShowUpgradeChoices()
        {
            EnsureUi();
            _currentChoices.Clear();
            BuildUpgradePool(_currentChoices);
            _upgradeSelectionUnlocked = false;
            IsUpgradeSelectionOpen = true;

            if (waveLabel != null)
            {
                waveLabel.text = $"WAVE {_wave} CLEARED";
            }

            for (var i = 0; i < upgradeButtons.Length; i++)
            {
                var index = i;
                var button = upgradeButtons[i];
                if (button == null)
                {
                    continue;
                }

                var hasChoice = i < _currentChoices.Count;
                button.gameObject.SetActive(hasChoice);
                button.interactable = false;
                button.onClick.RemoveAllListeners();
                if (hasChoice)
                {
                    button.onClick.AddListener(() => ApplyUpgrade(index));
                }

                if (i < upgradeButtonLabels.Length && upgradeButtonLabels[i] != null && hasChoice)
                {
                    upgradeButtonLabels[i].text = _currentChoices[i].Title + "\n" + _currentChoices[i].Description;
                }
            }

            _cursorLockBeforeUpgrade = Cursor.lockState;
            _cursorVisibleBeforeUpgrade = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f;
            upgradePanel.SetActive(true);
            UiEffectsUtility.EnsureButtonEffects(upgradePanel.transform);
            StartCoroutine(UnlockUpgradeSelectionAfterDelay());
        }

        private void ApplyUpgrade(int index)
        {
            if (!_upgradeSelectionUnlocked || index < 0 || index >= _currentChoices.Count)
            {
                return;
            }

            _currentChoices[index].Apply?.Invoke();
            RefreshActiveShadowModifiers();
            HideUpgradePanel();
            Time.timeScale = 1f;
            Cursor.lockState = _cursorLockBeforeUpgrade;
            Cursor.visible = _cursorVisibleBeforeUpgrade;
            StartCoroutine(StartNextWaveAfterDelay(nextWaveDelay));
        }

        private IEnumerator UnlockUpgradeSelectionAfterDelay()
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, upgradeUnlockDelay));
            _upgradeSelectionUnlocked = true;
            for (var i = 0; i < upgradeButtons.Length; i++)
            {
                if (upgradeButtons[i] != null && upgradeButtons[i].gameObject.activeSelf)
                {
                    upgradeButtons[i].interactable = true;
                }
            }
        }

        private void HideUpgradePanel()
        {
            IsUpgradeSelectionOpen = false;
            if (upgradePanel != null)
            {
                upgradePanel.SetActive(false);
            }
        }

        private void BuildUpgradePool(List<UpgradeChoice> choices)
        {
            var pool = new List<UpgradeChoice>
            {
                new UpgradeChoice("ENERGY REGEN", "+0.5 energy / sec", () => _player?.AddShadowEnergyRegen(0.5f)),
                new UpgradeChoice("ENERGY MAX", "+2 max energy", () => _player?.AddShadowEnergyMax(2f, true)),
                new UpgradeChoice("MAX HEALTH", "+20 max health", () => _player?.AddMaxHealth(20f, true)),
                new UpgradeChoice("HEALTH REGEN", "+1 health / sec", () => _player?.AddEndlessHealthRegen(1f)),
                new UpgradeChoice("SHADOW DAMAGE", "+20% shadow damage", () => EndlessUpgradeState.AddShadowDamage(0.2f)),
                new UpgradeChoice("SHADOW SPEED", "+15% shadow move speed", () => EndlessUpgradeState.AddShadowMoveSpeed(0.15f)),
                new UpgradeChoice("SHADOW ATTACK", "+15% shadow attack speed", () => EndlessUpgradeState.AddShadowAttackSpeed(0.15f)),
                new UpgradeChoice("SHADOW TIME", "+4 sec shadow lifetime", () => EndlessUpgradeState.AddShadowLifetime(4f))
            };

            while (choices.Count < 3 && pool.Count > 0)
            {
                var index = UnityEngine.Random.Range(0, pool.Count);
                choices.Add(pool[index]);
                pool.RemoveAt(index);
            }
        }

        private int GetUnlockedEnemyKinds()
        {
            if (_wave <= 2)
            {
                return Mathf.Min(1, _enemyPrefabs.Count);
            }

            if (_wave <= 4)
            {
                return Mathf.Min(2, _enemyPrefabs.Count);
            }

            if (_wave <= 6)
            {
                return Mathf.Min(3, _enemyPrefabs.Count);
            }

            if (_wave <= 8)
            {
                return Mathf.Min(4, _enemyPrefabs.Count);
            }

            return _enemyPrefabs.Count;
        }

        private void RefreshActiveShadowModifiers()
        {
            var shadows = ShadowCloneTarget.ActiveInstances;
            for (var i = 0; i < shadows.Count; i++)
            {
                if (shadows[i] != null && shadows[i].IsAlive)
                {
                    shadows[i].RefreshEndlessModifiers();
                }
            }
        }

        private void LoadEnemyPrefabs()
        {
            _enemyPrefabs.Clear();
            AddEnemyPrefab("PlayerBlock/Enemies/ShadowGrunt");
            AddEnemyPrefab("PlayerBlock/Enemies/ShadowRunner");
            AddEnemyPrefab("PlayerBlock/Enemies/ShadowShooter");
            AddEnemyPrefab("PlayerBlock/Enemies/ShadowBrute");
            AddEnemyPrefab("PlayerBlock/Enemies/ShadowShielded");
        }

        private void AddEnemyPrefab(string resourcePath)
        {
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab != null)
            {
                _enemyPrefabs.Add(prefab);
            }
        }

        private void RemoveSceneMinions()
        {
            var minions = FindObjectsByType<ShadowMinionController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < minions.Length; i++)
            {
                if (minions[i] != null)
                {
                    Destroy(minions[i].gameObject);
                }
            }
        }

        private void DisableRegularClearFlow()
        {
            var sealControllers = FindObjectsByType<MinionStageSealController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < sealControllers.Length; i++)
            {
                if (sealControllers[i] != null)
                {
                    sealControllers[i].enabled = false;
                }
            }

            var seal = GameObject.Find("seal") ?? GameObject.Find("Seal");
            if (seal != null)
            {
                seal.SetActive(false);
            }
        }

        private void EnsureUi()
        {
            if (upgradePanel != null)
            {
                return;
            }

            var hud = CombatHud.Instance;
            var parent = hud != null ? hud.transform : FindFirstObjectByType<Canvas>()?.transform;
            if (parent == null)
            {
                return;
            }

            upgradePanel = new GameObject("EndlessUpgradePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(UiPanelAnimator));
            upgradePanel.layer = 5;
            upgradePanel.transform.SetParent(parent, false);

            var rect = upgradePanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(1180f, 680f);

            var image = upgradePanel.GetComponent<Image>();
            image.color = new Color(0.015f, 0.014f, 0.012f, 0.97f);
            image.raycastTarget = true;

            titleLabel = CreateText(upgradePanel.transform, "EndlessUpgradeTitle", "CHOOSE UPGRADE", 70, new Vector2(0f, 250f), new Vector2(900f, 84f));
            titleLabel.color = new Color(0.94f, 0.86f, 0.68f, 1f);
            waveLabel = CreateText(upgradePanel.transform, "EndlessWaveLabel", "WAVE CLEARED", 34, new Vector2(0f, 188f), new Vector2(820f, 54f));
            waveLabel.color = new Color(0.86f, 0.78f, 0.6f, 1f);

            upgradeButtons = new Button[3];
            upgradeButtonLabels = new Text[3];
            for (var i = 0; i < 3; i++)
            {
                var x = -360f + i * 360f;
                upgradeButtons[i] = CreateUpgradeButton(upgradePanel.transform, $"UpgradeChoice{i + 1}", new Vector2(x, -40f), out upgradeButtonLabels[i]);
            }
        }

        private static Button CreateUpgradeButton(Transform parent, string name, Vector2 position, out Text label)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(UiButtonFeedback));
            buttonObject.layer = 5;
            buttonObject.transform.SetParent(parent, false);

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(300f, 260f);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.86f, 0.78f, 0.58f, 0.98f);
            image.raycastTarget = true;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            label = CreateText(buttonObject.transform, $"{name}Label", string.Empty, 30, Vector2.zero, new Vector2(260f, 220f));
            label.color = new Color(0.025f, 0.021f, 0.016f, 1f);
            return button;
        }

        private static Text CreateText(Transform parent, string name, string value, int fontSize, Vector2 position, Vector2 size)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.layer = 5;
            textObject.transform.SetParent(parent, false);

            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var text = textObject.GetComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private readonly struct UpgradeChoice
        {
            public readonly string Title;
            public readonly string Description;
            public readonly Action Apply;

            public UpgradeChoice(string title, string description, Action apply)
            {
                Title = title;
                Description = description;
                Apply = apply;
            }
        }
    }
}
