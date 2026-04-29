using UnityEngine;

namespace PlayerBlock
{
    public static class EndlessUpgradeState
    {
        public static float ShadowDamageMultiplier { get; private set; } = 1f;
        public static float ShadowAttackCooldownMultiplier { get; private set; } = 1f;
        public static float ShadowMoveSpeedMultiplier { get; private set; } = 1f;
        public static float ShadowLifetimeBonus { get; private set; }

        public static void Reset()
        {
            ShadowDamageMultiplier = 1f;
            ShadowAttackCooldownMultiplier = 1f;
            ShadowMoveSpeedMultiplier = 1f;
            ShadowLifetimeBonus = 0f;
        }

        public static void AddShadowDamage(float multiplierBonus)
        {
            ShadowDamageMultiplier += Mathf.Max(0f, multiplierBonus);
        }

        public static void AddShadowAttackSpeed(float multiplierBonus)
        {
            var speedMultiplier = 1f + Mathf.Max(0f, multiplierBonus);
            ShadowAttackCooldownMultiplier = Mathf.Clamp(ShadowAttackCooldownMultiplier / speedMultiplier, 0.35f, 1f);
        }

        public static void AddShadowMoveSpeed(float multiplierBonus)
        {
            ShadowMoveSpeedMultiplier += Mathf.Max(0f, multiplierBonus);
        }

        public static void AddShadowLifetime(float seconds)
        {
            ShadowLifetimeBonus += Mathf.Max(0f, seconds);
        }
    }
}
