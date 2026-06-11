using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MailSchedulingController : MonoBehaviour
{
    private const string ControllerName = "_MailSchedulingController";
    private const OfficeTaskKind TaskKind = OfficeTaskKind.Mail;

    private static readonly MailTask[] MailTasks =
    {
        new MailTask("TOPLANTI SAATİ HAKKINDA", "Toplantı", "Merhaba,\n\nBu hafta içerisinde gerçekleştirmeyi planladığımız toplantı için uygun olduğunuz saat aralıklarını paylaşabilir misiniz? Takvime göre planlama yapılacaktır."),
        new MailTask("İŞ GÖRÜŞMESİ SAATİ", "İş görüşmesi", "Sayın Aday,\n\nÖn değerlendirme sürecini başarıyla tamamladınız. İş görüşmesinin planlanabilmesi adına müsait olduğunuz tarih ve saat bilgilerini iletmenizi rica ederiz."),
        new MailTask("DOKTOR MUAYENESİ", "Doktor muayenesi", "Merhaba,\n\nDoktor muayene randevunuzun oluşturulabilmesi için tercih ettiğiniz uygun saat aralığını bizimle paylaşabilir misiniz?"),
        new MailTask("SEMİNER SAATİ", "Seminer", "Sayın Katılımcı,\n\nSeminer programının kesinleştirilebilmesi adına katılım sağlayabileceğiniz saat bilgisini tarafımıza iletmenizi rica ederiz."),
        new MailTask("2. TOPLANTI SAATİ", "2. toplantı", "Merhaba,\n\nEkip içi değerlendirme toplantısının planlaması yapılmaktadır. Katılım sağlayabileceğiniz uygun saat aralıklarını paylaşmanız rica olunur."),
        new MailTask("YENİ İŞ TALEBİ", "Yeni iş", "Merhaba,\n\nTarafınıza yönlendirilen yeni iş talebi için çalışmaya başlayabileceğiniz uygun saat bilgisini iletebilir misiniz? Planlama buna göre gerçekleştirilecektir."),
    };

    private static readonly string[] AvailableHours =
    {
        "11.00",
        "13.00",
        "14.00",
        "15.00",
        "16.00",
        "17.00",
    };

    private readonly Dictionary<int, string> hourByMailIndex = new Dictionary<int, string>();
    private readonly Dictionary<string, int> mailIndexByHour = new Dictionary<string, int>();
    private readonly List<Image> inboxItemImages = new List<Image>();
    private readonly List<TextMeshProUGUI> inboxItemLabels = new List<TextMeshProUGUI>();
    private readonly List<Button> hourButtons = new List<Button>();

    private GameObject panelRoot;
    private TextMeshProUGUI mailTitleLabel;
    private TextMeshProUGUI mailBodyLabel;
    private TextMeshProUGUI statusLabel;
    private TextMeshProUGUI progressLabel;
    private TaskTimer taskTimer;
    private int selectedMailIndex;
    private int wrongAttemptCount;
    private bool taskStarted;

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
        if (clickedObject == null || !IsPcObject(clickedObject))
        {
            return;
        }

        EnsureController().Open();
    }

    private static MailSchedulingController EnsureController()
    {
        MailSchedulingController controller = FindAnyObjectByType<MailSchedulingController>();
        if (controller != null)
        {
            return controller;
        }

        GameObject controllerObject = new GameObject(ControllerName);
        return controllerObject.AddComponent<MailSchedulingController>();
    }

    private static bool IsPcObject(ClickableDeskObject clickedObject)
    {
        string text = $"{clickedObject.ObjectId} {clickedObject.DisplayName} {clickedObject.gameObject.name}";
        text = text.ToLowerInvariant().Replace(" ", string.Empty);
        return text.Contains("pc") || text.Contains("bilgisayar");
    }

    private void Awake()
    {
        BuildUi();
    }

    private void Open()
    {
        if (!TaskAssignmentSession.IsTaskEnabled(TaskKind))
        {
            return;
        }

        if (taskStarted)
        {
            panelRoot.SetActive(true);
            SelectMail(selectedMailIndex);
            return;
        }

        selectedMailIndex = 0;
        wrongAttemptCount = 0;
        taskStarted = true;
        hourByMailIndex.Clear();
        mailIndexByHour.Clear();
        panelRoot.SetActive(true);
        StartTaskTimer();
        SelectMail(0, "Sol listeden bir mail seç veya bu mail için saat belirle.");
    }

    private void SelectMail(int mailIndex, string statusText = "")
    {
        selectedMailIndex = mailIndex;
        MailTask mailTask = MailTasks[selectedMailIndex];

        mailTitleLabel.text = mailTask.Title;
        mailBodyLabel.text = $"{mailTask.Body}\n\nSüre: 1 saat";
        if (hourByMailIndex.Count == MailTasks.Length)
        {
            int accuracy = TaskAssignmentSession.CalculateAccuracyPercent(MailTasks.Length, wrongAttemptCount);
            statusLabel.text = $"Tüm mailler çakışma olmadan planlandı.\n{TaskAssignmentSession.BuildAccuracyLine(TaskKind, accuracy)}";
        }
        else
        {
            statusLabel.text = string.IsNullOrEmpty(statusText) ? "Bu mail için uygun bir saat seç." : statusText;
        }

        UpdateInboxVisuals();
        UpdateProgressLabel();
        UpdateHourButtonVisuals();
    }

    private void SelectHour(string hour)
    {
        if (mailIndexByHour.TryGetValue(hour, out int existingMailIndex) && existingMailIndex != selectedMailIndex)
        {
            wrongAttemptCount++;
            SelectMail(selectedMailIndex, $"{hour} zaten '{MailTasks[existingMailIndex].InboxTitle}' için seçildi. Farklı saat seç.");
            return;
        }

        if (hourByMailIndex.TryGetValue(selectedMailIndex, out string previousHour))
        {
            mailIndexByHour.Remove(previousHour);
        }

        hourByMailIndex[selectedMailIndex] = hour;
        mailIndexByHour[hour] = selectedMailIndex;

        string nextStatus = hourByMailIndex.Count == MailTasks.Length
            ? "Tüm mailler çakışma olmadan planlandı."
            : $"{hour} kaydedildi. Soldan sıradaki maili seç.";

        if (hourByMailIndex.Count == MailTasks.Length)
        {
            if (taskTimer != null)
            {
                taskTimer.StopTimer();
            }

            TaskAssignmentSession.MarkTaskCompleted(TaskKind);
            int accuracy = TaskAssignmentSession.CalculateAccuracyPercent(MailTasks.Length, wrongAttemptCount);
            nextStatus = $"{nextStatus}\n{TaskAssignmentSession.BuildAccuracyLine(TaskKind, accuracy)}";
        }

        SelectMail(selectedMailIndex, nextStatus);
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
        int accuracy = TaskAssignmentSession.CalculateAccuracyPercent(hourByMailIndex.Count, wrongAttemptCount);
        statusLabel.text = $"Süre bitti. Görev başarısız.\n{TaskAssignmentSession.BuildAccuracyLine(TaskKind, accuracy)}";
    }

    private void BuildUi()
    {
        if (panelRoot != null)
        {
            return;
        }

        EnsureEventSystem();

        GameObject canvasObject = new GameObject("Mail Scheduling Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1001;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        panelRoot = CreateUiImage("Mail Scheduling Panel", canvasObject.transform, new Color(0f, 0f, 0f, 0.62f));
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        GameObject monitor = CreateUiImage("Computer Screen", panelRoot.transform, new Color(0.02f, 0.02f, 0.025f, 1f));
        RectTransform monitorRect = monitor.GetComponent<RectTransform>();
        monitorRect.sizeDelta = new Vector2(1220f, 720f);
        monitorRect.anchoredPosition = Vector2.zero;

        GameObject mailWindow = CreateUiImage("Outlook Mail Window", monitor.transform, new Color(0.96f, 0.96f, 0.95f, 1f));
        RectTransform windowRect = mailWindow.GetComponent<RectTransform>();
        windowRect.anchorMin = Vector2.zero;
        windowRect.anchorMax = Vector2.one;
        windowRect.offsetMin = new Vector2(26f, 26f);
        windowRect.offsetMax = new Vector2(-26f, -26f);

        CreateInboxPane(mailWindow.transform);
        CreateMailDetailPane(mailWindow.transform);
        CreateCloseButton(panelRoot.transform);

        panelRoot.SetActive(false);
    }

    private void CreateInboxPane(Transform parent)
    {
        GameObject inboxPane = CreateUiImage("Inbox Pane", parent, new Color(0.94f, 0.94f, 0.92f, 1f));
        RectTransform paneRect = inboxPane.GetComponent<RectTransform>();
        paneRect.anchorMin = new Vector2(0f, 0f);
        paneRect.anchorMax = new Vector2(0f, 1f);
        paneRect.sizeDelta = new Vector2(360f, 0f);
        paneRect.anchoredPosition = new Vector2(180f, 0f);

        TextMeshProUGUI inboxTitle = CreateLabel("InboxTitle", inboxPane.transform, "GELEN KUTUSU", 18f, new Color(0.1f, 0.1f, 0.1f, 1f));
        RectTransform titleRect = inboxTitle.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(20f, -82f);
        titleRect.offsetMax = new Vector2(-20f, -28f);

        CreateSeparator(inboxPane.transform, "InboxTitleSeparator", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -112f), new Vector2(0f, -109f));

        for (int i = 0; i < MailTasks.Length; i++)
        {
            int mailIndex = i;
            GameObject item = CreateUiImage($"Inbox Item {i + 1}", inboxPane.transform, Color.white);
            RectTransform itemRect = item.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 1f);
            itemRect.anchorMax = new Vector2(1f, 1f);
            itemRect.offsetMin = new Vector2(0f, -180f - i * 76f);
            itemRect.offsetMax = new Vector2(0f, -112f - i * 76f);

            Button button = item.AddComponent<Button>();
            button.onClick.AddListener(() => SelectMail(mailIndex));

            TextMeshProUGUI label = CreateLabel("InboxItemLabel", item.transform, MailTasks[i].InboxTitle, 17f, new Color(0.1f, 0.1f, 0.1f, 1f));
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(18f, 8f);
            labelRect.offsetMax = new Vector2(-18f, -8f);
            label.alignment = TextAlignmentOptions.Left;

            inboxItemImages.Add(item.GetComponent<Image>());
            inboxItemLabels.Add(label);
            CreateSeparator(inboxPane.transform, $"InboxSeparator {i + 1}", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -180f - i * 76f), new Vector2(0f, -177f - i * 76f));
        }
    }

    private void CreateMailDetailPane(Transform parent)
    {
        GameObject detailPane = CreateUiImage("Mail Detail Pane", parent, new Color(0.98f, 0.98f, 0.97f, 1f));
        RectTransform paneRect = detailPane.GetComponent<RectTransform>();
        paneRect.anchorMin = Vector2.zero;
        paneRect.anchorMax = Vector2.one;
        paneRect.offsetMin = new Vector2(360f, 0f);
        paneRect.offsetMax = Vector2.zero;

        CreateSeparator(parent, "VerticalSeparator", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(360f, 0f), new Vector2(363f, 0f));

        mailTitleLabel = CreateLabel("MailTitle", detailPane.transform, string.Empty, 36f, Color.black);
        RectTransform titleRect = mailTitleLabel.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(48f, -112f);
        titleRect.offsetMax = new Vector2(-48f, -42f);
        mailTitleLabel.alignment = TextAlignmentOptions.Left;

        mailBodyLabel = CreateLabel("MailBody", detailPane.transform, string.Empty, 21f, new Color(0.08f, 0.08f, 0.08f, 1f));
        RectTransform bodyRect = mailBodyLabel.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 1f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(52f, -270f);
        bodyRect.offsetMax = new Vector2(-72f, -140f);
        mailBodyLabel.alignment = TextAlignmentOptions.TopLeft;
        mailBodyLabel.fontStyle = FontStyles.Normal;

        progressLabel = CreateLabel("ProgressLabel", detailPane.transform, string.Empty, 18f, new Color(0.16f, 0.34f, 0.5f, 1f));
        RectTransform progressRect = progressLabel.GetComponent<RectTransform>();
        progressRect.anchorMin = new Vector2(1f, 1f);
        progressRect.anchorMax = new Vector2(1f, 1f);
        progressRect.sizeDelta = new Vector2(220f, 40f);
        progressRect.anchoredPosition = new Vector2(-145f, -48f);

        CreateHourButtons(detailPane.transform);

        statusLabel = CreateLabel("StatusLabel", detailPane.transform, string.Empty, 20f, new Color(0.62f, 0.08f, 0.06f, 1f));
        RectTransform statusRect = statusLabel.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(1f, 0f);
        statusRect.offsetMin = new Vector2(52f, 46f);
        statusRect.offsetMax = new Vector2(-52f, 92f);
        statusLabel.alignment = TextAlignmentOptions.Left;
    }

    private void CreateHourButtons(Transform parent)
    {
        for (int i = 0; i < AvailableHours.Length; i++)
        {
            string hour = AvailableHours[i];
            GameObject buttonObject = CreateUiImage($"Hour {hour}", parent, new Color(0.77f, 0.9f, 0.64f, 1f));
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 0.5f);
            buttonRect.anchorMax = new Vector2(0f, 0.5f);
            buttonRect.sizeDelta = new Vector2(155f, 58f);
            buttonRect.anchoredPosition = new Vector2(118f + (i % 3) * 185f, -50f - (i / 3) * 78f);

            Button button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener(() => SelectHour(hour));
            hourButtons.Add(button);

            TextMeshProUGUI label = CreateLabel("HourLabel", buttonObject.transform, hour, 19f, Color.black);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }
    }

    private void CreateCloseButton(Transform parent)
    {
        GameObject buttonObject = CreateUiImage("Close Mail Scheduling", parent, new Color(0.08f, 0.08f, 0.09f, 0.95f));
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(54f, 54f);
        buttonRect.anchoredPosition = new Vector2(650f, 395f);

        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(() => panelRoot.SetActive(false));

        TextMeshProUGUI label = CreateLabel("CloseLabel", buttonObject.transform, "X", 26f, Color.white);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }

    private void UpdateInboxVisuals()
    {
        for (int i = 0; i < inboxItemImages.Count; i++)
        {
            bool isSelected = i == selectedMailIndex;
            inboxItemImages[i].color = isSelected
                ? new Color(0.82f, 0.9f, 0.97f, 1f)
                : Color.white;

            inboxItemLabels[i].text = MailTasks[i].InboxTitle;
        }
    }

    private void UpdateProgressLabel()
    {
        progressLabel.text = $"{hourByMailIndex.Count}/{MailTasks.Length} planlandı";
    }

    private void UpdateHourButtonVisuals()
    {
        for (int i = 0; i < hourButtons.Count; i++)
        {
            string hour = AvailableHours[i];
            Image image = hourButtons[i].GetComponent<Image>();

            if (hourByMailIndex.TryGetValue(selectedMailIndex, out string selectedHour) && selectedHour == hour)
            {
                image.color = new Color(0.36f, 0.74f, 0.36f, 1f);
            }
            else
            {
                image.color = new Color(0.77f, 0.9f, 0.64f, 1f);
            }
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

    private static void CreateSeparator(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject separator = CreateUiImage(name, parent, new Color(0.08f, 0.08f, 0.08f, 1f));
        RectTransform rect = separator.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
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

    private readonly struct MailTask
    {
        public readonly string Title;
        public readonly string InboxTitle;
        public readonly string Body;

        public MailTask(string title, string inboxTitle, string body)
        {
            Title = title;
            InboxTitle = inboxTitle;
            Body = body;
        }
    }
}
