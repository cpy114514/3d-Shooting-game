using UnityEngine;

namespace LanShooter
{
    public sealed class LanShooterBootstrap : MonoBehaviour
    {
        private static bool s_Initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateBootstrap()
        {
            if (s_Initialized
                || FindFirstObjectByType<LanShooterBootstrap>() != null
                || (FindFirstObjectByType<LanShooterSession>() != null && FindFirstObjectByType<LanShooterHud>() != null))
            {
                return;
            }

            var bootstrap = new GameObject(nameof(LanShooterBootstrap));
            DontDestroyOnLoad(bootstrap);
            bootstrap.AddComponent<LanShooterBootstrap>();
            s_Initialized = true;
        }

        private void Awake()
        {
            var bootstraps = FindObjectsByType<LanShooterBootstrap>(FindObjectsSortMode.None);
            if (bootstraps.Length > 1)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            LanShooterRuntimeSceneSetup.EnsureReady();
            if (FindFirstObjectByType<LanShooterSession>() == null)
            {
                gameObject.AddComponent<LanShooterSession>();
            }

            if (FindFirstObjectByType<LanShooterHud>() == null)
            {
                gameObject.AddComponent<LanShooterHud>();
            }
        }
    }
}
