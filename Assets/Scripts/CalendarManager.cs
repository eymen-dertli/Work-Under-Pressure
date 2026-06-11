using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class CalendarManager : MonoBehaviour
{
    private const string ControllerName = "_CalendarManager";
    private const string MonthTitle = "HAZİRAN";

    private static readonly CalendarPlan[] ExistingPlans =
    {
        new CalendarPlan(3, "09:30", "Ekip toplantısı", "Haftalık iş dağılımı ve tamamlanan görevlerin kısa kontrolü.", new Color(0.2f, 0.45f, 0.72f, 1f)),
        new CalendarPlan(5, "14:00", "Aksoy Lojistik", "Teslimat raporu ve eksik paket kontrolü.", new Color(0.11f, 0.36f, 0.62f, 1f)),
        new CalendarPlan(7, "11:15", "Mira Teknoloji", "Servis formu ve cihaz teslim notları incelenecek.", new Color(0.52f, 0.18f, 0.52f, 1f)),
        new CalendarPlan(10, "16:30", "Muhasebe kontrolü", "Masraf belgeleri ay sonu dosyasına işlenecek.", new Color(0.83f, 0.49f, 0.14f, 1f)),
        new CalendarPlan(12, "10:00", "Duru Gıda", "Depo teslim fişi ve stok listesi karşılaştırılacak.", new Color(0.14f, 0.46f, 0.25f, 1f)),
        new CalendarPlan(14, "15:45", "Aday görüşmesi", "İK notları hazırlanacak ve görüşme odası ayrılacak.", new Color(0.42f, 0.32f, 0.68f, 1f)),
        new CalendarPlan(18, "13:00", "Yılmaz Hukuk", "Sözleşme eki arşiv dosyasına eklenecek.", new Color(0.72f, 0.16f, 0.12f, 1f)),
        new CalendarPlan(21, "09:00", "Yazıcı bakımı", "Eksik sayfa ve toner kontrolü yapılacak.", new Color(0.35f, 0.38f, 0.4f, 1f)),
        new CalendarPlan(24, "12:30", "Müşteri araması", "Telefon rehberi ve geri dönüş notları güncellenecek.", new Color(0.16f, 0.54f, 0.58f, 1f)),
        new CalendarPlan(27, "17:00", "Ay sonu raporu", "Tüm tamamlanan evraklar son kez kontrol edilecek.", new Color(0.62f, 0.34f, 0.18f, 1f)),
    };

    private static readonly CalendarTask[] ScheduleTasks =
    {
        new CalendarTask(6, "10:00", "Aksoy Lojistik teslim kontrolü", "Aksoy Lojistik teslim kontrolünü 6 Haziran saat 10:00'a ekle.", new Color(0.11f, 0.36f, 0.62f, 1f)),
        new CalendarTask(9, "14:30", "Mira Teknoloji servis görüşmesi", "Mira Teknoloji servis görüşmesini 9 Haziran saat 14:30'a planla.", new Color(0.52f, 0.18f, 0.52f, 1f)),
        new CalendarTask(16, "11:00", "Duru Gıda stok araması", "Duru Gıda stok aramasını 16 Haziran saat 11:00'a ekle.", new Color(0.14f, 0.46f, 0.25f, 1f)),
        new CalendarTask(23, "15:30", "Yılmaz Hukuk dosya teslimi", "Yılmaz Hukuk dosya teslimini 23 Haziran saat 15:30'a planla.", new Color(0.72f, 0.16f, 0.12f, 1f)),
    };

    private static readonly string[] TimeOptions = { "10:00", "11:00", "14:30", "15:30" };
    private static readonly string[] WeekdayLabels = { "PZT", "SAL", "ÇAR", "PER", "CUM", "CTS", "PAZ" };

    [Header("Trigger")]
    [SerializeField] private bool listenToClickableDeskObjects = true;
    [SerializeField] private ClickableDeskObject calendarObject;

    [Header("Calendar")]
    [SerializeField] private int totalDays = 30;
    [SerializeField] private int currentDay = 1;

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private RectTransform dayGrid;
    [SerializeField] private GameObject dayCellPrefab;
    [SerializeField] private Button closeButton;

    private readonly List<CalendarDayCell> dayCells = new List<CalendarDayCell>();
    private readonly List<TextMeshProUGUI> detailPlanLabels = new List<TextMeshProUGUI>();
    private readonly List<CalendarPlan> scheduledPlans = new List<CalendarPlan>();
    private readonly List<Button> timeButtons = new List<Button>();
    private TextMeshProUGUI titleLabel;
    private TextMeshProUGUI selectedDayLabel;
    private TextMeshProUGUI selectedPlanCountLabel;
    private TextMeshProUGUI taskProgressLabel;
    private TextMeshProUGUI taskLabel;
    private TextMeshProUGUI selectedTimeLabel;
    private Button scheduleButton;
    private RectTransform detailListRoot;
    private int currentTaskIndex;
    private int correctSchedules;
    private int wrongSchedules;
    private string selectedTime;

    public int CurrentDay => currentDay;

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
        if (!IsLevelScene(scene.name) || FindAnyObjectByType<CalendarManager>() != null)
        {
            return;
        }

        GameObject managerObject = new GameObject(ControllerName);
        managerObject.AddComponent<CalendarManager>();
    }

    private static bool IsLevelScene(string sceneName)
    {
        LevelDefinition level = LevelDatabase.Load().GetLevelBySceneName(sceneName);
        return level != null;
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

    private void Start()
    {
        EnsureUi();
        RefreshCalendar();
        Close();
    }

    public void ToggleCalendar()
    {
        EnsureUi();
        bool open = !panelRoot.activeSelf;
        panelRoot.SetActive(open);

        if (open)
        {
            StartTaskSession();
        }
    }

    public void Open()
    {
        EnsureUi();
        panelRoot.SetActive(true);
        StartTaskSession();
    }

    public void Close()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    public void AdvanceDay()
    {
        AdvanceDay(1);
    }

    public void AdvanceDay(int dayCount)
    {
        currentDay = Mathf.Clamp(currentDay + Mathf.Max(1, dayCount), 1, Mathf.Max(1, totalDays));
        RefreshCalendar();
    }

    public void SetDay(int day)
    {
        currentDay = Mathf.Clamp(day, 1, Mathf.Max(1, totalDays));
        RefreshCalendar();
    }

    public void SetTime(string time)
    {
        selectedTime = time;
        RefreshTaskPanel();
    }

    public void ScheduleSelectedTask()
    {
        if (currentTaskIndex >= ScheduleTasks.Length)
        {
            return;
        }

        CalendarTask task = ScheduleTasks[currentTaskIndex];
        bool correct = currentDay == task.Day && selectedTime == task.Time;

        if (correct)
        {
            correctSchedules++;
            scheduledPlans.Add(task.ToCalendarPlan());
            currentTaskIndex++;
        }
        else
        {
            wrongSchedules++;
        }

        RefreshCalendar();
        RefreshTaskPanel();
    }

    private void HandleDeskObjectClicked(ClickableDeskObject clickedObject)
    {
        if (OfficeMiniGameUi.MatchesClickedObject(clickedObject, calendarObject, "calendar", "takvim"))
        {
            ToggleCalendar();
        }
    }

    private void StartTaskSession()
    {
        scheduledPlans.Clear();
        currentTaskIndex = 0;
        correctSchedules = 0;
        wrongSchedules = 0;
        currentDay = 1;
        selectedTime = TimeOptions[0];
        RefreshCalendar();
        RefreshTaskPanel();
    }

    private void RefreshCalendar()
    {
        if (dayGrid == null)
        {
            return;
        }

        while (dayCells.Count < totalDays)
        {
            CalendarDayCell cell = CreateDayCell(dayCells.Count + 1);
            dayCells.Add(cell);
        }

        for (int i = 0; i < dayCells.Count; i++)
        {
            int dayNumber = i + 1;
            bool isVisible = dayNumber <= totalDays;
            dayCells[i].Root.SetActive(isVisible);

            if (isVisible)
            {
                dayCells[i].Refresh(dayNumber, dayNumber == currentDay, GetPlansForDay(dayNumber));
            }
        }

        RefreshSelectedDayDetails();
        RefreshTaskPanel();
    }

    private CalendarDayCell CreateDayCell(int dayNumber)
    {
        GameObject cellObject;
        if (dayCellPrefab != null)
        {
            cellObject = Instantiate(dayCellPrefab, dayGrid);
        }
        else
        {
            cellObject = OfficeMiniGameUi.CreateImage($"Day {dayNumber}", dayGrid, new Color(0.95f, 0.93f, 0.86f, 1f));
        }

        Button dayButton = cellObject.GetComponent<Button>();
        if (dayButton == null)
        {
            dayButton = cellObject.AddComponent<Button>();
        }

        int capturedDay = dayNumber;
        dayButton.onClick.RemoveAllListeners();
        dayButton.onClick.AddListener(() => SetDay(capturedDay));

        TextMeshProUGUI dayLabel = OfficeMiniGameUi.CreateLabel("DayLabel", cellObject.transform, dayNumber.ToString(), 22f, new Color(0.12f, 0.1f, 0.08f, 1f));
        RectTransform dayLabelRect = dayLabel.GetComponent<RectTransform>();
        dayLabelRect.anchorMin = new Vector2(0f, 1f);
        dayLabelRect.anchorMax = new Vector2(0f, 1f);
        dayLabelRect.sizeDelta = new Vector2(48f, 34f);
        dayLabelRect.anchoredPosition = new Vector2(28f, -20f);

        GameObject selectionObject = OfficeMiniGameUi.CreateImage("SelectedFrame", cellObject.transform, new Color(0.18f, 0.42f, 0.68f, 0.18f));
        selectionObject.GetComponent<Image>().raycastTarget = false;
        OfficeMiniGameUi.Stretch(selectionObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        selectionObject.transform.SetAsFirstSibling();

        RectTransform planRoot = new GameObject("Plan Dots", typeof(RectTransform)).GetComponent<RectTransform>();
        planRoot.transform.SetParent(cellObject.transform, false);
        planRoot.anchorMin = new Vector2(0f, 0f);
        planRoot.anchorMax = new Vector2(1f, 0f);
        planRoot.offsetMin = new Vector2(10f, 8f);
        planRoot.offsetMax = new Vector2(-10f, 28f);

        HorizontalLayoutGroup dotLayout = planRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        dotLayout.childAlignment = TextAnchor.MiddleLeft;
        dotLayout.childControlWidth = false;
        dotLayout.childControlHeight = false;
        dotLayout.childForceExpandWidth = false;
        dotLayout.childForceExpandHeight = false;
        dotLayout.spacing = 4f;

        List<Image> planDots = new List<Image>();
        for (int i = 0; i < 3; i++)
        {
            Image dot = OfficeMiniGameUi.CreateImage($"Plan Dot {i + 1}", planRoot, Color.white).GetComponent<Image>();
            RectTransform dotRect = dot.GetComponent<RectTransform>();
            dotRect.sizeDelta = new Vector2(18f, 8f);
            dot.raycastTarget = false;
            planDots.Add(dot);
        }

        return new CalendarDayCell(cellObject, dayLabel, selectionObject, planDots);
    }

    private void EnsureUi()
    {
        if (panelRoot != null && dayGrid != null && detailListRoot != null)
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }

            return;
        }

        Canvas canvas = OfficeMiniGameUi.CreateOverlayCanvas("Calendar Canvas", transform, 1005);
        panelRoot = OfficeMiniGameUi.CreateImage("Calendar Panel", canvas.transform, new Color(0f, 0f, 0f, 0.55f));
        OfficeMiniGameUi.Stretch(panelRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        GameObject board = OfficeMiniGameUi.CreateImage("Calendar Board", panelRoot.transform, new Color(0.9f, 0.87f, 0.78f, 1f));
        RectTransform boardRect = board.GetComponent<RectTransform>();
        boardRect.sizeDelta = new Vector2(1120f, 720f);
        boardRect.anchoredPosition = Vector2.zero;

        titleLabel = OfficeMiniGameUi.CreateLabel("Title", board.transform, $"{MonthTitle} TAKVİMİ", 34f, new Color(0.12f, 0.1f, 0.08f, 1f));
        RectTransform titleRect = titleLabel.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(38f, -72f);
        titleRect.offsetMax = new Vector2(-360f, -24f);
        titleLabel.alignment = TextAlignmentOptions.Left;

        CreateWeekdayRow(board.transform);
        CreateDayGrid(board.transform);
        CreateDetailPanel(board.transform);

        closeButton = OfficeMiniGameUi.CreateButton("Close Calendar", panelRoot.transform, "X", new Vector2(54f, 54f), new Color(0.08f, 0.08f, 0.09f, 0.95f), Close);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0.5f);
        closeRect.anchorMax = new Vector2(0.5f, 0.5f);
        closeRect.anchoredPosition = new Vector2(585f, 390f);
    }

    private void CreateWeekdayRow(Transform board)
    {
        RectTransform weekdayRow = new GameObject("Weekday Row", typeof(RectTransform), typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
        weekdayRow.transform.SetParent(board, false);
        weekdayRow.anchorMin = new Vector2(0f, 1f);
        weekdayRow.anchorMax = new Vector2(0f, 1f);
        weekdayRow.sizeDelta = new Vector2(688f, 34f);
        weekdayRow.anchoredPosition = new Vector2(386f, -104f);

        HorizontalLayoutGroup layout = weekdayRow.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = 8f;

        for (int i = 0; i < WeekdayLabels.Length; i++)
        {
            TextMeshProUGUI label = OfficeMiniGameUi.CreateLabel($"Weekday {WeekdayLabels[i]}", weekdayRow, WeekdayLabels[i], 16f, new Color(0.34f, 0.26f, 0.18f, 1f));
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(92f, 28f);
        }
    }

    private void CreateDayGrid(Transform board)
    {
        GameObject gridObject = new GameObject("Day Grid", typeof(RectTransform), typeof(GridLayoutGroup));
        gridObject.transform.SetParent(board, false);
        dayGrid = gridObject.GetComponent<RectTransform>();
        dayGrid.anchorMin = new Vector2(0f, 0f);
        dayGrid.anchorMax = new Vector2(0f, 1f);
        dayGrid.offsetMin = new Vector2(38f, 76f);
        dayGrid.offsetMax = new Vector2(726f, -130f);

        GridLayoutGroup grid = gridObject.GetComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 7;
        grid.cellSize = new Vector2(92f, 78f);
        grid.spacing = new Vector2(8f, 8f);
    }

    private void CreateDetailPanel(Transform board)
    {
        GameObject detailPanel = OfficeMiniGameUi.CreateImage("Daily Plan Panel", board, new Color(0.28f, 0.22f, 0.16f, 0.96f));
        RectTransform detailRect = detailPanel.GetComponent<RectTransform>();
        detailRect.anchorMin = new Vector2(1f, 0.5f);
        detailRect.anchorMax = new Vector2(1f, 0.5f);
        detailRect.sizeDelta = new Vector2(330f, 570f);
        detailRect.anchoredPosition = new Vector2(-196f, -6f);

        selectedDayLabel = OfficeMiniGameUi.CreateLabel("SelectedDay", detailPanel.transform, "1 HAZIRAN", 29f, Color.white);
        RectTransform selectedRect = selectedDayLabel.GetComponent<RectTransform>();
        selectedRect.anchorMin = new Vector2(0f, 1f);
        selectedRect.anchorMax = new Vector2(1f, 1f);
        selectedRect.offsetMin = new Vector2(22f, -68f);
        selectedRect.offsetMax = new Vector2(-22f, -20f);
        selectedDayLabel.alignment = TextAlignmentOptions.Left;

        selectedPlanCountLabel = OfficeMiniGameUi.CreateLabel("PlanCount", detailPanel.transform, "Plan yok", 18f, new Color(0.94f, 0.86f, 0.68f, 1f));
        RectTransform countRect = selectedPlanCountLabel.GetComponent<RectTransform>();
        countRect.anchorMin = new Vector2(0f, 1f);
        countRect.anchorMax = new Vector2(1f, 1f);
        countRect.offsetMin = new Vector2(22f, -104f);
        countRect.offsetMax = new Vector2(-22f, -72f);
        selectedPlanCountLabel.alignment = TextAlignmentOptions.Left;

        detailListRoot = new GameObject("Plan List", typeof(RectTransform), typeof(VerticalLayoutGroup)).GetComponent<RectTransform>();
        detailListRoot.transform.SetParent(detailPanel.transform, false);
        detailListRoot.anchorMin = Vector2.zero;
        detailListRoot.anchorMax = Vector2.one;
        detailListRoot.offsetMin = new Vector2(22f, 256f);
        detailListRoot.offsetMax = new Vector2(-22f, -122f);

        VerticalLayoutGroup layout = detailListRoot.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 12f;

        CreateSchedulingPanel(detailPanel.transform);
    }

    private void CreateSchedulingPanel(Transform detailPanel)
    {
        GameObject taskPanel = OfficeMiniGameUi.CreateImage("Scheduling Task Panel", detailPanel, new Color(0.96f, 0.92f, 0.82f, 1f));
        RectTransform taskRect = taskPanel.GetComponent<RectTransform>();
        taskRect.anchorMin = new Vector2(0f, 0f);
        taskRect.anchorMax = new Vector2(1f, 0f);
        taskRect.offsetMin = new Vector2(22f, 22f);
        taskRect.offsetMax = new Vector2(-22f, 236f);

        taskProgressLabel = OfficeMiniGameUi.CreateLabel("TaskProgress", taskPanel.transform, string.Empty, 17f, new Color(0.38f, 0.27f, 0.17f, 1f));
        RectTransform progressRect = taskProgressLabel.GetComponent<RectTransform>();
        progressRect.anchorMin = new Vector2(0f, 1f);
        progressRect.anchorMax = new Vector2(1f, 1f);
        progressRect.offsetMin = new Vector2(14f, -34f);
        progressRect.offsetMax = new Vector2(-14f, -8f);
        taskProgressLabel.alignment = TextAlignmentOptions.Left;

        taskLabel = OfficeMiniGameUi.CreateLabel("TaskText", taskPanel.transform, string.Empty, 16f, new Color(0.12f, 0.1f, 0.08f, 1f));
        RectTransform taskLabelRect = taskLabel.GetComponent<RectTransform>();
        taskLabelRect.anchorMin = new Vector2(0f, 1f);
        taskLabelRect.anchorMax = new Vector2(1f, 1f);
        taskLabelRect.offsetMin = new Vector2(14f, -98f);
        taskLabelRect.offsetMax = new Vector2(-14f, -38f);
        taskLabel.alignment = TextAlignmentOptions.TopLeft;
        taskLabel.textWrappingMode = TextWrappingModes.Normal;
        taskLabel.enableAutoSizing = true;
        taskLabel.fontSizeMin = 12f;
        taskLabel.fontSizeMax = 16f;

        selectedTimeLabel = OfficeMiniGameUi.CreateLabel("SelectedTime", taskPanel.transform, string.Empty, 15f, new Color(0.38f, 0.27f, 0.17f, 1f));
        RectTransform selectedTimeRect = selectedTimeLabel.GetComponent<RectTransform>();
        selectedTimeRect.anchorMin = new Vector2(0f, 0f);
        selectedTimeRect.anchorMax = new Vector2(1f, 0f);
        selectedTimeRect.offsetMin = new Vector2(14f, 78f);
        selectedTimeRect.offsetMax = new Vector2(-14f, 106f);
        selectedTimeLabel.alignment = TextAlignmentOptions.Left;

        RectTransform timeRow = new GameObject("Time Row", typeof(RectTransform), typeof(GridLayoutGroup)).GetComponent<RectTransform>();
        timeRow.transform.SetParent(taskPanel.transform, false);
        timeRow.anchorMin = new Vector2(0f, 0f);
        timeRow.anchorMax = new Vector2(1f, 0f);
        timeRow.offsetMin = new Vector2(14f, 48f);
        timeRow.offsetMax = new Vector2(-14f, 80f);

        GridLayoutGroup timeGrid = timeRow.GetComponent<GridLayoutGroup>();
        timeGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        timeGrid.constraintCount = 4;
        timeGrid.cellSize = new Vector2(62f, 30f);
        timeGrid.spacing = new Vector2(6f, 0f);

        timeButtons.Clear();
        for (int i = 0; i < TimeOptions.Length; i++)
        {
            string time = TimeOptions[i];
            Button timeButton = OfficeMiniGameUi.CreateButton($"Time {time}", timeRow, time, new Vector2(62f, 30f), new Color(0.38f, 0.28f, 0.2f, 1f), () => SetTime(time));
            TextMeshProUGUI label = timeButton.GetComponentInChildren<TextMeshProUGUI>();
            label.fontSize = 14f;
            timeButtons.Add(timeButton);
        }

        scheduleButton = OfficeMiniGameUi.CreateButton("Schedule Task", taskPanel.transform, "PLANLA", new Vector2(118f, 36f), new Color(0.2f, 0.45f, 0.34f, 1f), ScheduleSelectedTask);
        RectTransform scheduleRect = scheduleButton.GetComponent<RectTransform>();
        scheduleRect.anchorMin = new Vector2(1f, 0f);
        scheduleRect.anchorMax = new Vector2(1f, 0f);
        scheduleRect.anchoredPosition = new Vector2(-74f, 24f);
    }

    private void RefreshSelectedDayDetails()
    {
        if (selectedDayLabel == null || selectedPlanCountLabel == null || detailListRoot == null)
        {
            return;
        }

        CalendarPlan[] plans = GetPlansForDay(currentDay);
        selectedDayLabel.text = $"{currentDay} {MonthTitle}";
        selectedPlanCountLabel.text = plans.Length == 0 ? "Plan yok" : $"{plans.Length} plan";

        OfficeMiniGameUi.ClearChildren(detailListRoot);
        detailPlanLabels.Clear();

        if (plans.Length == 0)
        {
            CreatePlanCard("Boş Zaman", "Bugün için kayıtlı plan yok.", new Color(0.58f, 0.52f, 0.45f, 1f));
            return;
        }

        for (int i = 0; i < plans.Length; i++)
        {
            CalendarPlan plan = plans[i];
            CreatePlanCard($"{plan.Time}  {plan.Title}", plan.Description, plan.Color);
        }
    }

    private void RefreshTaskPanel()
    {
        if (taskProgressLabel == null || taskLabel == null || selectedTimeLabel == null || scheduleButton == null)
        {
            return;
        }

        bool hasTask = currentTaskIndex < ScheduleTasks.Length;
        taskProgressLabel.text = hasTask
            ? $"Görev {currentTaskIndex + 1}/{ScheduleTasks.Length}   Doğru: {correctSchedules}   Yanlış: {wrongSchedules}"
            : $"Tamamlandı   Doğru: {correctSchedules}   Yanlış: {wrongSchedules}";

        taskLabel.text = hasTask
            ? ScheduleTasks[currentTaskIndex].Instruction
            : "Tüm planlama notları takvime eklendi.";

        selectedTimeLabel.text = $"Seçilen: {currentDay} {MonthTitle}, {selectedTime}";
        scheduleButton.interactable = hasTask;

        for (int i = 0; i < timeButtons.Count; i++)
        {
            Image image = timeButtons[i].GetComponent<Image>();
            TextMeshProUGUI label = timeButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            bool selected = i < TimeOptions.Length && TimeOptions[i] == selectedTime;

            if (image != null)
            {
                image.color = selected ? new Color(0.2f, 0.45f, 0.34f, 1f) : new Color(0.38f, 0.28f, 0.2f, 1f);
            }

            if (label != null)
            {
                label.color = Color.white;
            }

            timeButtons[i].interactable = hasTask;
        }
    }

    private void CreatePlanCard(string title, string description, Color accentColor)
    {
        GameObject card = OfficeMiniGameUi.CreateImage($"Plan {title}", detailListRoot, new Color(0.96f, 0.92f, 0.82f, 1f));
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(0f, 104f);

        GameObject accent = OfficeMiniGameUi.CreateImage("Accent", card.transform, accentColor);
        RectTransform accentRect = accent.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 0f);
        accentRect.anchorMax = new Vector2(0f, 1f);
        accentRect.offsetMin = Vector2.zero;
        accentRect.offsetMax = new Vector2(8f, 0f);

        TextMeshProUGUI titleLabel = OfficeMiniGameUi.CreateLabel("PlanTitle", card.transform, title, 19f, new Color(0.12f, 0.1f, 0.08f, 1f));
        RectTransform titleRect = titleLabel.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(18f, -38f);
        titleRect.offsetMax = new Vector2(-12f, -8f);
        titleLabel.alignment = TextAlignmentOptions.Left;
        titleLabel.enableAutoSizing = true;
        titleLabel.fontSizeMin = 14f;
        titleLabel.fontSizeMax = 19f;

        TextMeshProUGUI descriptionLabel = OfficeMiniGameUi.CreateLabel("PlanDescription", card.transform, description, 15f, new Color(0.22f, 0.18f, 0.14f, 1f));
        RectTransform descriptionRect = descriptionLabel.GetComponent<RectTransform>();
        descriptionRect.anchorMin = new Vector2(0f, 0f);
        descriptionRect.anchorMax = new Vector2(1f, 1f);
        descriptionRect.offsetMin = new Vector2(18f, 12f);
        descriptionRect.offsetMax = new Vector2(-12f, -42f);
        descriptionLabel.alignment = TextAlignmentOptions.TopLeft;
        descriptionLabel.textWrappingMode = TextWrappingModes.Normal;
        descriptionLabel.enableAutoSizing = true;
        descriptionLabel.fontSizeMin = 11f;
        descriptionLabel.fontSizeMax = 15f;

        detailPlanLabels.Add(titleLabel);
        detailPlanLabels.Add(descriptionLabel);
    }

    private CalendarPlan[] GetPlansForDay(int day)
    {
        List<CalendarPlan> plans = new List<CalendarPlan>();
        for (int i = 0; i < ExistingPlans.Length; i++)
        {
            if (ExistingPlans[i].Day == day)
            {
                plans.Add(ExistingPlans[i]);
            }
        }

        for (int i = 0; i < scheduledPlans.Count; i++)
        {
            if (scheduledPlans[i].Day == day)
            {
                plans.Add(scheduledPlans[i]);
            }
        }

        return plans.ToArray();
    }

    private readonly struct CalendarTask
    {
        public readonly int Day;
        public readonly string Time;
        public readonly string Title;
        public readonly string Instruction;
        public readonly Color Color;

        public CalendarTask(int day, string time, string title, string instruction, Color color)
        {
            Day = day;
            Time = time;
            Title = title;
            Instruction = instruction;
            Color = color;
        }

        public CalendarPlan ToCalendarPlan()
        {
            return new CalendarPlan(Day, Time, Title, "Oyuncu tarafından takvime eklendi.", Color);
        }
    }

    private readonly struct CalendarPlan
    {
        public readonly int Day;
        public readonly string Time;
        public readonly string Title;
        public readonly string Description;
        public readonly Color Color;

        public CalendarPlan(int day, string time, string title, string description, Color color)
        {
            Day = day;
            Time = time;
            Title = title;
            Description = description;
            Color = color;
        }
    }

    private sealed class CalendarDayCell
    {
        public readonly GameObject Root;
        private readonly TextMeshProUGUI dayLabel;
        private readonly GameObject selectionObject;
        private readonly List<Image> planDots;

        public CalendarDayCell(GameObject root, TextMeshProUGUI dayLabel, GameObject selectionObject, List<Image> planDots)
        {
            Root = root;
            this.dayLabel = dayLabel;
            this.selectionObject = selectionObject;
            this.planDots = planDots;
        }

        public void Refresh(int dayNumber, bool selected, IReadOnlyList<CalendarPlan> plans)
        {
            dayLabel.text = dayNumber.ToString();
            selectionObject.SetActive(selected);

            for (int i = 0; i < planDots.Count; i++)
            {
                bool hasPlan = i < plans.Count;
                planDots[i].gameObject.SetActive(hasPlan);
                if (hasPlan)
                {
                    planDots[i].color = plans[i].Color;
                }
            }
        }
    }
}
