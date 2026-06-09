using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CalendarManager : MonoBehaviour
{
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

    public int CurrentDay => currentDay;

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
        panelRoot.SetActive(!panelRoot.activeSelf);
        RefreshCalendar();
    }

    public void Open()
    {
        EnsureUi();
        panelRoot.SetActive(true);
        RefreshCalendar();
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

    private void HandleDeskObjectClicked(ClickableDeskObject clickedObject)
    {
        if (OfficeMiniGameUi.MatchesClickedObject(clickedObject, calendarObject, "calendar", "takvim"))
        {
            ToggleCalendar();
        }
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
                dayCells[i].Refresh(dayNumber, dayNumber < currentDay, dayNumber == currentDay);
            }
        }
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

        TextMeshProUGUI dayLabel = cellObject.GetComponentInChildren<TextMeshProUGUI>(true);
        if (dayLabel == null)
        {
            dayLabel = OfficeMiniGameUi.CreateLabel("DayLabel", cellObject.transform, dayNumber.ToString(), 22f, new Color(0.12f, 0.1f, 0.08f, 1f));
            OfficeMiniGameUi.Stretch(dayLabel.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        }

        GameObject tickObject = OfficeMiniGameUi.CreateLabel("Tick", cellObject.transform, "✓", 28f, new Color(0.2f, 0.55f, 0.28f, 1f)).gameObject;
        OfficeMiniGameUi.Stretch(tickObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        GameObject circleObject = OfficeMiniGameUi.CreateImage("CurrentCircle", cellObject.transform, new Color(0f, 0f, 0f, 0f));
        Image circleImage = circleObject.GetComponent<Image>();
        circleImage.color = new Color(0.25f, 0.45f, 0.85f, 0.25f);
        RectTransform circleRect = circleObject.GetComponent<RectTransform>();
        circleRect.anchorMin = new Vector2(0.5f, 0.5f);
        circleRect.anchorMax = new Vector2(0.5f, 0.5f);
        circleRect.sizeDelta = new Vector2(56f, 56f);
        circleRect.anchoredPosition = Vector2.zero;
        circleObject.transform.SetAsFirstSibling();

        return new CalendarDayCell(cellObject, dayLabel, tickObject, circleObject);
    }

    private void EnsureUi()
    {
        if (panelRoot != null && dayGrid != null)
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
        boardRect.sizeDelta = new Vector2(760f, 660f);
        boardRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI title = OfficeMiniGameUi.CreateLabel("Title", board.transform, "TAKVIM", 34f, new Color(0.12f, 0.1f, 0.08f, 1f));
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(32f, -78f);
        titleRect.offsetMax = new Vector2(-32f, -24f);

        GameObject gridObject = new GameObject("Day Grid", typeof(RectTransform), typeof(GridLayoutGroup));
        gridObject.transform.SetParent(board.transform, false);
        dayGrid = gridObject.GetComponent<RectTransform>();
        dayGrid.anchorMin = Vector2.zero;
        dayGrid.anchorMax = Vector2.one;
        dayGrid.offsetMin = new Vector2(46f, 64f);
        dayGrid.offsetMax = new Vector2(-46f, -104f);

        GridLayoutGroup grid = gridObject.GetComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 7;
        grid.cellSize = new Vector2(86f, 78f);
        grid.spacing = new Vector2(8f, 8f);

        closeButton = OfficeMiniGameUi.CreateButton("Close Calendar", panelRoot.transform, "X", new Vector2(54f, 54f), new Color(0.08f, 0.08f, 0.09f, 0.95f), Close);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0.5f);
        closeRect.anchorMax = new Vector2(0.5f, 0.5f);
        closeRect.anchoredPosition = new Vector2(410f, 360f);
    }

    private sealed class CalendarDayCell
    {
        public readonly GameObject Root;
        private readonly TextMeshProUGUI dayLabel;
        private readonly GameObject tickObject;
        private readonly GameObject circleObject;

        public CalendarDayCell(GameObject root, TextMeshProUGUI dayLabel, GameObject tickObject, GameObject circleObject)
        {
            Root = root;
            this.dayLabel = dayLabel;
            this.tickObject = tickObject;
            this.circleObject = circleObject;
        }

        public void Refresh(int dayNumber, bool completed, bool current)
        {
            dayLabel.text = dayNumber.ToString();
            tickObject.SetActive(completed);
            circleObject.SetActive(current);
        }
    }
}
