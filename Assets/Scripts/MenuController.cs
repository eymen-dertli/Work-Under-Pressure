using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    private const string HtpButtonName = "HtpButton";
    private const string HowToPlayPanelName = "HowToPlayPanel";
    private const string SettingsButtonName = "SettingsButton";
    private const string SettingsPanelName = "SettingsPanel";

    public void PlayGame()
    {
        LevelProgression.TryLoadLevel(1);
    }

    public GameObject mainMenuPanel;
    public GameObject levelsPanel;
    public GameObject howToPlayPanel;
    public GameObject settingsPanel;

    private void Awake()
    {
        if (howToPlayPanel == null)
        {
            howToPlayPanel = GameObject.Find(HowToPlayPanelName);
        }

        if (settingsPanel == null)
        {
            settingsPanel = GameObject.Find(SettingsPanelName);
        }

        if (howToPlayPanel != null)
        {
            howToPlayPanel.SetActive(false);
        }

        if (settingsPanel != null)
        {
            if (settingsPanel.GetComponent<SettingsPanelController>() == null)
            {
                settingsPanel.AddComponent<SettingsPanelController>();
            }

            settingsPanel.SetActive(false);
        }

        GameObject htpButtonObject = GameObject.Find(HtpButtonName);
        if (htpButtonObject != null && htpButtonObject.TryGetComponent(out Button htpButton))
        {
            if (htpButton.onClick.GetPersistentEventCount() == 0)
            {
                htpButton.onClick.RemoveListener(OpenHowToPlayPanel);
                htpButton.onClick.AddListener(OpenHowToPlayPanel);
            }
        }

        GameObject settingsButtonObject = GameObject.Find(SettingsButtonName);
        if (settingsButtonObject != null && settingsButtonObject.TryGetComponent(out Button settingsButton))
        {
            settingsButton.onClick.RemoveListener(OpenSettingsPanel);
            settingsButton.onClick.AddListener(OpenSettingsPanel);
        }
    }

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

    public void OpenHowToPlayPanel()
    {
        if (levelsPanel != null)
        {
            levelsPanel.SetActive(false);
        }

        if (howToPlayPanel != null)
        {
            howToPlayPanel.SetActive(true);
        }
    }

    public void CloseHowToPlayPanel()
    {
        if (howToPlayPanel != null)
        {
            howToPlayPanel.SetActive(false);
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }
    }

    public void OpenSettingsPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    public void CloseSettingsPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
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
