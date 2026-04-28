using UnityEngine;

namespace PlayerBlock
{
    public static class LevelProgress
    {
        private const string UnlockedLevelKey = "PlayerBlock.LevelProgress.UnlockedLevel";
        private const int FirstLevel = 1;

        public static int HighestUnlockedLevel
        {
            get => Mathf.Max(FirstLevel, PlayerPrefs.GetInt(UnlockedLevelKey, FirstLevel));
            private set
            {
                PlayerPrefs.SetInt(UnlockedLevelKey, Mathf.Max(FirstLevel, value));
                PlayerPrefs.Save();
            }
        }

        public static bool IsUnlocked(int levelNumber)
        {
            return levelNumber <= HighestUnlockedLevel;
        }

        public static void UnlockLevel(int levelNumber)
        {
            if (levelNumber > HighestUnlockedLevel)
            {
                HighestUnlockedLevel = levelNumber;
            }
        }

        public static void UnlockAll(int maxLevel)
        {
            HighestUnlockedLevel = maxLevel;
        }
    }
}
