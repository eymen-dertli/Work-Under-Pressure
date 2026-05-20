using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LevelSelectController : MonoBehaviour
{
    private const string LevelsPanelName = "LevelsPanel";
    private const string ResetButtonName = "ResetProgressButton";

    [SerializeField] private Button[] levelButtons;
    [SerializeField] private Color lockedColor = new Color(0.35f, 0.35f, 0.35f, 0.75f);
    [SerializeField] private Color unlockedColor = Color.white;
    [SerializeField] private Color completedColor = new Color(0.68f, 1f, 0.68f, 1f);

    private LevelDatabase database;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallOnMainMenu()
    {
        GameObject levelsPanel = GameObject.Find(LevelsPanelName);
        if (levelsPanel == null || levelsPanel.GetComponent<LevelSelectController>() != null)
        {
            return;
        }

        levelsPanel.AddComponent<LevelSelectController>();
    }

    private void Awake()
    {
        database = LevelDatabase.Load();
        if (levelButtons == null || levelButtons.Length == 0)
        {
            levelButtons = FindLevelButtons();
        }

        CreateResetButtonIfMissing();
        Refresh();
    }

    public void Refresh()
    {
        if (database.levels == null || levelButtons == null)
        {
            return;
        }

        int count = Mathf.Min(database.levels.Length, levelButtons.Length);
        for (int i = 0; i < count; i++)
        {
            ConfigureButton(levelButtons[i], database.levels[i]);
        }
    }

    public void ResetProgress()
    {
        LevelProgression.ResetProgress();
        Refresh();
    }

    private Button[] FindLevelButtons()
    {
        return GetComponentsInChildren<Button>(true)
            .Where(button => button.gameObject.name != ResetButtonName)
            .OrderBy(button => ((RectTransform)button.transform).anchoredPosition.x)
            .ToArray();
    }

    private void ConfigureButton(Button button, LevelDefinition level)
    {
        bool isUnlocked = LevelProgression.IsLevelUnlocked(level);
        bool isCompleted = LevelProgression.IsLevelCompleted(level.levelNumber);

        button.interactable = isUnlocked;
        button.onClick.RemoveAllListeners();
        if (button.onClick.GetPersistentEventCount() == 0)
        {
            button.onClick.AddListener(() => LevelProgression.TryLoadLevel(level.levelNumber));
        }

        if (button.targetGraphic != null)
        {
            button.targetGraphic.color = isCompleted ? completedColor : isUnlocked ? unlockedColor : lockedColor;
        }

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.text = isUnlocked ? level.levelNumber.ToString() : "KILITLI";
            label.fontSize = isUnlocked ? 100f : 36f;
        }
    }

    private void CreateResetButtonIfMissing()
    {
        if (transform.Find(ResetButtonName) != null)
        {
            return;
        }

        GameObject resetButtonObject = new GameObject(ResetButtonName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        resetButtonObject.transform.SetParent(transform, false);

        RectTransform rect = resetButtonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -382f);
        rect.sizeDelta = new Vector2(320f, 72f);

        Image image = resetButtonObject.GetComponent<Image>();
        image.color = new Color(0.09f, 0.09f, 0.09f, 0.82f);

        Button button = resetButtonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(ResetProgress);

        GameObject textObject = new GameObject("Text (TMP)", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(resetButtonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = "ILERLEMEYI SIFIRLA";
        label.fontSize = 28f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
    }
}
