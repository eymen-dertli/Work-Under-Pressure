using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelProgression
{
    private const string UnlockedKeyPrefix = "WorkUnderPressure.LevelUnlocked.";
    private const string CompletedKeyPrefix = "WorkUnderPressure.LevelCompleted.";

    public static bool IsLevelUnlocked(LevelDefinition level)
    {
        return level != null && (level.unlockedByDefault || PlayerPrefs.GetInt(UnlockedKeyPrefix + level.levelNumber, 0) == 1);
    }

    public static bool IsLevelCompleted(int levelNumber)
    {
        return PlayerPrefs.GetInt(CompletedKeyPrefix + levelNumber, 0) == 1;
    }

    public static void UnlockLevel(int levelNumber)
    {
        PlayerPrefs.SetInt(UnlockedKeyPrefix + levelNumber, 1);
        PlayerPrefs.Save();
    }

    public static void CompleteLevel(int levelNumber)
    {
        LevelDatabase database = LevelDatabase.Load();
        LevelDefinition currentLevel = database.GetLevel(levelNumber);
        if (currentLevel == null)
        {
            Debug.LogWarning($"Level {levelNumber} could not be completed because it is not in level data.");
            return;
        }

        PlayerPrefs.SetInt(CompletedKeyPrefix + levelNumber, 1);

        LevelDefinition nextLevel = database.GetNextLevel(levelNumber);
        if (nextLevel != null)
        {
            PlayerPrefs.SetInt(UnlockedKeyPrefix + nextLevel.levelNumber, 1);
        }

        PlayerPrefs.Save();
    }

    public static void ResetProgress()
    {
        LevelDatabase database = LevelDatabase.Load();
        if (database.levels != null)
        {
            for (int i = 0; i < database.levels.Length; i++)
            {
                PlayerPrefs.DeleteKey(UnlockedKeyPrefix + database.levels[i].levelNumber);
                PlayerPrefs.DeleteKey(CompletedKeyPrefix + database.levels[i].levelNumber);
            }
        }

        PlayerPrefs.Save();
    }

    public static bool TryLoadLevel(int levelNumber)
    {
        LevelDatabase database = LevelDatabase.Load();
        LevelDefinition level = database.GetLevel(levelNumber);
        if (level == null)
        {
            Debug.LogWarning($"Level {levelNumber} is not defined.");
            return false;
        }

        if (!IsLevelUnlocked(level))
        {
            Debug.Log($"Level {levelNumber} is locked.");
            return false;
        }

        SceneManager.LoadScene(level.sceneName);
        return true;
    }
}
