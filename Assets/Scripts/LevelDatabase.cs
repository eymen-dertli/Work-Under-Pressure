using System;
using UnityEngine;

[Serializable]
public sealed class LevelDatabase
{
    private const string ResourcePath = "LevelData/levels";

    public LevelDefinition[] levels;

    public static LevelDatabase Load()
    {
        TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
        if (asset == null)
        {
            Debug.LogError($"Level data could not be loaded from Resources/{ResourcePath}.json");
            return new LevelDatabase { levels = Array.Empty<LevelDefinition>() };
        }

        LevelDatabase database = JsonUtility.FromJson<LevelDatabase>(asset.text);
        return database ?? new LevelDatabase { levels = Array.Empty<LevelDefinition>() };
    }

    public LevelDefinition GetLevel(int levelNumber)
    {
        if (levels == null)
        {
            return null;
        }

        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i].levelNumber == levelNumber)
            {
                return levels[i];
            }
        }

        return null;
    }

    public LevelDefinition GetNextLevel(int levelNumber)
    {
        return GetLevel(levelNumber + 1);
    }

    public LevelDefinition GetLevelBySceneName(string sceneName)
    {
        if (levels == null)
        {
            return null;
        }

        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i].sceneName == sceneName)
            {
                return levels[i];
            }
        }

        return null;
    }
}
