using UnityEngine;
using UnityEngine.UI;

namespace PlayerBlock
{
    public sealed class CombatHud : MonoBehaviour
    {
        private static CombatHud _instance;

        [SerializeField] private Image playerFill;
        [SerializeField] private Image playerEnergyFill;
        [SerializeField] private Image bossFill;
        [SerializeField] private Image bossPanel;
        [SerializeField] private Text playerLabel;
        [SerializeField] private Text playerEnergyLabel;
        [SerializeField] private Text bossLabel;
        [SerializeField] private Text phaseLabel;
        [SerializeField] private Text selectedShadowLabel;
        [SerializeField] private Image meleeShadowSlot;
        [SerializeField] private Image rangedShadowSlot;
        [SerializeField] private Image emptyHandsSlot;
        [SerializeField] private Text meleeShadowSlotLabel;
        [SerializeField] private Text rangedShadowSlotLabel;
        [SerializeField] private Text emptyHandsSlotLabel;
        [SerializeField] private Text meleeShadowCostLabel;
        [SerializeField] private Text rangedShadowCostLabel;
        [SerializeField] private Text emptyHandsCostLabel;
        [SerializeField] private Image crosshairCooldownFill;
        [SerializeField] private Image[] phaseSegments;

        public static CombatHud Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

                _instance = FindFirstObjectByType<CombatHud>();
                if (_instance != null)
                {
                    return _instance;
                }

                var hudObject = new GameObject("PlayerBlockCombatHud");
                _instance = hudObject.AddComponent<CombatHud>();
                return _instance;
            }
        }

        public void SetPlayerHealth(float health, float maxHealth)
        {
            EnsureUi();
            SetFill(playerFill, health, maxHealth);
            if (playerLabel != null)
            {
                playerLabel.text = $"PLAYER  {Mathf.CeilToInt(Mathf.Max(0f, health))}/{Mathf.CeilToInt(maxHealth)}";
            }
        }

        public void SetPlayerEnergy(float energy, float maxEnergy)
        {
            EnsureUi();
            SetFill(playerEnergyFill, energy, maxEnergy);
            if (playerEnergyLabel != null)
            {
                playerEnergyLabel.text = $"ENERGY  {Mathf.FloorToInt(Mathf.Max(0f, energy))}/{Mathf.CeilToInt(maxEnergy)}";
            }
        }

        public void SetBossHealth(float health, float maxHealth)
        {
            EnsureUi();
            SetBossVisible(maxHealth > 0f);
            SetFill(bossFill, health, maxHealth);
            if (bossLabel != null)
            {
                bossLabel.text = $"GIANT BOSS  {Mathf.CeilToInt(Mathf.Max(0f, health))}/{Mathf.CeilToInt(maxHealth)}";
            }
        }

        public void SetBossPhase(int phase)
        {
            EnsureUi();
            phase = Mathf.Clamp(phase, 1, 3);

            if (phaseLabel != null)
            {
                phaseLabel.text = phase == 1
                    ? "PHASE 1: ARM SLAM"
                    : phase == 2
                        ? "PHASE 2: JUMP SLAM"
                        : "PHASE 3: CHARGE";
            }

            if (phaseSegments == null)
            {
                return;
            }

            for (var i = 0; i < phaseSegments.Length; i++)
            {
                if (phaseSegments[i] != null)
                {
                    phaseSegments[i].color = i + 1 == phase
                        ? new Color(1f, 0.62f, 0.12f, 1f)
                        : new Color(0.28f, 0.19f, 0.14f, 0.9f);
                }
            }
        }

        public void SetSelectedShadow(string shadowName)
        {
            EnsureUi();
            if (selectedShadowLabel != null)
            {
                selectedShadowLabel.text = "INVENTORY";
            }
        }

        public void SetSelectedShadowInventory(CombatSelectionKind selectedKind, float meleeCost, float rangedCost)
        {
            EnsureUi();
            if (selectedShadowLabel != null)
            {
                selectedShadowLabel.text = "INVENTORY";
            }

            SetShadowSlotVisual(meleeShadowSlot, selectedKind == CombatSelectionKind.Melee);
            SetShadowSlotVisual(rangedShadowSlot, selectedKind == CombatSelectionKind.Ranged);
            SetShadowSlotVisual(emptyHandsSlot, selectedKind == CombatSelectionKind.Hands);

            if (meleeShadowSlotLabel != null)
            {
                meleeShadowSlotLabel.text = "1\nMELEE";
            }

            if (rangedShadowSlotLabel != null)
            {
                rangedShadowSlotLabel.text = "2\nRANGED";
            }

            if (emptyHandsSlotLabel != null)
            {
                emptyHandsSlotLabel.text = "3\nEMPTY";
            }

            if (meleeShadowCostLabel != null)
            {
                meleeShadowCostLabel.text = $"{meleeCost:0.#} ENERGY";
            }

            if (rangedShadowCostLabel != null)
            {
                rangedShadowCostLabel.text = $"{rangedCost:0.#} ENERGY";
            }

            if (emptyHandsCostLabel != null)
            {
                emptyHandsCostLabel.text = "0 ENERGY";
            }
        }

        public void SetShadowCooldownProgress(string shadowName, float remainingTime, float cooldownDuration)
        {
            EnsureUi();
            HideLegacyCrosshairCooldownPanel();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            EnsureUi();
        }

        public void EnsureEditableUi()
        {
            EnsureUi();
        }

        private void EnsureUi()
        {
            FindExistingUiReferences();
            if (playerFill != null
                && playerEnergyFill != null
                && bossFill != null
                && selectedShadowLabel != null
                && meleeShadowSlot != null
                && rangedShadowSlot != null
                && emptyHandsSlot != null)
            {
                HideLegacyCrosshairCooldownPanel();
                return;
            }

            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 35;

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            if (playerFill == null)
            {
                BuildPlayerPanel(transform);
            }
            else if (playerEnergyFill == null)
            {
                BuildPlayerEnergyBar();
            }

            if (bossFill == null)
            {
                BuildBossPanel(transform);
            }

            if (selectedShadowLabel == null || meleeShadowSlot == null || rangedShadowSlot == null || emptyHandsSlot == null)
            {
                BuildShadowSelectionPanel(transform);
            }

            HideLegacyCrosshairCooldownPanel();
        }

        private void BuildPlayerPanel(Transform parent)
        {
            var panel = CreateImage(parent, "PlayerHealthPanel", new Color(0.02f, 0.04f, 0.05f, 0.78f));
            var rect = panel.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(42f, 42f);
            rect.sizeDelta = new Vector2(430f, 128f);

            playerLabel = CreateText(panel.transform, "PlayerLabel", "PLAYER  100/100", 30, TextAnchor.MiddleLeft);
            SetRect(playerLabel.rectTransform, new Vector2(24f, 88f), new Vector2(382f, 32f), new Vector2(0f, 0f), new Vector2(0f, 0f));

            BuildPlayerEnergyBar();

            var back = CreateImage(panel.transform, "PlayerHealthBack", new Color(0f, 0f, 0f, 0.82f));
            SetRect(back.rectTransform, new Vector2(24f, 18f), new Vector2(382f, 20f), new Vector2(0f, 0f), new Vector2(0f, 0f));

            playerFill = CreateImage(back.transform, "PlayerHealthFill", new Color(0.18f, 1f, 0.32f, 1f));
            StretchFill(playerFill.rectTransform);
        }

        private void BuildPlayerEnergyBar()
        {
            var panelTransform = transform.Find("PlayerHealthPanel");
            if (panelTransform == null)
            {
                return;
            }

            var panelRect = panelTransform.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.sizeDelta = new Vector2(430f, 128f);
            }

            if (playerLabel != null)
            {
                SetRect(playerLabel.rectTransform, new Vector2(24f, 88f), new Vector2(382f, 32f), new Vector2(0f, 0f), new Vector2(0f, 0f));
            }

            var healthBack = panelTransform.Find("PlayerHealthBack") as RectTransform;
            if (healthBack != null)
            {
                SetRect(healthBack, new Vector2(24f, 18f), new Vector2(382f, 20f), new Vector2(0f, 0f), new Vector2(0f, 0f));
            }

            var energyBackTransform = panelTransform.Find("PlayerEnergyBack");
            Image energyBack;
            if (energyBackTransform == null)
            {
                energyBack = CreateImage(panelTransform, "PlayerEnergyBack", new Color(0f, 0f, 0f, 0.82f));
            }
            else
            {
                energyBack = energyBackTransform.GetComponent<Image>();
            }

            if (energyBack != null)
            {
                SetRect(energyBack.rectTransform, new Vector2(24f, 50f), new Vector2(382f, 18f), new Vector2(0f, 0f), new Vector2(0f, 0f));
            }

            if (playerEnergyFill == null)
            {
                var energyFillTransform = energyBack != null ? energyBack.transform.Find("PlayerEnergyFill") : null;
                playerEnergyFill = energyFillTransform != null
                    ? energyFillTransform.GetComponent<Image>()
                    : energyBack != null
                        ? CreateImage(energyBack.transform, "PlayerEnergyFill", new Color(0.08f, 0.48f, 1f, 1f))
                        : null;

                if (playerEnergyFill != null)
                {
                    StretchFill(playerEnergyFill.rectTransform);
                }
            }

            if (playerEnergyLabel == null)
            {
                var labelTransform = panelTransform.Find("PlayerEnergyLabel");
                playerEnergyLabel = labelTransform != null
                    ? labelTransform.GetComponent<Text>()
                    : CreateText(panelTransform, "PlayerEnergyLabel", "ENERGY  10/10", 20, TextAnchor.MiddleLeft);
            }

            if (playerEnergyLabel != null)
            {
                SetRect(playerEnergyLabel.rectTransform, new Vector2(24f, 64f), new Vector2(382f, 22f), new Vector2(0f, 0f), new Vector2(0f, 0f));
            }
        }

        private void BuildBossPanel(Transform parent)
        {
            bossPanel = CreateImage(parent, "BossHealthPanel", new Color(0.06f, 0.02f, 0.015f, 0.82f));
            var rect = bossPanel.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -34f);
            rect.sizeDelta = new Vector2(860f, 126f);

            bossLabel = CreateText(bossPanel.transform, "BossLabel", "GIANT BOSS  180/180", 34, TextAnchor.MiddleCenter);
            SetRect(bossLabel.rectTransform, new Vector2(0f, -18f), new Vector2(780f, 38f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));

            var back = CreateImage(bossPanel.transform, "BossHealthBack", new Color(0f, 0f, 0f, 0.86f));
            SetRect(back.rectTransform, new Vector2(0f, -60f), new Vector2(780f, 24f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));

            bossFill = CreateImage(back.transform, "BossHealthFill", new Color(1f, 0.16f, 0.1f, 1f));
            StretchFill(bossFill.rectTransform);

            phaseLabel = CreateText(bossPanel.transform, "BossPhaseLabel", "PHASE 1: ARM SLAM", 24, TextAnchor.MiddleCenter);
            SetRect(phaseLabel.rectTransform, new Vector2(0f, -93f), new Vector2(360f, 26f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));

            phaseSegments = new Image[3];
            for (var i = 0; i < phaseSegments.Length; i++)
            {
                var segment = CreateImage(bossPanel.transform, $"BossPhase{i + 1}", new Color(0.28f, 0.19f, 0.14f, 0.9f));
                SetRect(segment.rectTransform, new Vector2(-92f + i * 92f, -95f), new Vector2(72f, 16f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
                phaseSegments[i] = segment;
            }

            SetBossPhase(1);
            SetBossVisible(false);
        }

        private void BuildShadowSelectionPanel(Transform parent)
        {
            var existingPanel = parent.Find("ShadowSelectionPanel");
            var panel = existingPanel != null
                ? existingPanel.GetComponent<Image>()
                : CreateImage(parent, "ShadowSelectionPanel", new Color(0.015f, 0.018f, 0.02f, 0.82f));
            if (panel == null)
            {
                panel = existingPanel.gameObject.AddComponent<Image>();
                panel.color = new Color(0.015f, 0.018f, 0.02f, 0.82f);
            }

            var rect = panel.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 42f);
            rect.sizeDelta = new Vector2(520f, 142f);

            if (selectedShadowLabel == null)
            {
                var labelTransform = panel.transform.Find("SelectedShadowLabel");
                selectedShadowLabel = labelTransform != null
                    ? labelTransform.GetComponent<Text>()
                    : CreateText(panel.transform, "SelectedShadowLabel", "INVENTORY", 22, TextAnchor.MiddleCenter);
            }

            SetRect(selectedShadowLabel.rectTransform, new Vector2(0f, 112f), new Vector2(320f, 24f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            BuildShadowSlot(panel.transform, CombatSelectionKind.Melee, new Vector2(-162f, 54f));
            BuildShadowSlot(panel.transform, CombatSelectionKind.Ranged, new Vector2(0f, 54f));
            BuildShadowSlot(panel.transform, CombatSelectionKind.Hands, new Vector2(162f, 54f));
            SetSelectedShadowInventory(CombatSelectionKind.Melee, 1f, 2f);
        }

        private void BuildShadowSlot(Transform parent, CombatSelectionKind kind, Vector2 position)
        {
            var slotName = kind == CombatSelectionKind.Melee
                ? "MeleeShadowSlot"
                : kind == CombatSelectionKind.Ranged
                    ? "RangedShadowSlot"
                    : "EmptyHandsSlot";
            var labelName = kind == CombatSelectionKind.Melee
                ? "MeleeShadowSlotLabel"
                : kind == CombatSelectionKind.Ranged
                    ? "RangedShadowSlotLabel"
                    : "EmptyHandsSlotLabel";
            var costName = kind == CombatSelectionKind.Melee
                ? "MeleeShadowCostLabel"
                : kind == CombatSelectionKind.Ranged
                    ? "RangedShadowCostLabel"
                    : "EmptyHandsCostLabel";
            var slotTransform = parent.Find(slotName);
            var slot = slotTransform != null
                ? slotTransform.GetComponent<Image>()
                : CreateImage(parent, slotName, new Color(0.035f, 0.04f, 0.05f, 0.92f));

            if (slot != null)
            {
                SetRect(slot.rectTransform, position, new Vector2(132f, 92f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            }

            var labelTransform = slot != null ? slot.transform.Find(labelName) : null;
            var slotLabel = labelTransform != null
                ? labelTransform.GetComponent<Text>()
                : slot != null
                    ? CreateText(slot.transform, labelName,
                        kind == CombatSelectionKind.Melee ? "1\nMELEE" : kind == CombatSelectionKind.Ranged ? "2\nRANGED" : "3\nEMPTY",
                        22,
                        TextAnchor.MiddleCenter)
                    : null;

            var costTransform = slot != null ? slot.transform.Find(costName) : null;
            var costLabel = costTransform != null
                ? costTransform.GetComponent<Text>()
                : slot != null
                    ? CreateText(slot.transform, costName,
                        kind == CombatSelectionKind.Melee ? "1 ENERGY" : kind == CombatSelectionKind.Ranged ? "2 ENERGY" : "0 ENERGY",
                        15,
                        TextAnchor.MiddleCenter)
                    : null;

            if (slotLabel != null)
            {
                SetRect(slotLabel.rectTransform, new Vector2(0f, 16f), new Vector2(104f, 50f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            }

            if (costLabel != null)
            {
                costLabel.color = new Color(0.35f, 0.72f, 1f, 1f);
                SetRect(costLabel.rectTransform, new Vector2(0f, -30f), new Vector2(110f, 22f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            }

            if (kind == CombatSelectionKind.Melee)
            {
                meleeShadowSlot = slot;
                meleeShadowSlotLabel = slotLabel;
                meleeShadowCostLabel = costLabel;
            }
            else if (kind == CombatSelectionKind.Ranged)
            {
                rangedShadowSlot = slot;
                rangedShadowSlotLabel = slotLabel;
                rangedShadowCostLabel = costLabel;
            }
            else
            {
                emptyHandsSlot = slot;
                emptyHandsSlotLabel = slotLabel;
                emptyHandsCostLabel = costLabel;
            }
        }

        private void BuildCrosshairCooldownPanel(Transform parent)
        {
            var panel = CreateImage(parent, "CrosshairCooldownPanel", new Color(0.01f, 0.012f, 0.014f, 0.68f));
            var rect = panel.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(34f, -28f);
            rect.sizeDelta = new Vector2(150f, 18f);

            var back = CreateImage(panel.transform, "CrosshairCooldownBack", new Color(0f, 0f, 0f, 0.82f));
            SetRect(back.rectTransform, Vector2.zero, new Vector2(134f, 8f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            crosshairCooldownFill = CreateImage(back.transform, "CrosshairCooldownFill", new Color(0.22f, 1f, 0.48f, 0.95f));
            StretchFill(crosshairCooldownFill.rectTransform);
        }

        private void SetBossVisible(bool visible)
        {
            if (bossPanel != null)
            {
                bossPanel.gameObject.SetActive(visible);
            }
        }

        private void FindExistingUiReferences()
        {
            playerFill = playerFill != null ? playerFill : FindImage("PlayerHealthPanel/PlayerHealthBack/PlayerHealthFill");
            playerEnergyFill = playerEnergyFill != null ? playerEnergyFill : FindImage("PlayerHealthPanel/PlayerEnergyBack/PlayerEnergyFill");
            bossFill = bossFill != null ? bossFill : FindImage("BossHealthPanel/BossHealthBack/BossHealthFill");
            bossPanel = bossPanel != null ? bossPanel : FindImage("BossHealthPanel");
            playerLabel = playerLabel != null ? playerLabel : FindText("PlayerHealthPanel/PlayerLabel");
            playerEnergyLabel = playerEnergyLabel != null ? playerEnergyLabel : FindText("PlayerHealthPanel/PlayerEnergyLabel");
            bossLabel = bossLabel != null ? bossLabel : FindText("BossHealthPanel/BossLabel");
            phaseLabel = phaseLabel != null ? phaseLabel : FindText("BossHealthPanel/BossPhaseLabel");
            selectedShadowLabel = selectedShadowLabel != null ? selectedShadowLabel : FindText("ShadowSelectionPanel/SelectedShadowLabel");
            meleeShadowSlot = meleeShadowSlot != null ? meleeShadowSlot : FindImage("ShadowSelectionPanel/MeleeShadowSlot");
            rangedShadowSlot = rangedShadowSlot != null ? rangedShadowSlot : FindImage("ShadowSelectionPanel/RangedShadowSlot");
            emptyHandsSlot = emptyHandsSlot != null ? emptyHandsSlot : FindImage("ShadowSelectionPanel/EmptyHandsSlot");
            meleeShadowSlotLabel = meleeShadowSlotLabel != null ? meleeShadowSlotLabel : FindText("ShadowSelectionPanel/MeleeShadowSlot/MeleeShadowSlotLabel");
            rangedShadowSlotLabel = rangedShadowSlotLabel != null ? rangedShadowSlotLabel : FindText("ShadowSelectionPanel/RangedShadowSlot/RangedShadowSlotLabel");
            emptyHandsSlotLabel = emptyHandsSlotLabel != null ? emptyHandsSlotLabel : FindText("ShadowSelectionPanel/EmptyHandsSlot/EmptyHandsSlotLabel");
            meleeShadowCostLabel = meleeShadowCostLabel != null ? meleeShadowCostLabel : FindText("ShadowSelectionPanel/MeleeShadowSlot/MeleeShadowCostLabel");
            rangedShadowCostLabel = rangedShadowCostLabel != null ? rangedShadowCostLabel : FindText("ShadowSelectionPanel/RangedShadowSlot/RangedShadowCostLabel");
            emptyHandsCostLabel = emptyHandsCostLabel != null ? emptyHandsCostLabel : FindText("ShadowSelectionPanel/EmptyHandsSlot/EmptyHandsCostLabel");
            crosshairCooldownFill = crosshairCooldownFill != null ? crosshairCooldownFill : FindImage("CrosshairCooldownPanel/CrosshairCooldownBack/CrosshairCooldownFill");

            if (phaseSegments == null || phaseSegments.Length != 3)
            {
                phaseSegments = new Image[3];
            }

            for (var i = 0; i < phaseSegments.Length; i++)
            {
                phaseSegments[i] = phaseSegments[i] != null
                    ? phaseSegments[i]
                    : FindImage($"BossHealthPanel/BossPhase{i + 1}");
            }
        }

        private Image FindImage(string path)
        {
            var child = transform.Find(path);
            return child != null ? child.GetComponent<Image>() : null;
        }

        private Text FindText(string path)
        {
            var child = transform.Find(path);
            return child != null ? child.GetComponent<Text>() : null;
        }

        private void HideLegacyCrosshairCooldownPanel()
        {
            var legacyPanel = transform.Find("CrosshairCooldownPanel");
            if (legacyPanel != null)
            {
                legacyPanel.gameObject.SetActive(false);
            }

            var legacyLabel = FindText("CrosshairCooldownPanel/CrosshairCooldownLabel");
            if (legacyLabel != null)
            {
                legacyLabel.gameObject.SetActive(false);
            }
        }

        private static void SetFill(Image fill, float health, float maxHealth)
        {
            if (fill == null)
            {
                return;
            }

            var percent = maxHealth <= 0f ? 0f : Mathf.Clamp01(health / maxHealth);
            fill.rectTransform.anchorMax = new Vector2(percent, 1f);
        }

        private static void SetShadowSlotVisual(Image slot, bool selected)
        {
            if (slot == null)
            {
                return;
            }

            slot.color = selected
                ? new Color(0.12f, 0.36f, 0.68f, 0.96f)
                : new Color(0.035f, 0.04f, 0.05f, 0.92f);
            slot.rectTransform.localScale = selected ? Vector3.one * 1.08f : Vector3.one;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            var image = child.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateText(Transform parent, string name, string text, int fontSize, TextAnchor alignment)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            var label = child.AddComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        private static void SetRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void StretchFill(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
