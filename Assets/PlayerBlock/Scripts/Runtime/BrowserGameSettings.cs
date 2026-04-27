using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace PlayerBlock
{
    public static class BrowserGameSettings
    {
        public enum GraphicsQualityLevel
        {
            Low = 0,
            Medium = 1,
            High = 2,
        }

        [Serializable]
        private sealed class SettingsData
        {
            public int graphicsQuality = (int)GraphicsQualityLevel.High;
            public bool shadows = true;
            public bool showFps = false;
            public float mouseSensitivity = 0.14f;
            public bool invertYAxis = false;
            public float masterVolume = 1f;
            public float musicVolume = 0.85f;
            public float sfxVolume = 1f;
            public float cameraDistance = 1f;
            public bool screenShake = true;
        }

        private const string StorageKey = "PlayerBlock.Settings.Json";

        private static bool _loaded;
        private static SettingsData _data;

        public static event Action Changed;

        public static GraphicsQualityLevel GraphicsQuality
        {
            get
            {
                EnsureLoaded();
                return (GraphicsQualityLevel)_data.graphicsQuality;
            }
        }

        public static bool ShadowsEnabled
        {
            get
            {
                EnsureLoaded();
                return _data.shadows;
            }
        }

        public static bool ShowFps
        {
            get
            {
                EnsureLoaded();
                return _data.showFps;
            }
        }

        public static float MouseSensitivity
        {
            get
            {
                EnsureLoaded();
                return _data.mouseSensitivity;
            }
        }

        public static bool InvertYAxis
        {
            get
            {
                EnsureLoaded();
                return _data.invertYAxis;
            }
        }

        public static float MasterVolume
        {
            get
            {
                EnsureLoaded();
                return _data.masterVolume;
            }
        }

        public static float MusicVolume
        {
            get
            {
                EnsureLoaded();
                return _data.musicVolume;
            }
        }

        public static float SfxVolume
        {
            get
            {
                EnsureLoaded();
                return _data.sfxVolume;
            }
        }

        public static float CameraDistance
        {
            get
            {
                EnsureLoaded();
                return _data.cameraDistance;
            }
        }

        public static bool ScreenShakeEnabled
        {
            get
            {
                EnsureLoaded();
                return _data.screenShake;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            Load();
            Apply();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ReapplyAfterSceneLoad()
        {
            EnsureLoaded();
            Apply();
        }

        public static void Load()
        {
            _data = CreateDefaults();

            var storedJson = LoadStoredJson();
            if (!string.IsNullOrEmpty(storedJson))
            {
                try
                {
                    JsonUtility.FromJsonOverwrite(storedJson, _data);
                }
                catch (Exception)
                {
                    _data = CreateDefaults();
                }
            }

            ClampData();
            _loaded = true;
        }

        public static void Apply()
        {
            EnsureLoaded();

            AudioListener.volume = _data.masterVolume;
            ApplyGraphicsQuality();
            ApplyToActivePlayers();
        }

        public static void ResetDefaults()
        {
            _data = CreateDefaults();
            ClampData();
            Apply();
            Save();
            Changed?.Invoke();
        }

        public static void SetGraphicsQuality(GraphicsQualityLevel value)
        {
            Mutate(data => data.graphicsQuality = (int)value);
        }

        public static void CycleGraphicsQuality(int direction)
        {
            var next = WrapIndex((int)GraphicsQuality + (direction >= 0 ? 1 : -1), 3);
            SetGraphicsQuality((GraphicsQualityLevel)next);
        }

        public static void SetShadowsEnabled(bool value)
        {
            Mutate(data => data.shadows = value);
        }

        public static void ToggleShadows()
        {
            SetShadowsEnabled(!ShadowsEnabled);
        }

        public static void SetShowFps(bool value)
        {
            Mutate(data => data.showFps = value);
        }

        public static void ToggleShowFps()
        {
            SetShowFps(!ShowFps);
        }

        public static void SetMouseSensitivity(float value)
        {
            Mutate(data => data.mouseSensitivity = value);
        }

        public static void SetInvertYAxis(bool value)
        {
            Mutate(data => data.invertYAxis = value);
        }

        public static void ToggleInvertYAxis()
        {
            SetInvertYAxis(!InvertYAxis);
        }

        public static void SetMasterVolume(float value)
        {
            Mutate(data => data.masterVolume = value);
        }

        public static void SetMusicVolume(float value)
        {
            Mutate(data => data.musicVolume = value);
        }

        public static void SetSfxVolume(float value)
        {
            Mutate(data => data.sfxVolume = value);
        }

        public static void SetCameraDistance(float value)
        {
            Mutate(data => data.cameraDistance = value);
        }

        public static void SetScreenShakeEnabled(bool value)
        {
            Mutate(data => data.screenShake = value);
        }

        public static void ToggleScreenShake()
        {
            SetScreenShakeEnabled(!ScreenShakeEnabled);
        }

        public static float GetAdjustedDamageTakenByPlayer(float baseDamage)
        {
            return baseDamage;
        }

        public static float GetAdjustedDamageDealtToBoss(float baseDamage)
        {
            return baseDamage;
        }

        public static void Save()
        {
            EnsureLoaded();
            StoreJson(JsonUtility.ToJson(_data));
        }

        public static void ApplyToPlayer(BlockPlayerController player)
        {
            if (player == null)
            {
                return;
            }

            player.ApplyBrowserSettings(_data.mouseSensitivity, _data.invertYAxis, _data.cameraDistance, _data.screenShake);
        }

        private static void Mutate(Action<SettingsData> mutator)
        {
            EnsureLoaded();
            mutator?.Invoke(_data);
            ClampData();
            Apply();
            Save();
            Changed?.Invoke();
        }

        private static SettingsData CreateDefaults()
        {
            return new SettingsData();
        }

        private static void ClampData()
        {
            if (_data == null)
            {
                _data = CreateDefaults();
            }

            _data.graphicsQuality = Mathf.Clamp(_data.graphicsQuality, 0, 2);
            _data.mouseSensitivity = Mathf.Clamp(_data.mouseSensitivity, 0.03f, 0.3f);
            _data.masterVolume = Mathf.Clamp01(_data.masterVolume);
            _data.musicVolume = Mathf.Clamp01(_data.musicVolume);
            _data.sfxVolume = Mathf.Clamp01(_data.sfxVolume);
            _data.cameraDistance = Mathf.Clamp(_data.cameraDistance, 0.75f, 1.5f);
        }

        private static void ApplyGraphicsQuality()
        {
            var qualityNames = QualitySettings.names;
            if (qualityNames != null && qualityNames.Length > 0)
            {
                var mappedIndex = GraphicsQuality == GraphicsQualityLevel.Low
                    ? 0
                    : GraphicsQuality == GraphicsQualityLevel.Medium
                        ? qualityNames.Length / 2
                        : qualityNames.Length - 1;
                QualitySettings.SetQualityLevel(Mathf.Clamp(mappedIndex, 0, qualityNames.Length - 1), true);
            }

            switch (GraphicsQuality)
            {
                case GraphicsQualityLevel.Low:
                    QualitySettings.antiAliasing = 0;
                    QualitySettings.realtimeReflectionProbes = false;
                    QualitySettings.softParticles = false;
                    QualitySettings.lodBias = 0.7f;
                    break;
                case GraphicsQualityLevel.Medium:
                    QualitySettings.antiAliasing = 0;
                    QualitySettings.realtimeReflectionProbes = false;
                    QualitySettings.softParticles = false;
                    QualitySettings.lodBias = 1f;
                    break;
                default:
                    QualitySettings.antiAliasing = 2;
                    QualitySettings.realtimeReflectionProbes = true;
                    QualitySettings.softParticles = true;
                    QualitySettings.lodBias = 1.25f;
                    break;
            }

            if (_data.shadows)
            {
                QualitySettings.shadows = ShadowQuality.All;
                QualitySettings.shadowDistance = GraphicsQuality == GraphicsQualityLevel.Low ? 20f : GraphicsQuality == GraphicsQualityLevel.Medium ? 36f : 54f;
                QualitySettings.shadowCascades = GraphicsQuality == GraphicsQualityLevel.High ? 2 : 1;
            }
            else
            {
                QualitySettings.shadows = ShadowQuality.Disable;
                QualitySettings.shadowDistance = 0f;
                QualitySettings.shadowCascades = 0;
            }
        }

        private static void ApplyToActivePlayers()
        {
            var players = BlockPlayerController.ActiveInstances;
            for (var i = 0; i < players.Count; i++)
            {
                ApplyToPlayer(players[i]);
            }
        }

        private static int WrapIndex(int index, int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            index %= count;
            if (index < 0)
            {
                index += count;
            }

            return index;
        }

        private static void EnsureLoaded()
        {
            if (!_loaded)
            {
                Load();
            }
        }

        private static string LoadStoredJson()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var pointer = BrowserSettingsLoad(StorageKey);
            if (pointer == IntPtr.Zero)
            {
                return null;
            }

            var json = Marshal.PtrToStringAnsi(pointer);
            BrowserSettingsFree(pointer);
            return json;
#else
            return PlayerPrefs.GetString(StorageKey, string.Empty);
#endif
        }

        private static void StoreJson(string json)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            BrowserSettingsSave(StorageKey, json ?? string.Empty);
#else
            PlayerPrefs.SetString(StorageKey, json ?? string.Empty);
            PlayerPrefs.Save();
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void BrowserSettingsSave(string key, string value);

        [DllImport("__Internal")]
        private static extern IntPtr BrowserSettingsLoad(string key);

        [DllImport("__Internal")]
        private static extern void BrowserSettingsFree(IntPtr value);
#endif
    }
}
