using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BreakManager : MonoBehaviour
{
    public enum BreakTimeBehavior
    {
        PauseGameTime,
        FastForwardGameTime,
        KeepGameTimeRunning,
    }

    [Header("Game Time")]
    [SerializeField] private GameTimeManager gameTimeManager;
    [SerializeField] private BreakTimeBehavior timeBehavior = BreakTimeBehavior.PauseGameTime;
    [SerializeField] private float breakTimeMultiplier = 6f;

    [Header("UI")]
    [SerializeField] private Button breakButton;
    [SerializeField] private TextMeshProUGUI breakButtonLabel;
    [SerializeField] private GameObject breakPanelRoot;
    [SerializeField] private Button returnButton;
    [SerializeField] private TextMeshProUGUI breakStatusLabel;

    private bool isOnBreak;
    private bool previousGameTimePaused;
    private float previousGameTimeMultiplier = 1f;

    private void Start()
    {
        EnsureUi();
        CloseBreakPanel();
    }

    public void StartBreak()
    {
        EnsureUi();
        if (isOnBreak)
        {
            return;
        }

        isOnBreak = true;
        TaskTimer.PauseAllActive();
        ApplyGameTimeBreakBehavior();
        breakPanelRoot.SetActive(true);

        if (breakStatusLabel != null)
        {
            breakStatusLabel.text = "Mola aktif. Görev saatleri duraklatıldı.";
        }
    }

    public void ReturnFromBreak()
    {
        if (!isOnBreak)
        {
            CloseBreakPanel();
            return;
        }

        RestoreGameTime();
        TaskTimer.ResumeAllActive();
        isOnBreak = false;
        CloseBreakPanel();
    }

    private void ApplyGameTimeBreakBehavior()
    {
        GameTimeManager manager = ResolveGameTimeManager();
        if (manager == null)
        {
            return;
        }

        previousGameTimePaused = manager.IsPaused;
        previousGameTimeMultiplier = manager.TimeMultiplier;

        if (timeBehavior == BreakTimeBehavior.PauseGameTime)
        {
            manager.SetPaused(true);
        }
        else if (timeBehavior == BreakTimeBehavior.FastForwardGameTime)
        {
            manager.SetPaused(false);
            manager.SetTimeMultiplier(breakTimeMultiplier);
        }
    }

    private void RestoreGameTime()
    {
        GameTimeManager manager = ResolveGameTimeManager();
        if (manager == null)
        {
            return;
        }

        manager.SetTimeMultiplier(previousGameTimeMultiplier);
        manager.SetPaused(previousGameTimePaused);
    }

    private GameTimeManager ResolveGameTimeManager()
    {
        if (gameTimeManager != null)
        {
            return gameTimeManager;
        }

        gameTimeManager = GameTimeManager.Instance != null ? GameTimeManager.Instance : FindAnyObjectByType<GameTimeManager>();
        return gameTimeManager;
    }

    private void CloseBreakPanel()
    {
        if (breakPanelRoot != null)
        {
            breakPanelRoot.SetActive(false);
        }
    }

    private void EnsureUi()
    {
        if (breakButton != null)
        {
            breakButton.onClick.RemoveListener(StartBreak);
            breakButton.onClick.AddListener(StartBreak);
            if (breakButtonLabel == null)
            {
                breakButtonLabel = breakButton.GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        if (breakPanelRoot != null && returnButton != null)
        {
            returnButton.onClick.RemoveListener(ReturnFromBreak);
            returnButton.onClick.AddListener(ReturnFromBreak);
            return;
        }

        Canvas canvas = breakButton != null ? breakButton.GetComponentInParent<Canvas>() : null;
        if (canvas == null)
        {
            canvas = OfficeMiniGameUi.CreateOverlayCanvas("Break Canvas", transform, 1030);
        }

        if (breakButton == null)
        {
            breakButton = OfficeMiniGameUi.CreateButton("Break Button", canvas.transform, "Molaya Çık", new Vector2(190f, 54f), new Color(0.18f, 0.4f, 0.42f, 1f), StartBreak);
            RectTransform breakButtonRect = breakButton.GetComponent<RectTransform>();
            breakButtonRect.anchorMin = new Vector2(1f, 0f);
            breakButtonRect.anchorMax = new Vector2(1f, 0f);
            breakButtonRect.anchoredPosition = new Vector2(-125f, 64f);
            breakButtonLabel = breakButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (breakPanelRoot == null)
        {
            breakPanelRoot = OfficeMiniGameUi.CreateImage("Break Panel", canvas.transform, new Color(0f, 0f, 0f, 0.62f));
            OfficeMiniGameUi.Stretch(breakPanelRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        }

        GameObject card = OfficeMiniGameUi.CreateImage("Break Card", breakPanelRoot.transform, new Color(0.94f, 0.91f, 0.82f, 0.98f));
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(460f, 270f);
        cardRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI title = OfficeMiniGameUi.CreateLabel("BreakTitle", card.transform, "MOLA", 34f, new Color(0.12f, 0.1f, 0.08f, 1f));
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(24f, -74f);
        titleRect.offsetMax = new Vector2(-24f, -20f);

        breakStatusLabel = OfficeMiniGameUi.CreateLabel("BreakStatus", card.transform, "Mola aktif.", 21f, new Color(0.14f, 0.12f, 0.1f, 1f));
        RectTransform statusRect = breakStatusLabel.GetComponent<RectTransform>();
        statusRect.anchorMin = Vector2.zero;
        statusRect.anchorMax = Vector2.one;
        statusRect.offsetMin = new Vector2(34f, 82f);
        statusRect.offsetMax = new Vector2(-34f, -96f);

        if (returnButton == null)
        {
            returnButton = OfficeMiniGameUi.CreateButton("Return From Break", card.transform, "Moladan Dön", new Vector2(180f, 52f), new Color(0.25f, 0.48f, 0.34f, 1f), ReturnFromBreak);
            RectTransform returnRect = returnButton.GetComponent<RectTransform>();
            returnRect.anchorMin = new Vector2(0.5f, 0f);
            returnRect.anchorMax = new Vector2(0.5f, 0f);
            returnRect.anchoredPosition = new Vector2(0f, 48f);
        }

        breakButton.onClick.RemoveListener(StartBreak);
        breakButton.onClick.AddListener(StartBreak);
        returnButton.onClick.RemoveListener(ReturnFromBreak);
        returnButton.onClick.AddListener(ReturnFromBreak);
    }
}
