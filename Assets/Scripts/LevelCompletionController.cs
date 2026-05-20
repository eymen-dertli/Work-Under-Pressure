using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class LevelCompletionController : MonoBehaviour
{
    [SerializeField] private int levelNumber = 0;
    [SerializeField] private string menuSceneName = "MainScene";

    public void CompleteLevel()
    {
        int resolvedLevelNumber = ResolveLevelNumber();
        if (resolvedLevelNumber <= 0)
        {
            Debug.LogWarning("Current scene is not linked to any level data.");
            return;
        }

        LevelProgression.CompleteLevel(resolvedLevelNumber);
    }

    public void CompleteLevelAndReturnToMenu()
    {
        CompleteLevel();
        SceneManager.LoadScene(menuSceneName);
    }

    [ContextMenu("Complete Level")]
    private void CompleteLevelFromContextMenu()
    {
        CompleteLevel();
    }

    private int ResolveLevelNumber()
    {
        if (levelNumber > 0)
        {
            return levelNumber;
        }

        LevelDefinition level = LevelDatabase.Load().GetLevelBySceneName(SceneManager.GetActiveScene().name);
        return level != null ? level.levelNumber : 0;
    }
}
