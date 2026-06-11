using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class GameTimeManager : MonoBehaviour
{
    public static GameTimeManager Instance { get; private set; }

    [Header("Trigger")]
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private bool listenToClickableDeskObjects = true;
    [SerializeField] private ClickableDeskObject wallClockObject;

    [Header("Time")]
    [SerializeField] private int startHour = 9;
    [SerializeField] private int startMinute;
    [SerializeField] private float gameMinutesPerRealSecond = 1f;
    [SerializeField] private bool isPaused;

    [Header("Clock UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI timeLabel;
    [SerializeField] private TextMeshProUGUI taskListLabel;
    [SerializeField] private Button closeButton;

    [Header("Analog Hands")]
    [SerializeField] private RectTransform hourHand;
    [SerializeField] private RectTransform minuteHand;
    [SerializeField] private Transform worldHourHand;
    [SerializeField] private Transform worldMinuteHand;
    [SerializeField] private float handZeroAngle = 0f;
    [SerializeField] private bool clockwise = true;

    private float currentGameMinutes;

    public event Action<int, int> MinuteChanged;

    public int CurrentHour => Mathf.FloorToInt(currentGameMinutes / 60f) % 24;
    public int CurrentMinute => Mathf.FloorToInt(currentGameMinutes) % 60;
    public float CurrentTotalMinutes => currentGameMinutes;
    public float TimeMultiplier => gameMinutesPerRealSecond;
    public bool IsPaused => isPaused;
    public string CurrentTimeText => $"{CurrentHour:00}:{CurrentMinute:00}";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void RegisterSceneLoadedHook()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallForInitialScene()
    {
        EnsureManagerForScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureManagerForScene(scene);
    }

    private static void EnsureManagerForScene(Scene scene)
    {
        if (!IsLevelScene(scene.name) || FindAnyObjectByType<GameTimeManager>() != null)
        {
            return;
        }

        GameObject managerObject = new GameObject("_GameTimeManager");
        managerObject.AddComponent<GameTimeManager>();
    }

    private static bool IsLevelScene(string sceneName)
    {
        LevelDefinition level = LevelDatabase.Load().GetLevelBySceneName(sceneName);
        return level != null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }

        currentGameMinutes = Mathf.Clamp(startHour, 0, 23) * 60f + Mathf.Clamp(startMinute, 0, 59);
        EnsureUi();
        ClosePanel();
        RefreshClock(true);
    }

    private void OnEnable()
    {
        if (listenToClickableDeskObjects)
        {
            ClickableDeskObject.Clicked += HandleDeskObjectClicked;
        }
    }

    private void OnDisable()
    {
        ClickableDeskObject.Clicked -= HandleDeskObjectClicked;
    }

    private void Update()
    {
        if (isPaused)
        {
            return;
        }

        int oldHour = CurrentHour;
        int oldMinute = CurrentMinute;
        currentGameMinutes = Mathf.Repeat(currentGameMinutes + gameMinutesPerRealSecond * Time.unscaledDeltaTime, 24f * 60f);
        RefreshClock(oldHour != CurrentHour || oldMinute != CurrentMinute);
    }

    public void OpenPanel()
    {
        EnsureUi();
        panelRoot.SetActive(true);
        RefreshClock(false);
    }

    public void ClosePanel()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    public void TogglePanel()
    {
        EnsureUi();
        panelRoot.SetActive(!panelRoot.activeSelf);
        RefreshClock(false);
    }

    public void SetPaused(bool paused)
    {
        isPaused = paused;
    }

    public void SetTimeMultiplier(float multiplier)
    {
        gameMinutesPerRealSecond = Mathf.Max(0f, multiplier);
    }

    public void SetTime(int hour, int minute)
    {
        currentGameMinutes = Mathf.Clamp(hour, 0, 23) * 60f + Mathf.Clamp(minute, 0, 59);
        RefreshClock(true);
    }

    private void HandleDeskObjectClicked(ClickableDeskObject clickedObject)
    {
        if (OfficeMiniGameUi.MatchesClickedObject(clickedObject, wallClockObject, "clock", "saat"))
        {
            TogglePanel();
        }
    }

    private void RefreshClock(bool notifyMinuteChanged)
    {
        if (timeLabel != null)
        {
            timeLabel.text = CurrentTimeText;
        }

        RefreshTaskList();
        RotateHands();

        if (notifyMinuteChanged)
        {
            MinuteChanged?.Invoke(CurrentHour, CurrentMinute);
        }
    }

    private void RefreshTaskList()
    {
        if (taskListLabel == null)
        {
            return;
        }

        if (!TaskAssignmentSession.HasAssignments && !TaskAssignmentSession.HasSelectedTasks)
        {
            taskListLabel.text = "Görevler henüz seçilmedi.";
            return;
        }

        StringBuilder builder = new StringBuilder();
        if (TaskAssignmentSession.HasAssignments)
        {
            IReadOnlyList<TaskAssignment> assignments = TaskAssignmentSession.CurrentAssignments;
            for (int i = 0; i < assignments.Count; i++)
            {
                TaskAssignment assignment = assignments[i];
                string stateText = $"Süre: {TaskAssignmentSession.FormatSeconds(assignment.TimeLimitSeconds)}";

                if (TaskAssignmentSession.TryGetProgress(assignment.Kind, out TaskAssignmentSession.TaskProgress progress))
                {
                    if (progress.IsCompleted)
                    {
                        stateText = "Tamamlandı";
                    }
                    else if (progress.IsFailed)
                    {
                        stateText = "Süre bitti";
                    }
                    else if (progress.IsRunning)
                    {
                        stateText = $"Kalan: {TaskAssignmentSession.FormatSeconds(progress.RemainingSeconds)}";
                    }
                    else if (progress.HasStarted)
                    {
                        stateText = $"Durdu: {TaskAssignmentSession.FormatSeconds(progress.RemainingSeconds)}";
                    }
                }

                builder.Append(assignment.DisplayName)
                    .Append("  |  ")
                    .Append(stateText)
                    .Append("  |  Hedef: %")
                    .Append(assignment.AccuracyTargetPercent);

                if (i < assignments.Count - 1)
                {
                    builder.AppendLine();
                }
            }
        }
        else
        {
            IReadOnlyList<TaskSelection> selectedTasks = TaskAssignmentSession.CurrentSelectedTasks;
            for (int i = 0; i < selectedTasks.Count; i++)
            {
                builder.Append(selectedTasks[i].DisplayName)
                    .Append("  |  Süre seçilmedi");

                if (i < selectedTasks.Count - 1)
                {
                    builder.AppendLine();
                }
            }
        }

        taskListLabel.text = builder.ToString();
    }

    private void RotateHands()
    {
        float minuteAngle = CurrentMinute / 60f * 360f;
        float hourAngle = ((CurrentHour % 12) + CurrentMinute / 60f) / 12f * 360f;
        float direction = clockwise ? -1f : 1f;
        Quaternion minuteRotation = Quaternion.Euler(0f, 0f, handZeroAngle + minuteAngle * direction);
        Quaternion hourRotation = Quaternion.Euler(0f, 0f, handZeroAngle + hourAngle * direction);

        if (minuteHand != null)
        {
            minuteHand.localRotation = minuteRotation;
        }

        if (hourHand != null)
        {
            hourHand.localRotation = hourRotation;
        }

        if (worldMinuteHand != null)
        {
            worldMinuteHand.localRotation = minuteRotation;
        }

        if (worldHourHand != null)
        {
            worldHourHand.localRotation = hourRotation;
        }
    }

    private void EnsureUi()
    {
        if (panelRoot != null && timeLabel != null && taskListLabel != null)
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(ClosePanel);
                closeButton.onClick.AddListener(ClosePanel);
            }

            return;
        }

        Canvas canvas = OfficeMiniGameUi.CreateOverlayCanvas("Game Time Canvas", transform, 1006);
        panelRoot = OfficeMiniGameUi.CreateImage("Game Time Panel", canvas.transform, new Color(0f, 0f, 0f, 0.45f));
        OfficeMiniGameUi.Stretch(panelRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        GameObject card = OfficeMiniGameUi.CreateImage("Clock Card", panelRoot.transform, new Color(0.12f, 0.13f, 0.14f, 0.95f));
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(720f, 420f);
        cardRect.anchoredPosition = Vector2.zero;

        timeLabel = OfficeMiniGameUi.CreateLabel("TimeLabel", card.transform, "09:00", 54f, Color.white);
        RectTransform timeRect = timeLabel.GetComponent<RectTransform>();
        timeRect.anchorMin = new Vector2(0f, 1f);
        timeRect.anchorMax = new Vector2(1f, 1f);
        timeRect.offsetMin = new Vector2(24f, -104f);
        timeRect.offsetMax = new Vector2(-24f, -24f);

        taskListLabel = OfficeMiniGameUi.CreateLabel("TaskListLabel", card.transform, string.Empty, 21f, Color.white);
        RectTransform taskListRect = taskListLabel.GetComponent<RectTransform>();
        taskListRect.anchorMin = Vector2.zero;
        taskListRect.anchorMax = Vector2.one;
        taskListRect.offsetMin = new Vector2(42f, 86f);
        taskListRect.offsetMax = new Vector2(-42f, -124f);
        taskListLabel.alignment = TextAlignmentOptions.TopLeft;
        taskListLabel.fontStyle = FontStyles.Bold;
        taskListLabel.textWrappingMode = TextWrappingModes.Normal;
        taskListLabel.enableAutoSizing = true;
        taskListLabel.fontSizeMin = 15f;
        taskListLabel.fontSizeMax = 21f;

        closeButton = OfficeMiniGameUi.CreateButton("Close Clock", card.transform, "KAPAT", new Vector2(130f, 48f), new Color(0.25f, 0.38f, 0.52f, 1f), ClosePanel);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0f);
        closeRect.anchorMax = new Vector2(0.5f, 0f);
        closeRect.anchoredPosition = new Vector2(0f, 38f);
    }
}
