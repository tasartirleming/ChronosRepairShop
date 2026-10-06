using UnityEngine;

namespace ChronosRepairShop
{
    public static class SaveSystem
    {
        static string Key(LevelData level) => "chronos.level." + level.id + ".stars";

        public static int GetStars(LevelData level) => PlayerPrefs.GetInt(Key(level), 0);
        public static bool IsCompleted(LevelData level) => GetStars(level) > 0;

        public static void SaveResult(LevelData level, int stars)
        {
            if (stars <= GetStars(level)) return;
            PlayerPrefs.SetInt(Key(level), stars);
            PlayerPrefs.Save();
        }
    }
}
