using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public void PlayGame()
    {
        LevelProgression.TryLoadLevel(1);
    }

    public GameObject mainMenuPanel;
    public GameObject levelsPanel;

    public void OpenLevelsPanel()
    {
        levelsPanel.SetActive(true);
        mainMenuPanel.SetActive(false);

        LevelSelectController controller = levelsPanel.GetComponent<LevelSelectController>();
        if (controller == null)
        {
            controller = levelsPanel.AddComponent<LevelSelectController>();
        }

        controller.Refresh();
    }

    public void ResetLevelProgress()
    {
        LevelProgression.ResetProgress();

        if (levelsPanel != null && levelsPanel.TryGetComponent(out LevelSelectController controller))
        {
            controller.Refresh();
        }
    }
}
