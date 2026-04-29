using UnityEngine;

namespace PlayerBlock
{
    public sealed class GameAudioManager : MonoBehaviour
    {
        private static GameAudioManager _instance;

        [SerializeField] private AudioClip backgroundMusic;

        private AudioSource _musicSource;
        private AudioSource _sfxSource;
        private AudioClip _playerPunchClip;
        private AudioClip _playerShotClip;
        private AudioClip _playerHitClip;
        private AudioClip _enemyAttackClip;
        private AudioClip _enemyHitClip;
        private AudioClip _enemyShotClip;
        private AudioClip _bossAttackClip;
        private AudioClip _bossHitClip;
        private AudioClip _shieldBlockClip;
        private AudioClip _dodgeDashClip;
        private AudioClip _uiConfirmClip;
        private AudioClip _enemyDeathClip;
        private AudioClip _victoryStingClip;
        private AudioClip _gameOverClip;
        private AudioClip _deathClip;
        private bool _initialized;

        public static GameAudioManager Instance
        {
            get
            {
                EnsureInstance();
                return _instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            EnsureInstance();
        }

        public static void PlayPlayerPunch()
        {
            var manager = Instance;
            if (manager != null)
            {
                manager.PlayOneShot(manager.GetPlayerPunchClip(), 1f);
            }
        }

        public static void PlayPlayerShot()
        {
            var manager = Instance;
            if (manager != null)
            {
                manager.PlayOneShot(manager.GetPlayerShotClip(), 1f);
            }
        }

        public static void PlayPlayerHit()
        {
            var manager = Instance;
            if (manager != null)
            {
                manager.PlayOneShot(manager.GetPlayerHitClip(), 1f);
            }
        }

        public static void PlayEnemyAttack()
        {
            var manager = Instance;
            if (manager != null)
            {
                manager.PlayOneShot(manager.GetEnemyAttackClip(), 1f);
            }
        }

        public static void PlayEnemyShot()
        {
            var manager = Instance;
            if (manager != null)
            {
                manager.PlayOneShot(manager.GetEnemyShotClip(), 1f);
            }
        }

        public static void PlayEnemyHit()
        {
            var manager = Instance;
            if (manager != null)
            {
                manager.PlayOneShot(manager.GetEnemyHitClip(), 1f);
            }
        }

        public static void PlayBossAttack()
        {
            var manager = Instance;
            if (manager != null)
            {
                manager.PlayOneShot(manager.GetBossAttackClip(), 1f);
            }
        }

        public static void PlayBossHit()
        {
            var manager = Instance;
            if (manager != null)
            {
                manager.PlayOneShot(manager.GetBossHitClip(), 1f);
            }
        }

        public static void PlayShieldBlock()
        {
            var manager = Instance;
            if (manager != null)
            {
                manager.PlayOneShot(manager.GetShieldBlockClip(), 1f);
            }
        }

        public static void PlayDash()
        {
            var manager = Instance;
            if (manager != null)
            {
                manager.PlayOneShot(manager.GetDashClip(), 1f);
            }
        }

        public static void PlayUiConfirm()
        {
            var manager = Instance;
            if (manager != null)
            {
                manager.PlayOneShot(manager.GetUiConfirmClip(), 1f);
            }
        }

        public static void PlayEnemyDeath()
        {
            var manager = Instance;
            if (manager != null)
            {
                manager.PlayOneShot(manager.GetEnemyDeathClip(), 1f);
            }
        }

        public static void PlayVictory()
        {
            var manager = Instance;
            if (manager != null)
            {
                manager.PlayOneShot(manager.GetVictoryStingClip(), 1f);
            }
        }

        public static void PlayDeath()
        {
            var manager = Instance;
            if (manager != null)
            {
                manager.PlayOneShot(manager.GetGameOverClip(), 1f);
            }
        }

        private static void EnsureInstance()
        {
            if (_instance != null)
            {
                return;
            }

            _instance = Object.FindFirstObjectByType<GameAudioManager>();
            if (_instance != null)
            {
                _instance.InitializeIfNeeded();
                return;
            }

            var gameObject = new GameObject("GameAudioManager");
            _instance = gameObject.AddComponent<GameAudioManager>();
            _instance.InitializeIfNeeded();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeIfNeeded();
        }

        private void OnEnable()
        {
            BrowserGameSettings.Changed += ApplyVolumes;
        }

        private void OnDisable()
        {
            BrowserGameSettings.Changed -= ApplyVolumes;
        }

        private void InitializeIfNeeded()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            BuildSources();
            LoadMusicClip();
            LoadSfxClips();
            ApplyVolumes();
            StartMusicIfReady();
        }

        private void BuildSources()
        {
            if (_musicSource == null)
            {
                var musicObject = new GameObject("MusicSource");
                musicObject.transform.SetParent(transform, false);
                _musicSource = musicObject.AddComponent<AudioSource>();
                _musicSource.playOnAwake = false;
                _musicSource.loop = true;
                _musicSource.spatialBlend = 0f;
            }

            if (_sfxSource == null)
            {
                var sfxObject = new GameObject("SfxSource");
                sfxObject.transform.SetParent(transform, false);
                _sfxSource = sfxObject.AddComponent<AudioSource>();
                _sfxSource.playOnAwake = false;
                _sfxSource.loop = false;
                _sfxSource.spatialBlend = 0f;
            }
        }

        private void LoadMusicClip()
        {
            if (backgroundMusic == null)
            {
                backgroundMusic = Resources.Load<AudioClip>("Audio/Shadow Arena Pulse");
                if (backgroundMusic == null)
                {
                    backgroundMusic = Resources.Load<AudioClip>("Audio/backgroundmusic");
                }
            }

            if (_musicSource != null && backgroundMusic != null)
            {
                _musicSource.clip = backgroundMusic;
            }
        }

        private void LoadSfxClips()
        {
            _playerPunchClip = LoadClipOrCreate(_playerPunchClip, "Audio/sfx_attack_swing", () => CreateHybridBurstClip("PlayerPunch", 0.18f, 110f, 62f, 0.085f, 0.7f, 8f, 0.34f));
            _playerShotClip = LoadClipOrCreate(_playerShotClip, "Audio/sfx_attack_swing", () => CreateNoiseBurstClip("PlayerShot", 0.11f, 0.18f, 0.72f));
            _playerHitClip = LoadClipOrCreate(_playerHitClip, "Audio/sfx_hurt", () => CreateHybridBurstClip("PlayerHit", 0.2f, 170f, 86f, 0.08f, 0.62f, 10f, 0.32f));
            _enemyAttackClip = LoadClipOrCreate(_enemyAttackClip, "Audio/sfx_attack_swing", () => CreateHybridBurstClip("EnemyAttack", 0.17f, 150f, 76f, 0.08f, 0.6f, 8f, 0.38f));
            _enemyHitClip = LoadClipOrCreate(_enemyHitClip, "Audio/sfx_hit_impact", () => CreateHybridBurstClip("EnemyHit", 0.18f, 135f, 70f, 0.09f, 0.55f, 9f, 0.3f));
            _enemyShotClip = LoadClipOrCreate(_enemyShotClip, "Audio/sfx_attack_swing", () => CreateNoiseBurstClip("EnemyShot", 0.12f, 0.18f, 0.8f));
            _bossAttackClip = LoadClipOrCreate(_bossAttackClip, "Audio/sfx_heavy_swing", () => CreateHybridBurstClip("BossAttack", 0.24f, 72f, 38f, 0.1f, 0.7f, 6f, 0.34f));
            _bossHitClip = LoadClipOrCreate(_bossHitClip, "Audio/sfx_metal_hit", () => CreateHybridBurstClip("BossHit", 0.26f, 96f, 54f, 0.09f, 0.72f, 7f, 0.28f));
            _shieldBlockClip = LoadClipOrCreate(_shieldBlockClip, "Audio/sfx_metal_hit", () => CreateHybridBurstClip("ShieldBlock", 0.15f, 260f, 180f, 0.14f, 0.52f, 13f, 0.4f));
            _dodgeDashClip = LoadClipOrCreate(_dodgeDashClip, "Audio/sfx_dodge_dash", () => CreateNoiseBurstClip("Dash", 0.14f, 0.16f, 0.78f));
            _uiConfirmClip = LoadClipOrCreate(_uiConfirmClip, "Audio/sfx_ui_confirm", () => CreateNoiseBurstClip("UiConfirm", 0.09f, 0.1f, 0.7f));
            _enemyDeathClip = LoadClipOrCreate(_enemyDeathClip, "Audio/sfx_enemy_death", () => CreateNoiseBurstClip("EnemyDeath", 0.26f, 0.2f, 0.82f));
            _victoryStingClip = LoadClipOrCreate(_victoryStingClip, "Audio/sfx_victory_sting", () => CreateHybridBurstClip("VictorySting", 0.45f, 180f, 88f, 0.04f, 0.7f, 5f, 0.22f));
            _gameOverClip = LoadClipOrCreate(_gameOverClip, "Audio/sfx_game_over", () => CreateNoiseBurstClip("GameOver", 0.34f, 0.22f, 0.86f));
        }

        private void ApplyVolumes()
        {
            if (_musicSource != null)
            {
                _musicSource.volume = Mathf.Clamp01(BrowserGameSettings.MusicVolume);
            }

            if (_sfxSource != null)
            {
                _sfxSource.volume = Mathf.Clamp01(BrowserGameSettings.SfxVolume);
            }

            StartMusicIfReady();
        }

        private void StartMusicIfReady()
        {
            if (_musicSource == null || backgroundMusic == null)
            {
                return;
            }

            if (_musicSource.clip != backgroundMusic)
            {
                _musicSource.clip = backgroundMusic;
            }

            if (!_musicSource.isPlaying)
            {
                _musicSource.Play();
            }
        }

        private void PlayOneShot(AudioClip clip, float volumeScale)
        {
            if (_sfxSource == null || clip == null)
            {
                return;
            }

            var originalPitch = _sfxSource.pitch;
            _sfxSource.pitch = Random.Range(0.95f, 1.05f);
            _sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
            _sfxSource.pitch = originalPitch;
        }

        private static AudioClip LoadClipOrCreate(AudioClip existingClip, string resourcePath, System.Func<AudioClip> fallbackFactory)
        {
            if (existingClip != null)
            {
                return existingClip;
            }

            var loaded = Resources.Load<AudioClip>(resourcePath);
            return loaded != null ? loaded : fallbackFactory();
        }

        private AudioClip GetPlayerPunchClip()
        {
            return _playerPunchClip;
        }

        private AudioClip GetPlayerShotClip()
        {
            return _playerShotClip;
        }

        private AudioClip GetPlayerHitClip()
        {
            return _playerHitClip;
        }

        private AudioClip GetEnemyAttackClip()
        {
            return _enemyAttackClip;
        }

        private AudioClip GetEnemyHitClip()
        {
            return _enemyHitClip;
        }

        private AudioClip GetEnemyShotClip()
        {
            return _enemyShotClip;
        }

        private AudioClip GetBossAttackClip()
        {
            return _bossAttackClip;
        }

        private AudioClip GetBossHitClip()
        {
            return _bossHitClip;
        }

        private AudioClip GetShieldBlockClip()
        {
            return _shieldBlockClip;
        }

        private AudioClip GetDashClip()
        {
            return _dodgeDashClip;
        }

        private AudioClip GetUiConfirmClip()
        {
            return _uiConfirmClip;
        }

        private AudioClip GetEnemyDeathClip()
        {
            return _enemyDeathClip;
        }

        private AudioClip GetVictoryStingClip()
        {
            return _victoryStingClip;
        }

        private AudioClip GetGameOverClip()
        {
            return _gameOverClip;
        }

        private static AudioClip CreateNoiseBurstClip(string name, float duration, float noiseAmount, float decay)
        {
            return CreateClip(name, duration, sampleIndex =>
            {
                var sampleRate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000;
                var time = sampleIndex / (float)sampleRate;
                var envelope = Mathf.Exp(-time * decay) * Mathf.Clamp01(1f - (time / Mathf.Max(0.0001f, duration)));
                var seed = sampleIndex * 1103515245 + 12345;
                var noise = (((seed >> 16) & 0x7fff) / 16384f) - 1f;
                return noise * noiseAmount * envelope;
            });
        }

        private static AudioClip CreateHybridBurstClip(string name, float duration, float startFrequency, float endFrequency, float noiseAmount, float toneAmount, float decay, float wobbleAmount)
        {
            return CreateClip(name, duration, sampleIndex =>
            {
                var sampleRate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000;
                var time = sampleIndex / (float)sampleRate;
                var progress = Mathf.Clamp01(time / Mathf.Max(0.0001f, duration));
                var envelope = Mathf.Exp(-time * decay) * Mathf.Clamp01(1f - progress);
                var frequency = Mathf.Lerp(startFrequency, endFrequency, progress);
                var phase = 2f * Mathf.PI * frequency * time;
                var tone = Mathf.Sin(phase + Mathf.Sin(time * 20f) * wobbleAmount) * toneAmount;
                var seed = sampleIndex * 1103515245 + 12345;
                var noise = (((seed >> 16) & 0x7fff) / 16384f) - 1f;
                return Mathf.Clamp((tone + noise * noiseAmount) * envelope, -1f, 1f);
            });
        }

        private static AudioClip CreateClip(string name, float duration, System.Func<int, float> sampleGenerator)
        {
            var sampleRate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000;
            var samples = Mathf.Max(1, Mathf.CeilToInt(duration * sampleRate));
            var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            var data = new float[samples];
            for (var i = 0; i < samples; i++)
            {
                data[i] = Mathf.Clamp(sampleGenerator(i), -1f, 1f);
            }

            clip.SetData(data, 0);
            return clip;
        }
    }
}
