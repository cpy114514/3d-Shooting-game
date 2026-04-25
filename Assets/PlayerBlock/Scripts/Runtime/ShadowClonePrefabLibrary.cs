using UnityEngine;

namespace PlayerBlock
{
    internal static class ShadowClonePrefabLibrary
    {
        private const string ResourceRoot = "PlayerBlock/ShadowClones/";

        private static GameObject _meleePrefab;
        private static GameObject _rangedPrefab;
        private static GameObject _shieldPrefab;

        public static GameObject GetPrefab(ShadowCloneKind kind)
        {
            return kind switch
            {
                ShadowCloneKind.Melee => _meleePrefab ??= LoadPrefab("ShadowMelee"),
                ShadowCloneKind.Ranged => _rangedPrefab ??= LoadPrefab("ShadowRanged"),
                ShadowCloneKind.Shield => _shieldPrefab ??= LoadPrefab("ShadowShield"),
                _ => _meleePrefab ??= LoadPrefab("ShadowMelee")
            };
        }

        private static GameObject LoadPrefab(string prefabName)
        {
            return Resources.Load<GameObject>(ResourceRoot + prefabName);
        }
    }
}
