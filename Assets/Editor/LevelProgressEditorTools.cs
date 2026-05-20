using UnityEditor;
using UnityEngine;

public static class LevelProgressEditorTools
{
    [MenuItem("Tools/Work Under Pressure/Reset Level Progress")]
    public static void ResetLevelProgress()
    {
        LevelProgression.ResetProgress();
        Debug.Log("Level progress has been reset from the Unity Editor menu.");
    }

    [MenuItem("Tools/Work Under Pressure/Unlock All Levels")]
    public static void UnlockAllLevels()
    {
        LevelDatabase database = LevelDatabase.Load();
        if (database.levels == null)
        {
            return;
        }

        for (int i = 0; i < database.levels.Length; i++)
        {
            LevelProgression.UnlockLevel(database.levels[i].levelNumber);
        }

        Debug.Log("All levels have been unlocked from the Unity Editor menu.");
    }
}
