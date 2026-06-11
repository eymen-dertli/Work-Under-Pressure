using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class CustomerFileSortingController : MonoBehaviour
{
    public static event Action<int> MistakePercentReported;

    private const string ControllerName = "_CustomerFileSortingController";
    private const OfficeTaskKind TaskKind = OfficeTaskKind.CustomerFiles;
    private const int VisibleRows = 13;
    private const float RowHeight = 42f;

    private static readonly string[] CorrectNames =
    {
        "Ahmet Arslan",
        "Aylin Aydın",
        "Barış Balcı",
        "Berk Demir",
        "Cem Çelik",
        "Ceren Doğan",
        "Deniz Duran",
        "Ece Ekinci",
        "Emir Ergin",
        "Esra Güler",
        "Faruk Güneş",
        "Gizem Kaya",
        "Hakan Keskin",
        "İrem Kılıç",
        "Kaan Koç",
        "Lale Kurt",
        "Mert Mutlu",
        "Mina Öztürk",
        "Nehir Polat",
        "Onur Sarı",
        "Özge Şahin",
        "Pelin Taş",
        "Rana Tekin",
        "Seda Uçar",
        "Selim Uzun",
        "Sinem Yalçın",
        "Tuna Yıldız",
        "Umut Yılmaz",
        "Volkan Yücel",
        "Yasemin Zengin",
        "Zeynep Aksoy",
        "Alp Başar",
        "Burcu Can",
        "Canan Deniz",
    };

    private readonly List<string> currentOrder = new List<string>();
    private readonly List<TextMeshProUGUI> rowLabels = new List<TextMeshProUGUI>();
    private readonly List<Image> rowImages = new List<Image>();
    private readonly List<Button> rowButtons = new List<Button>();
    private readonly List<string> sortedNames = new List<string>();

    private GameObject panelRoot;
    private RectTransform listContent;
    private Slider scrollSlider;
    private TextMeshProUGUI statusLabel;
    private TextMeshProUGUI percentLabel;
    private TaskTimer taskTimer;
    private int selectedIndex;
    private int firstVisibleIndex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void RegisterSceneLoadedHook()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ClickableDeskObject.Clicked -= HandleDeskObjectClicked;
        ClickableDeskObject.Clicked += HandleDeskObjectClicked;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallForInitialScene()
    {
        EnsureController();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureController();
    }

    private static void HandleDeskObjectClicked(ClickableDeskObject clickedObject)
    {
        if (clickedObject == null || !IsCustomerFilesObject(clickedObject))
        {
            return;
        }

        EnsureController().Open();
    }

    private static CustomerFileSortingController EnsureController()
    {
        CustomerFileSortingController controller = FindAnyObjectByType<CustomerFileSortingController>();
        if (controller != null)
        {
            return controller;
        }

        GameObject controllerObject = new GameObject(ControllerName);
        return controllerObject.AddComponent<CustomerFileSortingController>();
    }

    private static bool IsCustomerFilesObject(ClickableDeskObject clickedObject)
    {
        string text = $"{clickedObject.ObjectId} {clickedObject.DisplayName} {clickedObject.gameObject.name}";
        text = NormalizeTurkish(text);
        return text.Contains("musteri") && text.Contains("dosya");
    }

    private static string NormalizeTurkish(string text)
    {
        return text.ToLowerInvariant()
            .Replace("ö", "o")
            .Replace("ü", "u")
            .Replace("ğ", "g")
            .Replace("ş", "s")
            .Replace("ı", "i")
            .Replace("ç", "c");
    }

    private void Awake()
    {
        BuildUi();
        PrepareSortedNames();
    }

    private void Open()
    {
        if (!TaskAssignmentSession.IsTaskEnabled(TaskKind))
        {
            return;
        }

        ShuffleNames();
        selectedIndex = 0;
        firstVisibleIndex = 0;
        panelRoot.SetActive(true);
        StartTaskTimer();
        statusLabel.text = "Müşteri dosyalarını alfabetik sıraya koy.";
        percentLabel.text = "Hata: %0";
        RefreshRows();
    }

    private void PrepareSortedNames()
    {
        sortedNames.Clear();
        sortedNames.AddRange(CorrectNames);
        sortedNames.Sort(StringComparer.Create(new CultureInfo("tr-TR"), false));
    }

    private void ShuffleNames()
    {
        currentOrder.Clear();
        currentOrder.AddRange(sortedNames);

        for (int i = 0; i < currentOrder.Count; i++)
        {
            int swapIndex = (i * 7 + 11) % currentOrder.Count;
            (currentOrder[i], currentOrder[swapIndex]) = (currentOrder[swapIndex], currentOrder[i]);
        }
    }

    private void SelectRow(int visibleRowIndex)
    {
        int index = firstVisibleIndex + visibleRowIndex;
        if (index < 0 || index >= currentOrder.Count)
        {
            return;
        }

        selectedIndex = index;
        RefreshRows();
    }

    private void MoveSelected(int direction)
    {
        int targetIndex = selectedIndex + direction;
        if (targetIndex < 0 || targetIndex >= currentOrder.Count)
        {
            return;
        }

        (currentOrder[selectedIndex], currentOrder[targetIndex]) = (currentOrder[targetIndex], currentOrder[selectedIndex]);
        selectedIndex = targetIndex;

        if (selectedIndex < firstVisibleIndex)
        {
            firstVisibleIndex = selectedIndex;
        }
        else if (selectedIndex >= firstVisibleIndex + VisibleRows)
        {
            firstVisibleIndex = selectedIndex - VisibleRows + 1;
        }

        RefreshRows();
    }

    private void ScrollList(int direction)
    {
        int maxFirstIndex = Mathf.Max(0, currentOrder.Count - VisibleRows);
        firstVisibleIndex = Mathf.Clamp(firstVisibleIndex + direction, 0, maxFirstIndex);
        RefreshRows();
    }

    private void HandleScrollSliderChanged(float value)
    {
        int maxFirstIndex = Mathf.Max(0, currentOrder.Count - VisibleRows);
        firstVisibleIndex = Mathf.RoundToInt((1f - value) * maxFirstIndex);
        RefreshRows(false);
    }

    private void CheckOrder()
    {
        int wrongCount = 0;

        for (int i = 0; i < currentOrder.Count; i++)
        {
            if (currentOrder[i] != sortedNames[i])
            {
                wrongCount++;
            }
        }

        int mistakePercent = Mathf.RoundToInt(wrongCount / (float)currentOrder.Count * 100f);
        int accuracy = TaskAssignmentSession.CalculateAccuracyPercent(currentOrder.Count - wrongCount, wrongCount);
        percentLabel.text = $"Hata: %{mistakePercent}";
        MistakePercentReported?.Invoke(mistakePercent);

        if (wrongCount == 0)
        {
            if (taskTimer != null)
            {
                taskTimer.StopTimer();
            }

            TaskAssignmentSession.MarkTaskCompleted(TaskKind);
            statusLabel.text = $"Doğru. Tüm dosyalar alfabetik sırada.\n{TaskAssignmentSession.BuildAccuracyLine(TaskKind, accuracy)}";
            return;
        }

        statusLabel.text = $"Sıralamada hata var. {wrongCount} dosya yanlış yerde.\n{TaskAssignmentSession.BuildAccuracyLine(TaskKind, accuracy)}";
    }

    private void StartTaskTimer()
    {
        if (!TaskAssignmentSession.TryGetAssignment(TaskKind, out TaskAssignment assignment))
        {
            return;
        }

        if (taskTimer == null)
        {
            taskTimer = gameObject.AddComponent<TaskTimer>();
        }

        taskTimer.TimerExpired -= HandleTaskTimerExpired;
        taskTimer.TimerExpired += HandleTaskTimerExpired;
        taskTimer.StartTimer(assignment.TimeLimitSeconds);
        TaskAssignmentSession.RegisterTaskTimer(TaskKind, taskTimer);
    }

    private void HandleTaskTimerExpired(TaskTimer expiredTimer)
    {
        TaskAssignmentSession.MarkTaskFailed(TaskKind);
        int wrongCount = 0;
        for (int i = 0; i < currentOrder.Count; i++)
        {
            if (currentOrder[i] != sortedNames[i])
            {
                wrongCount++;
            }
        }

        int accuracy = TaskAssignmentSession.CalculateAccuracyPercent(currentOrder.Count - wrongCount, wrongCount);
        statusLabel.text = $"Süre bitti. Görev başarısız.\n{TaskAssignmentSession.BuildAccuracyLine(TaskKind, accuracy)}";
    }

    private void BuildUi()
    {
        if (panelRoot != null)
        {
            return;
        }

        EnsureEventSystem();

        GameObject canvasObject = new GameObject("Customer File Sorting Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1002;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        panelRoot = CreateUiImage("Customer File Sorting Panel", canvasObject.transform, new Color(0f, 0f, 0f, 0.6f));
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        GameObject board = CreateUiImage("File Sorting Board", panelRoot.transform, new Color(0.94f, 0.92f, 0.86f, 1f));
        RectTransform boardRect = board.GetComponent<RectTransform>();
        boardRect.sizeDelta = new Vector2(1220f, 880f);
        boardRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI title = CreateLabel("Title", board.transform, "MÜŞTERİ DOSYALARI", 34f, new Color(0.12f, 0.1f, 0.08f, 1f));
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(40f, -82f);
        titleRect.offsetMax = new Vector2(-40f, -28f);

        statusLabel = CreateLabel("Status", board.transform, string.Empty, 22f, new Color(0.16f, 0.12f, 0.1f, 1f));
        RectTransform statusRect = statusLabel.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(1f, 0f);
        statusRect.offsetMin = new Vector2(54f, 78f);
        statusRect.offsetMax = new Vector2(-310f, 132f);
        statusLabel.alignment = TextAlignmentOptions.Left;

        percentLabel = CreateLabel("MistakePercent", board.transform, "Hata: %0", 24f, new Color(0.65f, 0.1f, 0.08f, 1f));
        RectTransform percentRect = percentLabel.GetComponent<RectTransform>();
        percentRect.anchorMin = new Vector2(1f, 0f);
        percentRect.anchorMax = new Vector2(1f, 0f);
        percentRect.sizeDelta = new Vector2(230f, 58f);
        percentRect.anchoredPosition = new Vector2(-175f, 105f);

        CreateList(board.transform);
        CreateControls(board.transform);
        CreateCloseButton(panelRoot.transform);

        panelRoot.SetActive(false);
    }

    private void CreateList(Transform parent)
    {
        GameObject listPanel = CreateUiImage("File List", parent, new Color(0.78f, 0.7f, 0.55f, 1f));
        RectTransform listRect = listPanel.GetComponent<RectTransform>();
        listRect.anchorMin = new Vector2(0f, 0f);
        listRect.anchorMax = new Vector2(1f, 1f);
        listRect.offsetMin = new Vector2(54f, 150f);
        listRect.offsetMax = new Vector2(-330f, -155f);

        listContent = listPanel.GetComponent<RectTransform>();

        for (int i = 0; i < VisibleRows; i++)
        {
            int rowIndex = i;
            GameObject row = CreateUiImage($"File Row {i + 1}", listPanel.transform, Color.white);
            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.offsetMin = new Vector2(16f, -14f - (i + 1) * RowHeight);
            rowRect.offsetMax = new Vector2(-58f, -16f - i * RowHeight);

            Button rowButton = row.AddComponent<Button>();
            rowButton.onClick.AddListener(() => SelectRow(rowIndex));

            TextMeshProUGUI label = CreateLabel("FileName", row.transform, string.Empty, 20f, new Color(0.12f, 0.1f, 0.08f, 1f));
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(18f, 0f);
            labelRect.offsetMax = new Vector2(-18f, 0f);
            label.alignment = TextAlignmentOptions.Left;

            rowImages.Add(row.GetComponent<Image>());
            rowLabels.Add(label);
            rowButtons.Add(rowButton);
        }

        CreateScrollSlider(listPanel.transform);
    }

    private void CreateScrollSlider(Transform parent)
    {
        GameObject sliderObject = CreateUiImage("File List Scrollbar", parent, new Color(0.5f, 0.42f, 0.32f, 1f));
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(1f, 0f);
        sliderRect.anchorMax = new Vector2(1f, 1f);
        sliderRect.offsetMin = new Vector2(-44f, 18f);
        sliderRect.offsetMax = new Vector2(-16f, -18f);

        scrollSlider = sliderObject.AddComponent<Slider>();
        scrollSlider.direction = Slider.Direction.BottomToTop;
        scrollSlider.minValue = 0f;
        scrollSlider.maxValue = 1f;
        scrollSlider.value = 1f;
        scrollSlider.wholeNumbers = false;

        GameObject handleObject = CreateUiImage("Handle", sliderObject.transform, new Color(0.95f, 0.82f, 0.42f, 1f));
        RectTransform handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0f, 0f);
        handleRect.anchorMax = new Vector2(1f, 0f);
        handleRect.sizeDelta = new Vector2(0f, 72f);
        handleRect.anchoredPosition = Vector2.zero;

        scrollSlider.handleRect = handleRect;
        scrollSlider.targetGraphic = handleObject.GetComponent<Image>();
        scrollSlider.onValueChanged.AddListener(HandleScrollSliderChanged);
    }

    private void CreateControls(Transform parent)
    {
        CreateControlButton(parent, "UpButton", "SEÇİLİYİ\nYUKARI AL", new Vector2(-165f, 205f), () => MoveSelected(-1));
        CreateControlButton(parent, "DownButton", "SEÇİLİYİ\nAŞAĞI AL", new Vector2(-165f, 125f), () => MoveSelected(1));
        CreateControlButton(parent, "ScrollUpButton", "LİSTEYİ\nYUKARI", new Vector2(-165f, 15f), () => ScrollList(-1));
        CreateControlButton(parent, "ScrollDownButton", "LİSTEYİ\nAŞAĞI", new Vector2(-165f, -65f), () => ScrollList(1));
        CreateControlButton(parent, "CompleteButton", "TAMAM", new Vector2(-165f, -205f), CheckOrder, new Color(0.25f, 0.55f, 0.25f, 1f));
    }

    private void CreateControlButton(Transform parent, string name, string text, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
    {
        CreateControlButton(parent, name, text, anchoredPosition, action, new Color(0.22f, 0.28f, 0.34f, 1f));
    }

    private void CreateControlButton(Transform parent, string name, string text, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action, Color color)
    {
        GameObject buttonObject = CreateUiImage(name, parent, color);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 0.5f);
        buttonRect.anchorMax = new Vector2(1f, 0.5f);
        buttonRect.sizeDelta = new Vector2(220f, 66f);
        buttonRect.anchoredPosition = anchoredPosition;

        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(action);

        TextMeshProUGUI label = CreateLabel("Label", buttonObject.transform, text, 18f, Color.white);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }

    private void CreateCloseButton(Transform parent)
    {
        GameObject buttonObject = CreateUiImage("Close Customer Sorting", parent, new Color(0.08f, 0.08f, 0.09f, 0.95f));
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(54f, 54f);
        buttonRect.anchoredPosition = new Vector2(635f, 465f);

        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(() => panelRoot.SetActive(false));

        TextMeshProUGUI label = CreateLabel("CloseLabel", buttonObject.transform, "X", 26f, Color.white);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }

    private void RefreshRows()
    {
        RefreshRows(true);
    }

    private void RefreshRows(bool updateSlider)
    {
        int maxFirstIndex = Mathf.Max(0, currentOrder.Count - VisibleRows);
        firstVisibleIndex = Mathf.Clamp(firstVisibleIndex, 0, maxFirstIndex);

        for (int i = 0; i < VisibleRows; i++)
        {
            int dataIndex = firstVisibleIndex + i;
            bool hasData = dataIndex < currentOrder.Count;

            rowButtons[i].interactable = hasData;
            rowLabels[i].text = hasData ? $"{dataIndex + 1:00}. {currentOrder[dataIndex]}" : string.Empty;
            rowImages[i].color = dataIndex == selectedIndex
                ? new Color(0.95f, 0.82f, 0.42f, 1f)
                : new Color(0.98f, 0.95f, 0.86f, 1f);
        }

        if (updateSlider && scrollSlider != null)
        {
            scrollSlider.interactable = maxFirstIndex > 0;
            float sliderValue = maxFirstIndex > 0 ? 1f - firstVisibleIndex / (float)maxFirstIndex : 1f;
            scrollSlider.SetValueWithoutNotify(sliderValue);
        }
    }

    private static GameObject CreateUiImage(string name, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = true;

        return imageObject;
    }

    private static TextMeshProUGUI CreateLabel(string name, Transform parent, string text, float fontSize, Color color)
    {
        GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.raycastTarget = false;

        return label;
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }
}
