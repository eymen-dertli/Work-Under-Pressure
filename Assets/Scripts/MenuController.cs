using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene("Game1Scene");
    }

    public GameObject mainMenuPanel;
    public GameObject levelsPanel;

    public void OpenLevelsPanel()
    {
        levelsPanel.SetActive(true);
        mainMenuPanel.SetActive(false);
    }
}