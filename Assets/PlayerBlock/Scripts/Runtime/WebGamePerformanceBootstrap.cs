using UnityEngine;

namespace PlayerBlock
{
    public static class WebGamePerformanceBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyBrowserFriendlyDefaults()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.shadowDistance = 0f;
            QualitySettings.shadowCascades = 0;
            QualitySettings.antiAliasing = 0;
            QualitySettings.realtimeReflectionProbes = false;
            QualitySettings.softParticles = false;
            QualitySettings.particleRaycastBudget = 8;
            QualitySettings.lodBias = Mathf.Min(QualitySettings.lodBias, 0.75f);
            QualitySettings.maximumLODLevel = Mathf.Max(QualitySettings.maximumLODLevel, 1);

            Physics.defaultSolverIterations = Mathf.Min(Physics.defaultSolverIterations, 6);
            Physics.defaultSolverVelocityIterations = Mathf.Min(Physics.defaultSolverVelocityIterations, 2);
        }
    }
}
